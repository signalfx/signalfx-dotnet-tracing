// <copyright file="ImmutableTracerSettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Datadog.Trace.Logging.DirectSubmission;
using Datadog.Trace.Util;

namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Contains Tracer settings.
    /// </summary>
    public class ImmutableTracerSettings
    {
        private readonly DomainMetadata _domainMetadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableTracerSettings"/> class
        /// using the specified <see cref="IConfigurationSource"/> to initialize values.
        /// </summary>
        /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
        public ImmutableTracerSettings(IConfigurationSource source)
            : this(new TracerSettings(source))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableTracerSettings"/> class from
        /// a TracerSettings instance.
        /// </summary>
        /// <param name="settings">The tracer settings to use to populate the immutable tracer settings</param>
        public ImmutableTracerSettings(TracerSettings settings)
        {
            Environment = settings.Environment;
            ServiceName = settings.ServiceName;
            ServiceVersion = settings.ServiceVersion;
            TraceEnabled = settings.TraceEnabled;
            ExporterSettings = new ImmutableExporterSettings(settings.ExporterSettings);
            TracesTransport = settings.TracesTransport;
#pragma warning disable 618 // App analytics is deprecated, but still used
            AnalyticsEnabled = settings.AnalyticsEnabled;
#pragma warning restore 618
            MaxTracesSubmittedPerSecond = settings.MaxTracesSubmittedPerSecond;
            CustomSamplingRules = settings.CustomSamplingRules;
            GlobalSamplingRate = settings.GlobalSamplingRate;
            Integrations = new ImmutableIntegrationSettingsCollection(settings.Integrations, settings.DisabledIntegrationNames);
            GlobalTags = new ReadOnlyDictionary<string, string>(settings.GlobalTags);
            HeaderTags = new ReadOnlyDictionary<string, string>(settings.HeaderTags);
            GrpcTags = new ReadOnlyDictionary<string, string>(settings.GrpcTags);
            TracerMetricsEnabled = settings.TracerMetricsEnabled;
            RuntimeMetricsEnabled = settings.RuntimeMetricsEnabled;
            KafkaCreateConsumerScopeEnabled = settings.KafkaCreateConsumerScopeEnabled;
            StartupDiagnosticLogEnabled = settings.StartupDiagnosticLogEnabled;
            HttpClientExcludedUrlSubstrings = settings.HttpClientExcludedUrlSubstrings;
            HttpServerErrorStatusCodes = settings.HttpServerErrorStatusCodes;
            HttpClientErrorStatusCodes = settings.HttpClientErrorStatusCodes;
            ServiceNameMappings = settings.ServiceNameMappings;
            TraceBufferSize = settings.TraceBufferSize;
            TraceBatchInterval = settings.TraceBatchInterval;
            RouteTemplateResourceNamesEnabled = settings.RouteTemplateResourceNamesEnabled;
            DelayWcfInstrumentationEnabled = settings.DelayWcfInstrumentationEnabled;
            TraceMethods = settings.TraceMethods;
            IsActivityListenerEnabled = settings.IsActivityListenerEnabled;

            TagMongoCommands = settings.TagMongoCommands;
            TagRedisCommands = settings.TagRedisCommands;
            TagElasticsearchQueries = settings.TagElasticsearchQueries;
            TraceResponseHeaderEnabled = settings.TraceResponseHeaderEnabled;
            RecordedValueMaxLength = settings.RecordedValueMaxLength;
            SignalFxAccessToken = settings.SignalFxAccessToken;
            Convention = settings.Convention;
            Exporter = settings.Exporter;

            ThreadSamplingEnabled = settings.ThreadSamplingEnabled;
            ThreadSamplingPeriod = settings.ThreadSamplingPeriod;
            LogSubmissionSettings = ImmutableDirectLogSubmissionSettings.Create(settings.LogSubmissionSettings);
            // Logs injection is enabled by default if direct log submission is enabled, otherwise disabled by default
            LogsInjectionEnabled = settings.LogSubmissionSettings.LogsInjectionEnabled ?? LogSubmissionSettings.IsEnabled;

            PropagationStyleInject = settings.PropagationStyleInject;
            PropagationStyleExtract = settings.PropagationStyleExtract;

            // we cached the static instance here, because is being used in the hotpath
            // by IsIntegrationEnabled method (called from all integrations)
            _domainMetadata = DomainMetadata.Instance;

            ExpandRouteTemplatesEnabled = settings.ExpandRouteTemplatesEnabled || !RouteTemplateResourceNamesEnabled;
        }

        /// <summary>
        /// Gets the default environment name applied to all spans.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.Environment"/>
        public string Environment { get; }

        /// <summary>
        /// Gets the service name applied to top-level spans and used to build derived service names.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.ServiceName"/>
        public string ServiceName { get; }

        /// <summary>
        /// Gets the version tag applied to all spans.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.ServiceVersion"/>
        public string ServiceVersion { get; }

        /// <summary>
        /// Gets a value indicating whether tracing is enabled.
        /// Default is <c>true</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TraceEnabled"/>
        public bool TraceEnabled { get; }

        /// <summary>
        /// Gets the exporter settings that dictate how the tracer exports data.
        /// </summary>
        public ImmutableExporterSettings ExporterSettings { get; }

        /// <summary>
        /// Gets the key used to determine the transport for sending traces.
        /// Default is <c>null</c>, which will use the default path decided in <see cref="Agent.Api"/>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TracesTransport"/>
        public string TracesTransport { get; }

        /// <summary>
        /// Gets a value indicating whether default Analytics are enabled.
        /// Settings this value is a shortcut for setting
        /// <see cref="Configuration.IntegrationSettings.AnalyticsEnabled"/> on some predetermined integrations.
        /// See the documentation for more details.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.GlobalAnalyticsEnabled"/>
        [Obsolete(DeprecationMessages.AppAnalytics)]
        public bool AnalyticsEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether correlation identifiers are
        /// automatically injected into the logging context.
        /// Default is <c>false</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.LogsInjectionEnabled"/>
        public bool LogsInjectionEnabled { get; }

        /// <summary>
        /// Gets a value indicating the maximum number of traces set to AutoKeep (p1) per second.
        /// Default is <c>100</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TraceRateLimit"/>
        public int MaxTracesSubmittedPerSecond { get; }

        /// <summary>
        /// Gets a value indicating custom sampling rules.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.CustomSamplingRules"/>
        public string CustomSamplingRules { get; }

        /// <summary>
        /// Gets a value indicating a global rate for sampling.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.GlobalSamplingRate"/>
        public double? GlobalSamplingRate { get; }

        /// <summary>
        /// Gets a collection of <see cref="Integrations"/> keyed by integration name.
        /// </summary>
        public ImmutableIntegrationSettingsCollection Integrations { get; }

        /// <summary>
        /// Gets the global tags, which are applied to all <see cref="Span"/>s.
        /// </summary>
        public IReadOnlyDictionary<string, string> GlobalTags { get; }

        /// <summary>
        /// Gets the map of header keys to tag names, which are applied to the root <see cref="Span"/>
        /// of incoming and outgoing requests.
        /// </summary>
        public IReadOnlyDictionary<string, string> HeaderTags { get; }

        /// <summary>
        /// Gets the map of metadata keys to tag names, which are applied to the root <see cref="Span"/>
        /// of incoming and outgoing GRPC requests.
        /// </summary>
        public IReadOnlyDictionary<string, string> GrpcTags { get; }

        /// <summary>
        /// Gets a value indicating whether internal metrics
        /// are enabled and sent to DogStatsd.
        /// </summary>
        public bool TracerMetricsEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether a span context should be created on exiting a successful Kafka
        /// Consumer.Consume() call, and closed on entering Consumer.Consume().
        /// </summary>
        /// <seealso cref="ConfigurationKeys.KafkaCreateConsumerScopeEnabled"/>
        public bool KafkaCreateConsumerScopeEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the diagnostic log at startup is enabled
        /// </summary>
        public bool StartupDiagnosticLogEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether runtime metrics
        /// are enabled and sent to DogStatsd.
        /// </summary>
        internal bool RuntimeMetricsEnabled { get; }

        /// <summary>
        /// Gets the comma separated list of url patterns to skip tracing.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpClientExcludedUrlSubstrings"/>
        internal string[] HttpClientExcludedUrlSubstrings { get; }

        /// <summary>
        /// Gets the HTTP status code that should be marked as errors for server integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpServerErrorStatusCodes"/>
        internal bool[] HttpServerErrorStatusCodes { get; }

        /// <summary>
        /// Gets the HTTP status code that should be marked as errors for client integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpClientErrorStatusCodes"/>
        internal bool[] HttpClientErrorStatusCodes { get; }

        /// <summary>
        /// Gets configuration values for changing service names based on configuration
        /// </summary>
        internal ServiceNames ServiceNameMappings { get; }

        /// <summary>
        /// Gets a value indicating the size in bytes of the trace buffer
        /// </summary>
        internal int TraceBufferSize { get; }

        /// <summary>
        /// Gets a value indicating the batch interval for the serialization queue, in milliseconds
        /// </summary>
        internal int TraceBatchInterval { get; }

        /// <summary>
        /// Gets a value indicating whether the feature flag to enable the updated ASP.NET resource names is enabled
        /// </summary>
        /// <seealso cref="ConfigurationKeys.FeatureFlags.RouteTemplateResourceNamesEnabled"/>
        internal bool RouteTemplateResourceNamesEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether route parameters in ASP.NET and ASP.NET Core resource names
        /// should be expanded with their values. Only applies when  <see cref="RouteTemplateResourceNamesEnabled"/>
        /// is enabled.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.ExpandRouteTemplatesEnabled"/>
        internal bool ExpandRouteTemplatesEnabled { get; }

        internal ImmutableDirectLogSubmissionSettings LogSubmissionSettings { get; }

        /// <summary>
        /// Gets a value indicating whether MongoDb integration should tag the command BsonDocument as db.statement.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TagMongoCommands"/>
        internal bool TagMongoCommands { get; }

        /// <summary>
        /// Gets a value indicating whether Redis integrations
        /// should tag commands as db.statement.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TagRedisCommands"/>
        public bool TagRedisCommands { get; }

        /// <summary>
        /// Gets a value indicating whether Elasticsearch integration
        /// should tag PostData as db.statement.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TagElasticsearchQueries"/>
        public bool TagElasticsearchQueries { get; }

        /// <summary>
        /// Gets a value indicating whether context server timing header will be added.
        /// </summary>
        internal bool TraceResponseHeaderEnabled { get; }

        /// <summary>
        /// Gets a value with the maximum length a tag/log value can have.
        /// Values are completely truncated when set to 0, and ignored when set to negative
        /// or non-integer string. The default value is 12000.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.RecordedValueMaxLength"/>
        public int RecordedValueMaxLength { get; }

        /// <summary>
        /// Gets a value with the SignalFx access token. This is to be used when sending data
        /// directly to ingestion URL, ie.: no agent or collector is being used.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.SignalFxAccessToken"/>
        internal string SignalFxAccessToken { get; }

        /// <summary>
        /// Gets the name of the semantic convention to be used.
        /// The Tracer uses it to define operation names, span tags, statuses etc.
        /// <seealso cref="ConfigurationKeys.Convention"/>
        /// </summary>
        public ConventionType Convention { get; }

        /// <summary>
        /// Gets the name of the exporter to be used. The Tracer uses it to encode and
        /// dispatch traces.
        /// <seealso cref="ConfigurationKeys.Exporter"/>
        /// </summary>
        public ExporterType Exporter { get; }

        /// <summary>
        /// Gets a value indicating whether the thread sampling is enabled.
        /// </summary>
        public bool ThreadSamplingEnabled { get; }

        /// <summary>
        /// Gets a value for the thread sampling period.
        /// </summary>
        public TimeSpan ThreadSamplingPeriod { get; }

        /// <summary>
        /// Gets a value indicating whether to enable the updated WCF instrumentation that delays execution
        /// until later in the WCF pipeline when the WCF server exception handling is established.
        /// </summary>
        internal bool DelayWcfInstrumentationEnabled { get; }

        /// <summary>
        /// Gets a value indicating the injection propagation style.
        /// </summary>
        internal string[] PropagationStyleInject { get; }

        /// <summary>
        /// Gets a value indicating the extraction propagation style.
        /// </summary>
        internal string[] PropagationStyleExtract { get; }

        /// <summary>
        /// Gets a value indicating the trace methods configuration.
        /// </summary>
        internal string TraceMethods { get; }

        /// <summary>
        /// Gets a value indicating whether the activity listener is enabled or not.
        /// </summary>
        internal bool IsActivityListenerEnabled { get; }

        /// <summary>
        /// Create a <see cref="ImmutableTracerSettings"/> populated from the default sources
        /// returned by <see cref="GlobalSettings.CreateDefaultConfigurationSource()"/>.
        /// </summary>
        /// <returns>A <see cref="ImmutableTracerSettings"/> populated from the default sources.</returns>
        public static ImmutableTracerSettings FromDefaultSources()
        {
            var source = GlobalSettings.CreateDefaultConfigurationSource();
            return new ImmutableTracerSettings(source);
        }

        internal bool IsErrorStatusCode(int statusCode, bool serverStatusCode)
        {
            var source = serverStatusCode ? HttpServerErrorStatusCodes : HttpClientErrorStatusCodes;

            if (source == null)
            {
                return false;
            }

            if (statusCode >= source.Length)
            {
                return false;
            }

            return source[statusCode];
        }

        internal bool IsIntegrationEnabled(IntegrationId integration, bool defaultValue = true)
        {
            if (TraceEnabled && !_domainMetadata.ShouldAvoidAppDomain())
            {
                return Integrations[integration].Enabled ?? defaultValue;
            }

            return false;
        }

        [Obsolete(DeprecationMessages.AppAnalytics)]
        internal double? GetIntegrationAnalyticsSampleRate(IntegrationId integration, bool enabledWithGlobalSetting)
        {
            var integrationSettings = Integrations[integration];
            var analyticsEnabled = integrationSettings.AnalyticsEnabled ?? (enabledWithGlobalSetting && AnalyticsEnabled);
            return analyticsEnabled ? integrationSettings.AnalyticsSampleRate : (double?)null;
        }

        internal string GetServiceName(Tracer tracer, string serviceName)
        {
            return ServiceNameMappings.GetServiceName(tracer.DefaultServiceName, serviceName);
        }

        internal bool TryGetServiceName(string key, out string serviceName)
        {
            return ServiceNameMappings.TryGetServiceName(key, out serviceName);
        }
    }
}
