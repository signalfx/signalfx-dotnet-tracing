// <copyright file="TracerSettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Datadog.Trace.Configuration.Helpers;
using Datadog.Trace.Debugger;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Logging.DirectSubmission;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.Vendors.Serilog;

namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Contains Tracer settings.
    /// </summary>
    public class TracerSettings
    {
        private const int DefaultRecordedValueMaxLength = 12000;

        /// <summary>
        /// Default obfuscation query string regex if none specified via env DD_OBFUSCATION_QUERY_STRING_REGEXP
        /// </summary>
        internal const string DefaultObfuscationQueryStringRegex = @"((?i)(?:p(?:ass)?w(?:or)?d|pass(?:_?phrase)?|secret|(?:api_?|private_?|public_?|access_?|secret_?)key(?:_?id)?|token|consumer_?(?:id|key|secret)|sign(?:ed|ature)?|auth(?:entication|orization)?)(?:(?:\s|%20)*(?:=|%3D)[^&]+|(?:""|%22)(?:\s|%20)*(?::|%3A)(?:\s|%20)*(?:""|%22)(?:%2[^2]|%[^2]|[^""%])+(?:""|%22))|bearer(?:\s|%20)+[a-z0-9\._\-]|token(?::|%3A)[a-z0-9]{13}|gh[opsu]_[0-9a-zA-Z]{36}|ey[I-L](?:[\w=-]|%3D)+\.ey[I-L](?:[\w=-]|%3D)+(?:\.(?:[\w.+\/=-]|%3D|%2F|%2B)+)?|[\-]{5}BEGIN(?:[a-z\s]|%20)+PRIVATE(?:\s|%20)KEY[\-]{5}[^\-]+[\-]{5}END(?:[a-z\s]|%20)+PRIVATE(?:\s|%20)KEY|ssh-rsa(?:\s|%20)*(?:[a-z0-9\/\.+]|%2F|%5C|%2B){100,})";

        /// <summary>
        /// Initializes a new instance of the <see cref="TracerSettings"/> class with default values.
        /// </summary>
        public TracerSettings()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TracerSettings"/> class with default values,
        /// or initializes the configuration from environment variables and configuration files.
        /// Calling <c>new TracerSettings(true)</c> is equivalent to calling <c>TracerSettings.FromDefaultSources()</c>
        /// </summary>
        /// <param name="useDefaultSources">If <c>true</c>, creates a <see cref="TracerSettings"/> populated from
        /// the default sources such as environment variables etc. If <c>false</c>, uses the default values.</param>
        public TracerSettings(bool useDefaultSources)
            : this(useDefaultSources ? CreateDefaultConfigurationSource() : null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TracerSettings"/> class
        /// using the specified <see cref="IConfigurationSource"/> to initialize values.
        /// </summary>
        /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
        public TracerSettings(IConfigurationSource source)
        {
            Environment = source?.GetString(ConfigurationKeys.Environment);

            ServiceName = source?.GetString(ConfigurationKeys.ServiceName);

            ServiceVersion = source?.GetString(ConfigurationKeys.ServiceVersion);

            SignalFxAccessToken = source?.GetString(ConfigurationKeys.SignalFxAccessToken);

            TraceEnabled = source?.GetBool(ConfigurationKeys.TraceEnabled) ??
                           // default value
                           true;

            if (AzureAppServices.Metadata.IsRelevant && AzureAppServices.Metadata.IsUnsafeToTrace)
            {
                TraceEnabled = false;
            }

            DisabledIntegrationNames = new HashSet<string>(source.GetStrings(ConfigurationKeys.DisabledIntegrations), StringComparer.OrdinalIgnoreCase);

            Integrations = new IntegrationSettingsCollection(source);

            MetricsIntegrations = new MetricsIntegrationSettingsCollection(source);

            ExporterSettings = new ExporterSettings(source);

#pragma warning disable 618 // App analytics is deprecated, but still used
            AnalyticsEnabled = source?.GetBool(ConfigurationKeys.GlobalAnalyticsEnabled) ??
                               // default value
                               false;
#pragma warning restore 618

            MaxTracesSubmittedPerSecond = source?.GetInt32(ConfigurationKeys.TraceRateLimit) ??
                                          // default value
                                          100;

            GlobalTags = source?.GetDictionary(ConfigurationKeys.GlobalTags) ??
                         // backwards compatibility for names used in the past
                         source?.GetDictionary("SIGNALFX_TRACE_GLOBAL_TAGS") ??
                         // default value (empty)
                         new ConcurrentDictionary<string, string>();

            // Filter out tags with empty keys or empty values, and trim whitespace
            GlobalTags = GlobalTags.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                                   .ToDictionary(kvp => kvp.Key.Trim(), kvp => kvp.Value.Trim());

            var inputHeaderTags = source?.GetDictionary(ConfigurationKeys.HeaderTags, allowOptionalMappings: true) ??
                                  // default value (empty)
                                  new Dictionary<string, string>();

            var headerTagsNormalizationFixEnabled = source?.GetBool(ConfigurationKeys.FeatureFlags.HeaderTagsNormalizationFixEnabled) ?? true;
            // Filter out tags with empty keys or empty values, and trim whitespaces
            HeaderTags = InitializeHeaderTags(inputHeaderTags, headerTagsNormalizationFixEnabled);

            var serviceNameMappings = source?.GetDictionary(ConfigurationKeys.ServiceNameMappings)
                                            ?.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                                            ?.ToDictionary(kvp => kvp.Key.Trim(), kvp => kvp.Value.Trim());

            ServiceNameMappings = new ServiceNames(serviceNameMappings);

            TracerMetricsEnabled = source?.GetBool(ConfigurationKeys.TracerMetricsEnabled) ??
                                   // default value
                                   false;

            TagRedisCommands = source?.GetBool(ConfigurationKeys.TagRedisCommands) ??
                            // default value
                            true;

            StatsComputationEnabled = source?.GetBool(ConfigurationKeys.StatsComputationEnabled) ?? false;

            CustomSamplingRules = source?.GetString(ConfigurationKeys.CustomSamplingRules);

            GlobalSamplingRate = source?.GetDouble(ConfigurationKeys.GlobalSamplingRate);

            StartupDiagnosticLogEnabled = source?.GetBool(ConfigurationKeys.StartupDiagnosticLogEnabled) ??
                                          // default value
                                          true;

            Exporter = source.GetTypedValue<ExporterType>(ConfigurationKeys.Exporter);

            Convention = source.GetTypedValue<ConventionType>(ConfigurationKeys.Convention);

            var urlSubstringSkips = source?.GetString(ConfigurationKeys.HttpClientExcludedUrlSubstrings) ??
                                    // default value
                                    (AzureAppServices.Metadata.IsRelevant ? AzureAppServices.Metadata.DefaultHttpClientExclusions : null);

            if (urlSubstringSkips != null)
            {
                HttpClientExcludedUrlSubstrings = TrimSplitString(urlSubstringSkips.ToUpperInvariant(), ',').ToArray();
            }

            var httpServerErrorStatusCodes = source?.GetString(ConfigurationKeys.HttpServerErrorStatusCodes) ??
                                             // Default value
                                             "500-599";

            HttpServerErrorStatusCodes = ParseHttpCodesToArray(httpServerErrorStatusCodes);

            var httpClientErrorStatusCodes = source?.GetString(ConfigurationKeys.HttpClientErrorStatusCodes) ??
                                        // Default value
                                        "400-599";
            HttpClientErrorStatusCodes = ParseHttpCodesToArray(httpClientErrorStatusCodes);

            TraceBatchInterval = source?.GetInt32(ConfigurationKeys.SerializationBatchInterval)
                              ?? 100;

            RecordedValueMaxLength = source.SafeReadInt32(
                key: ConfigurationKeys.RecordedValueMaxLength,
                defaultTo: DefaultRecordedValueMaxLength,
                validators: (value) => value >= 0);

            RouteTemplateResourceNamesEnabled = source?.GetBool(ConfigurationKeys.FeatureFlags.RouteTemplateResourceNamesEnabled)
                                             ?? true;

            TraceResponseHeaderEnabled = source?.GetBool(ConfigurationKeys.TraceResponseHeaderEnabled)
                                            ?? true;
            ExpandRouteTemplatesEnabled = source?.GetBool(ConfigurationKeys.ExpandRouteTemplatesEnabled)
                                          // disabled by default if route template resource names enabled
                                       ?? !RouteTemplateResourceNamesEnabled;

            KafkaCreateConsumerScopeEnabled = source?.GetBool(ConfigurationKeys.KafkaCreateConsumerScopeEnabled)
                                           ?? true; // default

            TagMongoCommands = source?.GetBool(ConfigurationKeys.TagMongoCommands) ?? true;

            DelayWcfInstrumentationEnabled = source?.GetBool(ConfigurationKeys.FeatureFlags.DelayWcfInstrumentationEnabled)
                                          ?? false;

            ObfuscationQueryStringRegex = source?.GetString(ConfigurationKeys.ObfuscationQueryStringRegex) ?? DefaultObfuscationQueryStringRegex;

            QueryStringReportingEnabled = source?.GetBool(ConfigurationKeys.QueryStringReportingEnabled) ?? true;

            ObfuscationQueryStringRegexTimeout = source?.GetDouble(ConfigurationKeys.ObfuscationQueryStringRegexTimeout) is { } x and > 0 ? x : 200;

            PropagationStyleInject = TrimSplitString(source?.GetString(ConfigurationKeys.Propagators) ?? $"{nameof(Propagators.ContextPropagators.Names.B3)},{nameof(Propagators.ContextPropagators.Names.W3C)}", ',').ToArray();

            PropagationStyleExtract = PropagationStyleInject;

            TagElasticsearchQueries = source?.GetBool(ConfigurationKeys.TagElasticsearchQueries) ?? true;

            // If you change this, change environment_variables.h too
            CpuProfilingEnabled = source?.GetBool(ConfigurationKeys.AlwaysOnProfiler.CpuEnabled) ?? false;
            var memoryProfilingEnabled = source?.GetBool(ConfigurationKeys.AlwaysOnProfiler.MemoryEnabled) ?? false;
            MemoryProfilingEnabled = memoryProfilingEnabled;
            ThreadSamplingPeriod = GetThreadSamplingPeriod(source);

            var profilingExportInterval = source?.GetInt32(ConfigurationKeys.AlwaysOnProfiler.ExportInterval) ?? 10000;
            ProfilerExportInterval = TimeSpan.FromMilliseconds(profilingExportInterval);

            LogSubmissionSettings = new DirectLogSubmissionSettings(source);

            TraceMethods = source?.GetString(ConfigurationKeys.TraceMethods) ??
                           // Default value
                           string.Empty;

            var grpcTags = source?.GetDictionary(ConfigurationKeys.GrpcTags, allowOptionalMappings: true) ??
                           // default value (empty)
                           new Dictionary<string, string>();

            // Filter out tags with empty keys or empty values, and trim whitespaces
            GrpcTags = InitializeHeaderTags(grpcTags, headerTagsNormalizationFixEnabled: true);

            var propagationHeaderMaximumLength = source?.GetInt32(ConfigurationKeys.TagPropagation.HeaderMaxLength);

            TagPropagationHeaderMaxLength = propagationHeaderMaximumLength is >= 0 and <= Tagging.TagPropagation.OutgoingPropagationHeaderMaxLength
                ? (int)propagationHeaderMaximumLength
                : Tagging.TagPropagation.OutgoingPropagationHeaderMaxLength;

            IsActivityListenerEnabled = source?.GetBool(ConfigurationKeys.FeatureFlags.ActivityListenerEnabled) ??
                                        // default value
                                        false;

            IpHeader = source?.GetString(ConfigurationKeys.IpHeader);

            IpHeaderDisabled = source?.GetBool(ConfigurationKeys.IpHeaderDisabled) ?? false;

            if (IsActivityListenerEnabled)
            {
                // If the activities support is activated, we must enable W3C propagators
                if (!Array.Exists(PropagationStyleExtract, key => string.Equals(key, nameof(Propagators.ContextPropagators.Names.W3C), StringComparison.OrdinalIgnoreCase)))
                {
                    PropagationStyleExtract = PropagationStyleExtract.Concat(nameof(Propagators.ContextPropagators.Names.W3C));
                }

                if (!Array.Exists(PropagationStyleInject, key => string.Equals(key, nameof(Propagators.ContextPropagators.Names.W3C), StringComparison.OrdinalIgnoreCase)))
                {
                    PropagationStyleInject = PropagationStyleInject.Concat(nameof(Propagators.ContextPropagators.Names.W3C));
                }
            }

            DebuggerSettings = new DebuggerSettings(source);
        }

        /// <summary>
        /// Gets or sets the default environment name applied to all spans.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.Environment"/>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the service name applied to top-level spans and used to build derived service names.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.ServiceName"/>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the version tag applied to all spans.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.ServiceVersion"/>
        public string ServiceVersion { get; set; }

        /// <summary>
        /// Gets or sets a value with the SignalFx access token. This is to be used when sending data
        /// directly to ingestion URL, ie.: no agent or collector is being used.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.SignalFxAccessToken"/>
        public string SignalFxAccessToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tracing is enabled.
        /// Default is <c>true</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TraceEnabled"/>
        public bool TraceEnabled { get; set; }

        /// <summary>
        /// Gets or sets the names of disabled integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.DisabledIntegrations"/>
        public HashSet<string> DisabledIntegrationNames { get; set; }

        /// <summary>
        /// Gets or sets the transport settings that dictate how the tracer connects to the agent.
        /// </summary>
        public ExporterSettings ExporterSettings { get; set; }

        /// <summary>
        /// Gets or sets the key used to determine the transport for sending traces.
        /// Default is <c>null</c>, which will use the default path decided in <see cref="Agent.Api"/>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TracesTransport"/>
        public string TracesTransport { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether default Analytics are enabled.
        /// Settings this value is a shortcut for setting
        /// <see cref="Configuration.IntegrationSettings.AnalyticsEnabled"/> on some predetermined integrations.
        /// See the documentation for more details.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.GlobalAnalyticsEnabled"/>
        [Obsolete(DeprecationMessages.AppAnalytics)]
        public bool AnalyticsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether correlation identifiers are
        /// automatically injected into the logging context.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.LogsInjectionEnabled"/>
        public bool LogsInjectionEnabled
        {
            get => LogSubmissionSettings?.LogsInjectionEnabled ?? false;
            set => LogSubmissionSettings.LogsInjectionEnabled = value;
        }

        /// <summary>
        /// Gets or sets a value indicating the maximum number of traces set to AutoKeep (p1) per second.
        /// Default is <c>100</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TraceRateLimit"/>
        public int MaxTracesSubmittedPerSecond { get; set; }

        /// <summary>
        /// Gets or sets a value indicating custom sampling rules.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.CustomSamplingRules"/>
        public string CustomSamplingRules { get; set; }

        /// <summary>
        /// Gets or sets a value indicating a global rate for sampling.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.GlobalSamplingRate"/>
        public double? GlobalSamplingRate { get; set; }

        /// <summary>
        /// Gets a collection of <see cref="Integrations"/> keyed by integration name.
        /// </summary>
        public IntegrationSettingsCollection Integrations { get; }

        /// <summary>
        /// Gets a collection of <see cref="MetricsIntegrations"/> keyed by metric integration name.
        /// </summary>
        public MetricsIntegrationSettingsCollection MetricsIntegrations { get; }

        /// <summary>
        /// Gets or sets the global tags, which are applied to all <see cref="Span"/>s.
        /// </summary>
        public IDictionary<string, string> GlobalTags { get; set; }

        /// <summary>
        /// Gets or sets the map of header keys to tag names, which are applied to the root <see cref="Span"/>
        /// of incoming and outgoing HTTP requests.
        /// </summary>
        public IDictionary<string, string> HeaderTags { get; set; }

        /// <summary>
        /// Gets or sets a custom request header configured to read the ip from. For backward compatibility, it fallbacks on DD_APPSEC_IPHEADER
        /// </summary>
        internal string IpHeader { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ip header should not be collected. The default is false.
        /// </summary>
        internal bool IpHeaderDisabled { get; set; }

        /// <summary>
        /// Gets or sets the map of metadata keys to tag names, which are applied to the root <see cref="Span"/>
        /// of incoming and outgoing GRPC requests.
        /// </summary>
        public IDictionary<string, string> GrpcTags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether internal metrics
        /// are enabled and sent to DogStatsd.
        /// </summary>
        public bool TracerMetricsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Redis integrations
        /// should tag commands as db.statement.
        /// Default is <c>true</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TagRedisCommands"/>
        public bool TagRedisCommands { get; set; }

        /// <summary>
        /// Gets or sets the name of the exporter to be used. The Tracer uses it to encode and
        /// dispatch traces.S
        /// Default is <c>"Zipkin"</c>.
        /// <seealso cref="ConfigurationKeys.Exporter"/>
        /// </summary>
        public ExporterType Exporter { get; set; }

        /// <summary>
        /// Gets or sets the name of the semantic convention to be used.
        /// The Tracer uses it to define operation names, span tags, statuses etc.
        /// Default is <c>"Default"</c>.
        /// <seealso cref="ConfigurationKeys.Convention"/>
        /// </summary>
        public ConventionType Convention { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether stats are computed on the tracer side
        /// </summary>
        public bool StatsComputationEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the use
        /// of System.Diagnostics.DiagnosticSource is enabled.
        /// Default is <c>true</c>.
        /// </summary>
        /// <remark>
        /// This value cannot be set in code. Instead,
        /// set it using the <c>SIGNALFX_TRACE_DIAGNOSTIC_SOURCE_ENABLED</c>
        /// environment variable or in configuration files.
        /// </remark>
        public bool DiagnosticSourceEnabled
        {
            get => GlobalSettings.Source.DiagnosticSourceEnabled;
            set { }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a span context should be created on exiting a successful Kafka
        /// Consumer.Consume() call, and closed on entering Consumer.Consume().
        /// </summary>
        /// <seealso cref="ConfigurationKeys.KafkaCreateConsumerScopeEnabled"/>
        public bool KafkaCreateConsumerScopeEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable the updated WCF instrumentation that delays execution
        /// until later in the WCF pipeline when the WCF server exception handling is established.
        /// </summary>
        internal bool DelayWcfInstrumentationEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the regex to apply to obfuscate http query strings.
        /// </summary>
        internal string ObfuscationQueryStringRegex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not http.url should contain the query string, enabled by default
        /// </summary>
        internal bool QueryStringReportingEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating a timeout in milliseconds to the execution of the query string obfuscation regex
        /// Default value is 100ms
        /// </summary>
        internal double ObfuscationQueryStringRegexTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the diagnostic log at startup is enabled
        /// </summary>
        public bool StartupDiagnosticLogEnabled { get; set; }

        /// <summary>
        /// Gets or sets the maximum length of an outgoing propagation header's value ("x-datadog-tags")
        /// when injecting it into downstream service calls.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TagPropagation.HeaderMaxLength"/>
        /// <remarks>
        /// This value is not used when extracting an incoming propagation header from an upstream service.
        /// </remarks>
        internal int TagPropagationHeaderMaxLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the injection propagation style.
        /// </summary>
        internal string[] PropagationStyleInject { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the extraction propagation style.
        /// </summary>
        internal string[] PropagationStyleExtract { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether runtime metrics
        /// are enabled and sent to DogStatsd.
        /// </summary>
        public bool TraceResponseHeaderEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether MongoDb integration
        /// should tag the command BsonDocument as db.statement.
        /// Default is <c>true</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TagMongoCommands"/>
        public bool TagMongoCommands { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Elasticsearch integration
        /// should tag PostData as db.statement.
        /// Default is <c>true</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TagElasticsearchQueries"/>
        public bool TagElasticsearchQueries { get; set; }

        /// <summary>
        /// Gets or sets a value with the maximum length a tag/log value can have.
        /// Values are completely truncated when set to 0, and ignored when set to negative
        /// or non-integer string. The default value is 12000.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.RecordedValueMaxLength"/>
        public int RecordedValueMaxLength { get; set; }

        /// <summary>
        /// Gets or sets the comma separated list of url patterns to skip tracing.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpClientExcludedUrlSubstrings"/>
        internal string[] HttpClientExcludedUrlSubstrings { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code that should be marked as errors for server integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpServerErrorStatusCodes"/>
        internal bool[] HttpServerErrorStatusCodes { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code that should be marked as errors for client integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpClientErrorStatusCodes"/>
        internal bool[] HttpClientErrorStatusCodes { get; set; }

        /// <summary>
        /// Gets configuration values for changing service names based on configuration
        /// </summary>
        internal ServiceNames ServiceNameMappings { get; }

        /// <summary>
        /// Gets or sets a value indicating the batch interval for the serialization queue, in milliseconds
        /// </summary>
        internal int TraceBatchInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the cpu profiling is enabled.
        /// The default value is false (disabled)
        /// </summary>
        /// <seealso cref="ConfigurationKeys.AlwaysOnProfiler.CpuEnabled"/>
        internal bool CpuProfilingEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the allocation profiling is enabled.
        /// The default value is false (disabled)
        /// </summary>
        /// <seealso cref="ConfigurationKeys.AlwaysOnProfiler.MemoryEnabled"/>
        internal bool MemoryProfilingEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value for the thread sampling period.
        /// The default value is 1000 milliseconds.
        /// </summary>
        /// <seealso cref="TracerSettings.ThreadSamplingPeriod"/>
        internal TimeSpan ThreadSamplingPeriod { get; set; }

        /// <summary>
        /// Gets or sets a value for the profiling data export interval.
        /// The default value is 1000 milliseconds.
        /// If CPU profiling is enables this value should match ThreadSamplingPeriod.
        /// </summary>
        /// <seealso cref="TracerSettings.ProfilerExportInterval"/>
        /// <seealso cref="TracerSettings.ThreadSamplingPeriod"/>
        internal TimeSpan ProfilerExportInterval { get; set; }

        /// <summary>
        /// Gets a value indicating whether the feature flag to enable the updated ASP.NET resource names is enabled
        /// </summary>
        /// <seealso cref="ConfigurationKeys.FeatureFlags.RouteTemplateResourceNamesEnabled"/>
        internal bool RouteTemplateResourceNamesEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether resource names for ASP.NET and ASP.NET Core spans should be expanded. Only applies
        /// when <see cref="RouteTemplateResourceNamesEnabled"/> is <code>true</code>.
        /// </summary>
        internal bool ExpandRouteTemplatesEnabled { get; }

        /// <summary>
        /// Gets or sets the debugger settings.
        /// </summary>
        internal DebuggerSettings DebuggerSettings { get; set; }

        /// <summary>
        /// Gets or sets the direct log submission settings.
        /// </summary>
        internal DirectLogSubmissionSettings LogSubmissionSettings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the trace methods configuration.
        /// </summary>
        internal string TraceMethods { get; set; }

        /// <summary>
        /// Gets a value indicating whether the activity listener is enabled or not.
        /// </summary>
        internal bool IsActivityListenerEnabled { get; }

        /// <summary>
        /// Create a <see cref="TracerSettings"/> populated from the default sources
        /// returned by <see cref="CreateDefaultConfigurationSource"/>.
        /// </summary>
        /// <returns>A <see cref="TracerSettings"/> populated from the default sources.</returns>
        public static TracerSettings FromDefaultSources()
        {
            var source = CreateDefaultConfigurationSource();
            return new TracerSettings(source);
        }

        /// <summary>
        /// Creates a <see cref="IConfigurationSource"/> by combining environment variables,
        /// AppSettings where available, and a local datadog.json file, if present.
        /// </summary>
        /// <returns>A new <see cref="IConfigurationSource"/> instance.</returns>
        public static CompositeConfigurationSource CreateDefaultConfigurationSource()
        {
            return GlobalSettings.CreateDefaultConfigurationSource();
        }

        /// <summary>
        /// Sets the HTTP status code that should be marked as errors for client integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpClientErrorStatusCodes"/>
        /// <param name="statusCodes">Status codes that should be marked as errors</param>
        public void SetHttpClientErrorStatusCodes(IEnumerable<int> statusCodes)
        {
            HttpClientErrorStatusCodes = ParseHttpCodesToArray(string.Join(",", statusCodes));
        }

        /// <summary>
        /// Sets the HTTP status code that should be marked as errors for server integrations.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.HttpServerErrorStatusCodes"/>
        /// <param name="statusCodes">Status codes that should be marked as errors</param>
        public void SetHttpServerErrorStatusCodes(IEnumerable<int> statusCodes)
        {
            HttpServerErrorStatusCodes = ParseHttpCodesToArray(string.Join(",", statusCodes));
        }

        /// <summary>
        /// Sets the mappings to use for service names within a <see cref="Span"/>
        /// </summary>
        /// <param name="mappings">Mappings to use from original service name (e.g. <code>mssql</code> or <code>graphql</code>)
        /// as the <see cref="KeyValuePair{TKey, TValue}.Key"/>) to replacement service names as <see cref="KeyValuePair{TKey, TValue}.Value"/>).</param>
        public void SetServiceNameMappings(IEnumerable<KeyValuePair<string, string>> mappings)
        {
            ServiceNameMappings.SetServiceNameMappings(mappings);
        }

        /// <summary>
        /// Create an instance of <see cref="ImmutableTracerSettings"/> that can be used to build a <see cref="Tracer"/>
        /// </summary>
        /// <returns>The <see cref="ImmutableTracerSettings"/> that can be passed to a <see cref="Tracer"/> instance</returns>
        public ImmutableTracerSettings Build()
        {
            return new ImmutableTracerSettings(this);
        }

        private static IDictionary<string, string> InitializeHeaderTags(IDictionary<string, string> configurationDictionary, bool headerTagsNormalizationFixEnabled)
        {
            var headerTags = new Dictionary<string, string>();

            foreach (var kvp in configurationDictionary)
            {
                var headerName = kvp.Key;
                var providedTagName = kvp.Value;
                if (string.IsNullOrWhiteSpace(headerName))
                {
                    continue;
                }

                // The user has not provided a tag name. The normalization will happen later, when adding the prefix.
                if (string.IsNullOrEmpty(providedTagName))
                {
                    headerTags.Add(headerName.Trim(), string.Empty);
                }
                else if (headerTagsNormalizationFixEnabled && providedTagName.TryConvertToNormalizedTagName(normalizePeriods: false, out var normalizedTagName))
                {
                    // If the user has provided a tag name, then we don't normalize periods in the provided tag name
                    headerTags.Add(headerName.Trim(), normalizedTagName);
                }
                else if (!headerTagsNormalizationFixEnabled && providedTagName.TryConvertToNormalizedTagName(normalizePeriods: true, out var normalizedTagNameNoPeriods))
                {
                    // Back to the previous behaviour if the flag is set
                    headerTags.Add(headerName.Trim(), normalizedTagNameNoPeriods);
                }
            }

            return headerTags;
        }

        // internal for testing
        internal static IEnumerable<string> TrimSplitString(string textValues, char separator)
        {
            var values = textValues.Split(separator);

            for (var i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    yield return values[i].Trim();
                }
            }
        }

        internal static bool[] ParseHttpCodesToArray(string httpStatusErrorCodes)
        {
            bool[] httpErrorCodesArray = new bool[600];

            void TrySetValue(int index)
            {
                if (index >= 0 && index < httpErrorCodesArray.Length)
                {
                    httpErrorCodesArray[index] = true;
                }
            }

            string[] configurationsArray = httpStatusErrorCodes.Replace(" ", string.Empty).Split(',');

            foreach (string statusConfiguration in configurationsArray)
            {
                int startStatus;

                // Checks that the value about to be used follows the `401-404` structure or single 3 digit number i.e. `401` else log the warning
                if (!Regex.IsMatch(statusConfiguration, @"^\d{3}-\d{3}$|^\d{3}$"))
                {
                    Log.Warning("Wrong format '{0}' for SIGNALFX_HTTP_SERVER/CLIENT_ERROR_STATUSES configuration.", statusConfiguration);
                }

                // If statusConfiguration equals a single value i.e. `401` parse the value and save to the array
                else if (int.TryParse(statusConfiguration, out startStatus))
                {
                    TrySetValue(startStatus);
                }
                else
                {
                    string[] statusCodeLimitsRange = statusConfiguration.Split('-');

                    startStatus = int.Parse(statusCodeLimitsRange[0]);
                    int endStatus = int.Parse(statusCodeLimitsRange[1]);

                    if (endStatus < startStatus)
                    {
                        startStatus = endStatus;
                        endStatus = int.Parse(statusCodeLimitsRange[0]);
                    }

                    for (int statusCode = startStatus; statusCode <= endStatus; statusCode++)
                    {
                        TrySetValue(statusCode);
                    }
                }
            }

            return httpErrorCodesArray;
        }

        private static TimeSpan GetThreadSamplingPeriod(IConfigurationSource source)
        {
            // If you change any of these constants, check with always_on_profiler.cpp first
            int period = source.SafeReadInt32(
                key: ConfigurationKeys.AlwaysOnProfiler.ThreadSamplingPeriod,
                defaultTo: 10_000,
                validators: (value) => value >= 1_000);

            return TimeSpan.FromMilliseconds(period);
        }
    }
}
