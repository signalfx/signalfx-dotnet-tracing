// <copyright file="TracerManagerFactory.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Datadog.Trace.Abstractions;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.Zipkin;
using Datadog.Trace.ClrProfiler;
using Datadog.Trace.Configuration;
using Datadog.Trace.ContinuousProfiler;
using Datadog.Trace.Conventions;
using Datadog.Trace.DogStatsd;
using Datadog.Trace.Logging;
using Datadog.Trace.Logging.DirectSubmission;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.Propagators;
using Datadog.Trace.RuntimeMetrics;
using Datadog.Trace.Sampling;
using Datadog.Trace.SignalFx.Metrics;
using Datadog.Trace.Telemetry;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.StatsdClient;
using ConfigurationKeys = Datadog.Trace.Configuration.ConfigurationKeys;
using MetricsTransportType = Datadog.Trace.Vendors.StatsdClient.Transport.TransportType;

namespace Datadog.Trace
{
    internal class TracerManagerFactory
    {
        private const string UnknownServiceName = "UnknownService";
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<TracerManagerFactory>();

        public static readonly TracerManagerFactory Instance = new();
        private const int MaxMetricsInAsyncQueue = 1000;

        /// <summary>
        /// The primary factory method, called by <see cref="TracerManager"/>,
        /// providing the previous global <see cref="TracerManager"/> instance (may be null)
        /// </summary>
        internal TracerManager CreateTracerManager(ImmutableTracerSettings settings, TracerManager previous)
        {
            // TODO: If relevant settings have not changed, continue using existing statsd/agent writer/runtime metrics etc
            var tracer = CreateTracerManager(
                settings,
                agentWriter: null,
                sampler: null,
                scopeManager: previous?.ScopeManager, // no configuration, so can always use the same one
                statsd: null,
                runtimeMetrics: null,
                logSubmissionManager: previous?.DirectLogSubmission,
                telemetry: null,
                metricSender: null);

            try
            {
                if (Profiler.Instance.Status.IsProfilerReady)
                {
                    NativeInterop.SetApplicationInfoForAppDomain(RuntimeId.Get(), tracer.DefaultServiceName, tracer.Settings.Environment, tracer.Settings.ServiceVersion);
                }
            }
            catch (Exception ex)
            {
                // We failed to retrieve the runtime from native this can be because:
                // - P/Invoke issue (unknown dll, unknown entrypoint...)
                // - We are running in a partial trust environment
                Log.Warning(ex, "Failed to set the service name for native.");
            }

            return tracer;
        }

        /// <summary>
        /// Internal for use in tests that create "standalone" <see cref="TracerManager"/> by
        /// </summary>
        /// <see cref="Tracer(TracerSettings, IAgentWriter, ISampler, IScopeManager, IDogStatsd, ITelemetryController)"/>
        internal TracerManager CreateTracerManager(
            ImmutableTracerSettings settings,
            IAgentWriter agentWriter,
            ISampler sampler,
            IScopeManager scopeManager,
            IDogStatsd statsd,
            RuntimeMetricsWriter runtimeMetrics,
            DirectLogSubmissionManager logSubmissionManager,
            ITelemetryController telemetry,
            ISignalFxMetricSender metricSender)
        {
            settings ??= ImmutableTracerSettings.FromDefaultSources();

            var defaultServiceName = settings.ServiceName ??
                                     GetApplicationName() ??
                                     UnknownServiceName;

            var traceIdConvention = GetTraceIdConvention(settings.Convention);

            if (settings.TracerMetricsEnabled)
            {
                metricSender ??= CreateMetricSender(settings, defaultServiceName);
                statsd ??= CreateDogStatsdClient(settings, defaultServiceName, metricSender);
            }
            else
            {
                statsd = null;
            }

            sampler ??= GetSampler(settings);

            agentWriter ??= GetAgentWriter(settings, statsd, sampler);
            var supportsCpuProfiling = settings.CpuProfilingEnabled && FrameworkDescription.Instance.SupportsCpuProfiling();
            var supportsMemoryProfiling = settings.MemoryProfilingEnabled && FrameworkDescription.Instance.SupportsMemoryProfiling();
            scopeManager ??= new AsyncLocalScopeManager(supportsCpuProfiling || supportsMemoryProfiling);

            if (settings.MetricsIntegrations.Settings.Any(s => s.Enabled) && !DistributedTracer.Instance.IsChildTracer)
            {
                metricSender ??= CreateMetricSender(settings, defaultServiceName);
                runtimeMetrics ??= new RuntimeMetricsWriter(settings.MetricsIntegrations, metricSender, TimeSpan.FromSeconds(10));
            }

            logSubmissionManager = DirectLogSubmissionManager.Create(
                logSubmissionManager,
                settings.LogSubmissionSettings,
                defaultServiceName,
                settings.Environment,
                settings.ServiceVersion);

            telemetry ??= TelemetryFactory.CreateTelemetryController(settings);
            telemetry.RecordTracerSettings(settings, defaultServiceName, AzureAppServices.Metadata);

            SpanContextPropagator.Instance = ContextPropagators.GetSpanContextPropagator(settings.PropagationStyleInject, settings.PropagationStyleExtract);

            var tracerManager = CreateTracerManagerFrom(settings, agentWriter, sampler, scopeManager, statsd, runtimeMetrics, traceIdConvention, logSubmissionManager, telemetry, defaultServiceName, metricSender);
            return tracerManager;
        }

        /// <summary>
        ///  Can be overriden to create a different <see cref="TracerManager"/>, e.g. <see cref="Ci.CITracerManager"/>
        /// </summary>
        protected virtual TracerManager CreateTracerManagerFrom(
            ImmutableTracerSettings settings,
            IAgentWriter agentWriter,
            ISampler sampler,
            IScopeManager scopeManager,
            IDogStatsd statsd,
            RuntimeMetricsWriter runtimeMetrics,
            ITraceIdConvention traceIdConvention,
            DirectLogSubmissionManager logSubmissionManager,
            ITelemetryController telemetry,
            string defaultServiceName,
            ISignalFxMetricSender metricSender)
            => new TracerManager(settings, agentWriter, sampler, scopeManager, statsd, runtimeMetrics, traceIdConvention, logSubmissionManager, telemetry, defaultServiceName, metricSender);

        protected virtual ISampler GetSampler(ImmutableTracerSettings settings)
        {
            var sampler = new RuleBasedSampler(new TracerRateLimiter(settings.MaxTracesSubmittedPerSecond));

            if (!string.IsNullOrWhiteSpace(settings.CustomSamplingRules))
            {
                foreach (var rule in CustomSamplingRule.BuildFromConfigurationString(settings.CustomSamplingRules))
                {
                    sampler.RegisterRule(rule);
                }
            }

            if (settings.GlobalSamplingRate != null)
            {
                var globalRate = (float)settings.GlobalSamplingRate;

                if (globalRate < 0f || globalRate > 1f)
                {
                    Log.Warning("{ConfigurationKey} configuration of {ConfigurationValue} is out of range", ConfigurationKeys.GlobalSamplingRate, settings.GlobalSamplingRate);
                }
                else
                {
                    sampler.RegisterRule(new GlobalSamplingRule(globalRate));
                }
            }

            return sampler;
        }

        protected virtual IAgentWriter GetAgentWriter(ImmutableTracerSettings settings, IDogStatsd statsd, ISampler sampler)
        {
            IMetrics metrics = statsd != null
                ? new DogStatsdMetrics(statsd)
                : new NullMetrics();

            switch (settings.Exporter)
            {
                case ExporterType.Zipkin:
                    return new ExporterWriter(new ZipkinExporter(settings), metrics, queueSize: settings.ExporterSettings.TraceBufferSize);
                default:
                    var apiRequestFactory = TracesTransportStrategy.Get(settings.ExporterSettings);
                    var api = new Api(apiRequestFactory, statsd, sampler.SetDefaultSampleRates, settings.ExporterSettings.PartialFlushEnabled, settings.StatsComputationEnabled);

                    var statsAggregator = StatsAggregator.Create(api, settings);

                    const int maxBufferSize = 1024 * 1024 * 10;
                    return new AgentWriter(api, statsAggregator, metrics, maxBufferSize: maxBufferSize);
            }
        }

        private static IDogStatsd CreateDogStatsdClient(ImmutableTracerSettings settings, string serviceName, ISignalFxMetricSender metricSender)
        {
            try
            {
                var constantTags = GetConstantTags(settings, serviceName);

                if (settings.ExporterSettings.MetricsExporter == MetricsExporterType.SignalFx)
                {
                    return new SignalFxStats(metricSender);
                }

                var statsd = new DogStatsdService();
                switch (settings.ExporterSettings.MetricsTransport)
                {
                    case MetricsTransportType.NamedPipe:
                        // Environment variables for windows named pipes are not explicitly passed to statsd.
                        // They are retrieved within the vendored code, so there is nothing to pass.
                        // Passing anything through StatsdConfig may cause bugs when windows named pipes should be used.
                        Log.Information("Using windows named pipes for metrics transport.");
                        statsd.Configure(new StatsdConfig
                        {
                            ConstantTags = constantTags.ToArray()
                        });
                        break;
                    case MetricsTransportType.UDP:
                    default:
                        statsd.Configure(new StatsdConfig
                        {
                            StatsdServerName = settings.ExporterSettings.AgentUri.DnsSafeHost,
                            StatsdPort = settings.ExporterSettings.DogStatsdPort,
                            ConstantTags = constantTags.ToArray()
                        });
                        break;
                }

                return statsd;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to instantiate StatsD client.");
                return new NoOpStatsd();
            }
        }

        private static SignalFxMetricSender CreateMetricSender(ImmutableTracerSettings settings, string serviceName)
        {
            var metricExporter = new SignalFxMetricExporter(settings.ExporterSettings.MetricsEndpointUrl, settings.SignalFxAccessToken);
            return new SignalFxMetricSender(GetConstantTags(settings, serviceName).ToArray(), metricExporter, MaxMetricsInAsyncQueue);
        }

        private static List<string> GetConstantTags(ImmutableTracerSettings settings, string serviceName)
        {
            var constantTags = new List<string>
            {
                "lang:.NET",
                $"lang_interpreter:{FrameworkDescription.Instance.Name}",
                $"lang_version:{FrameworkDescription.Instance.ProductVersion}",
                $"tracer_version:{TracerConstants.AssemblyVersion}",
                $"service:{serviceName}",
                $"{Tags.RuntimeId}:{DistributedTracer.Instance.GetRuntimeId()}"
            };

            if (settings.Environment != null)
            {
                constantTags.Add($"env:{settings.Environment}");
            }

            if (settings.ServiceVersion != null)
            {
                constantTags.Add($"version:{settings.ServiceVersion}");
            }

            return constantTags;
        }

        /// <summary>
        /// Gets an "application name" for the executing application by looking at
        /// the hosted app name (.NET Framework on IIS only), assembly name, and process name.
        /// </summary>
        /// <returns>The default service name.</returns>
        private static string GetApplicationName()
        {
            try
            {
                if (AzureAppServices.Metadata.IsRelevant)
                {
                    return AzureAppServices.Metadata.SiteName;
                }

                try
                {
                    if (TryLoadAspNetSiteName(out var siteName))
                    {
                        return siteName;
                    }
                }
                catch (Exception ex)
                {
                    // Unable to call into System.Web.dll
                    Log.Error(ex, "Unable to get application name through ASP.NET settings");
                }

                return Assembly.GetEntryAssembly()?.GetName().Name ??
                       ProcessHelpers.GetCurrentProcessName();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating default service name.");
                return null;
            }
        }

        private static bool TryLoadAspNetSiteName(out string siteName)
        {
#if NETFRAMEWORK
            // System.Web.dll is only available on .NET Framework
            if (System.Web.Hosting.HostingEnvironment.IsHosted)
            {
                // if this app is an ASP.NET application, return "SiteName/ApplicationVirtualPath".
                // note that ApplicationVirtualPath includes a leading slash.
                siteName = (System.Web.Hosting.HostingEnvironment.SiteName + System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath).TrimEnd('/');
                return true;
            }

#endif
            siteName = default;
            return false;
        }

        private ITraceIdConvention GetTraceIdConvention(ConventionType convention)
        {
            switch (convention)
            {
                case ConventionType.Datadog:
                    return new DatadogTraceIdConvention();
                case ConventionType.OpenTelemetry:
                default:
                    return new OtelTraceIdConvention();
            }
        }
    }
}
