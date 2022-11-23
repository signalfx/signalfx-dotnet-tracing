// <copyright file="RuntimeMetricsWriter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.SignalFx.Metrics;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.Newtonsoft.Json.Utilities;
using Datadog.Trace.Vendors.StatsdClient;
using MetricType = Datadog.Tracer.SignalFx.Metrics.Protobuf.MetricType;

namespace Datadog.Trace.RuntimeMetrics
{
    internal class RuntimeMetricsWriter : IDisposable
    {
        private const string ProcessMetrics = $"{MetricsNames.Process.ThreadsCount}, {MetricsNames.Process.MemoryUsage}, {MetricsNames.Process.MemoryVirtual}, {MetricsNames.Process.CpuTime}, {MetricsNames.Process.CpuUtilization}";

        private static readonly string[] CpuUserTag = new[] { "state:user" };
        private static readonly string[] CpuSystemTag = new[] { "state:system" };

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<RuntimeMetricsWriter>();
        private static readonly Func<ImmutableMetricsIntegrationSettingsCollection, ISignalFxMetricSender, TimeSpan, IRuntimeMetricsListener> InitializeListenerFunc = InitializeListener;

        private readonly IList<Action> sendMetricsActions = new List<Action>();

        private readonly ImmutableMetricsIntegrationSettingsCollection _settings;
        private readonly TimeSpan _delay;

        private readonly ISignalFxMetricSender _metricSender;
        private readonly Timer _timer;

        private readonly IRuntimeMetricsListener _listener;

        private readonly bool _enableProcessMetrics;

        private readonly ConcurrentDictionary<string, int> _exceptionCounts = new ConcurrentDictionary<string, int>();

        private TimeSpan _previousUserCpu;
        private TimeSpan _previousSystemCpu;

        public RuntimeMetricsWriter(ImmutableMetricsIntegrationSettingsCollection settings, ISignalFxMetricSender metricSender, TimeSpan delay)
            : this(settings, metricSender, delay, InitializeListenerFunc)
        {
        }

        internal RuntimeMetricsWriter(
            ImmutableMetricsIntegrationSettingsCollection settings,
            ISignalFxMetricSender metricSender,
            TimeSpan delay,
            Func<ImmutableMetricsIntegrationSettingsCollection, ISignalFxMetricSender, TimeSpan, IRuntimeMetricsListener> initializeListener)
        {
            _settings = settings;

            _delay = delay;
            _metricSender = metricSender;
            _timer = new Timer(_ => PushEvents(), null, delay, delay);

            try
            {
                AppDomain.CurrentDomain.FirstChanceException += FirstChanceException;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "First chance exceptions won't be monitored");
            }

            try
            {
                ProcessHelpers.GetCurrentProcessRuntimeMetrics(out var userCpu, out var systemCpu, out _, out _, out _);

                _previousUserCpu = userCpu;
                _previousSystemCpu = systemCpu;

                _enableProcessMetrics = true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to get current process information");
                _enableProcessMetrics = false;
            }

            try
            {
                _listener = initializeListener(_settings, metricSender, delay);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to initialize runtime listener, some runtime metrics will be missing");
            }

            var actionsForMetricsSending = new Dictionary<string, Action>
            {
                [MetricsIntegrationId.Process.ToString()] = SendProcessMetrics,
                [MetricsIntegrationId.NetRuntime.ToString()] = SendNetRuntimeMetrics
            };

            foreach (var setting in settings.Settings)
            {
                if (setting.Enabled && actionsForMetricsSending.TryGetValue(setting.IntegrationName, out var action))
                {
                    sendMetricsActions.Add(action);
                }
            }
        }

        /// <summary>
        /// Gets the internal exception counts, to be used for tests
        /// </summary>
        internal ConcurrentDictionary<string, int> ExceptionCounts => _exceptionCounts;

        public void Dispose()
        {
            AppDomain.CurrentDomain.FirstChanceException -= FirstChanceException;
            _timer.Dispose();
            _listener?.Dispose();
            _exceptionCounts.Clear();
        }

        internal void PushEvents()
        {
            try
            {
                foreach (var sendMetrics in sendMetricsActions)
                {
                    sendMetrics();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error while updating metrics");
            }
        }

        private static IRuntimeMetricsListener InitializeListener(ImmutableMetricsIntegrationSettingsCollection settings, ISignalFxMetricSender metricSender, TimeSpan delay)
        {
#if NETCOREAPP
            if (settings[MetricsIntegrationId.NetRuntime].Enabled || settings[MetricsIntegrationId.AspNet].Enabled)
            {
                return new RuntimeEventListener(settings, metricSender, delay);
            }

            return null;
#elif NETFRAMEWORK
            if (settings[MetricsIntegrationId.NetRuntime].Enabled)
            {
                return AzureAppServices.Metadata.IsRelevant ? new AzureAppServicePerformanceCounters(metricSender) : new PerformanceCountersListener(metricSender);
            }

            return null;
#else
            return null;
#endif
        }

        private void SendNetRuntimeMetrics()
        {
            _listener?.Refresh();

            GcMetrics.PushCollectionCounts(_metricSender);
            _metricSender.SendLong(MetricsNames.NetRuntime.Gc.TotalObjectsSize, GC.GetTotalMemory(false), MetricType.GAUGE);

            if (!_exceptionCounts.IsEmpty)
            {
                foreach (var element in _exceptionCounts)
                {
                    _metricSender.SendLong(MetricsNames.NetRuntime.ExceptionsCount, element.Value, MetricType.COUNTER, new[] { $"exception_type:{element.Key}" });
                }

                // There's a race condition where we could clear items that haven't been pushed
                // Having an exact exception count is probably not worth the overhead required to fix it
                _exceptionCounts.Clear();

                Log.Debug("Sent the following metrics: {metrics}", MetricsNames.NetRuntime.ExceptionsCount);
            }
            else
            {
                Log.Debug("Did not send the following metrics: {metrics}", MetricsNames.NetRuntime.ExceptionsCount);
            }
        }

        private void SendProcessMetrics()
        {
            if (!_enableProcessMetrics)
            {
                return;
            }

            ProcessHelpers.GetCurrentProcessRuntimeMetrics(out var newUserCpu, out var newSystemCpu, out var threadCount, out var virtualMemory, out var workingSet);

            var userCpu = newUserCpu - _previousUserCpu;
            var systemCpu = newSystemCpu - _previousSystemCpu;

            _previousUserCpu = newUserCpu;
            _previousSystemCpu = newSystemCpu;

            _metricSender.SendLong(MetricsNames.Process.ThreadsCount, threadCount, MetricType.GAUGE);

            _metricSender.SendLong(MetricsNames.Process.MemoryUsage, workingSet, MetricType.GAUGE);
            _metricSender.SendLong(MetricsNames.Process.MemoryVirtual, virtualMemory, MetricType.GAUGE);

            _metricSender.SendDouble(MetricsNames.Process.CpuTime, newUserCpu.TotalSeconds, MetricType.CUMULATIVE_COUNTER, CpuUserTag);
            _metricSender.SendDouble(MetricsNames.Process.CpuTime, newSystemCpu.TotalSeconds, MetricType.CUMULATIVE_COUNTER, CpuSystemTag);

            // Note: the behavior of Environment.ProcessorCount has changed a lot across version: https://github.com/dotnet/runtime/issues/622
            // What we want is the number of cores attributed to the container, which is the behavior in 3.1.2+ (and, I believe, in 2.x)
            var maximumCpu = Environment.ProcessorCount * _delay.TotalSeconds;

            // https://github.com/open-telemetry/opentelemetry-dotnet-contrib/pull/687#discussion_r995076259
            _metricSender.SendDouble(MetricsNames.Process.CpuUtilization, Math.Min(userCpu.TotalSeconds / maximumCpu, 1D), MetricType.GAUGE, CpuUserTag);
            _metricSender.SendDouble(MetricsNames.Process.CpuUtilization, Math.Min(systemCpu.TotalSeconds / maximumCpu, 1D), MetricType.GAUGE, CpuSystemTag);

            Log.Debug("Sent the following metrics: {metrics}", ProcessMetrics);
        }

        private void FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            var name = e.Exception.GetType().Name;

            _exceptionCounts.AddOrUpdate(name, 1, (_, count) => count + 1);
        }
    }
}
