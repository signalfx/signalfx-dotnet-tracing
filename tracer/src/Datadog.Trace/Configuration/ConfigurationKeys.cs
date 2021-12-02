// <copyright file="ConfigurationKeys.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// String constants for standard Datadog configuration keys.
    /// </summary>
    public static class ConfigurationKeys
    {
        /// <summary>
        /// Configuration key for the path to the configuration file.
        /// Can only be set with an environment variable
        /// or in the <c>app.config</c>/<c>web.config</c> file.
        /// </summary>
        public const string ConfigurationFileName = "SIGNALFX_TRACE_CONFIG_FILE";

        /// <summary>
        /// Configuration key for the path to the plugins configuration file.
        /// </summary>
        public const string PluginConfigurationFileName = "SIGNALFX_DOTNET_TRACER_PLUGINS_FILE";

        /// <summary>
        /// Configuration key for the application's environment. Sets the "deployment.environment" tag on every <see cref="Span"/>.
        /// </summary>
        /// <seealso cref="TracerSettings.Environment"/>
        public const string Environment = "SIGNALFX_ENV";

        /// <summary>
        /// Configuration key for the application's default service name.
        /// Used as the service name for top-level spans,
        /// and used to determine service name of some child spans.
        /// </summary>
        /// <seealso cref="TracerSettings.ServiceName"/>
        public const string ServiceName = "SIGNALFX_SERVICE";

        /// <summary>
        /// Configuration key for the application's version. Sets the "version" tag on every <see cref="Span"/>.
        /// </summary>
        /// <seealso cref="TracerSettings.ServiceVersion"/>
        public const string ServiceVersion = "SIGNALFX_VERSION";

        /// <summary>
        /// Configuration key to set the SignalFx access token. This is to be used when sending data
        /// directly to ingestion URL, ie.: no agent or collector is being used.
        /// </summary>
        /// <seealso cref="TracerSettings.SignalFxAccessToken"/>
        public const string SignalFxAccessToken = "SIGNALFX_ACCESS_TOKEN";

        /// <summary>
        /// Configuration key for enabling or disabling the Tracer.
        /// Default is value is true (enabled).
        /// </summary>
        /// <seealso cref="TracerSettings.TraceEnabled"/>
        public const string TraceEnabled = "SIGNALFX_TRACE_ENABLED";

        /// <summary>
        /// Configuration key for enabling or disabling the AppSec.
        /// Default is value is false (disabled).
        /// </summary>
        public const string AppSecEnabled = "SIGNALFX_APPSEC_ENABLED";

        /// <summary>
        /// Configuration key for enabling or disabling blocking in AppSec.
        /// Default is value is false (disabled).
        /// </summary>
        public const string AppSecBlockingEnabled = "SIGNALFX_APPSEC_BLOCKING_ENABLED";

        /// <summary>
        /// Override the default rules file provided. Must be a path to a valid JSON rules file.
        /// Default is value is null (do not override).
        /// </summary>
        public const string AppSecRules = "SIGNALFX_APPSEC_RULES";

        /// <summary>
        /// Configuration key indicating the optional name of the custom header to take into account for the ip address.
        /// Default is value is null (do not override).
        /// </summary>
        public const string AppSecCustomIpHeader = "SIGNALFX_APPSEC_IPHEADER";

        /// <summary>
        /// Comma separated keys indicating the optional custom headers the user wants to send.
        /// Default is value is null.
        /// </summary>
        public const string AppSecExtraHeaders = "SIGNALFX_APPSEC_EXTRA_HEADERS";

        /// <summary>
        /// Configuration key for enabling or disabling the Tracer's debug mode.
        /// Default is value is false (disabled).
        /// </summary>
        /// <seealso cref="TracerSettings.DebugEnabled"/>
        public const string DebugEnabled = "SIGNALFX_TRACE_DEBUG";

        /// <summary>
        /// Gets a value indicating whether file log is enabled.
        /// Default is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Not exposed via <see cref="TracerSettings"/> since the logger
        /// is created before it is set.
        /// </remarks>
        /// <seealso cref="GlobalSettings.FileLogEnabled"/>
        public const string FileLogEnabled = "SIGNALFX_FILE_LOG_ENABLED";

        /// <summary>
        /// Gets a value indicating whether stdout log is enabled.
        /// Default is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// Not exposed via <see cref="TracerSettings"/> since the logger
        /// is created before it is set.
        /// </remarks>
        /// <seealso cref="GlobalSettings.StdoutLogEnabled"/>
        public const string StdoutLogEnabled = "SIGNALFX_STDOUT_LOG_ENABLED";

        /// <summary>
        /// Allows to override the default output template used with stdout logging.
        /// It is ignored if stdout log is disabled.
        /// </summary>
        public const string StdoutLogTemplate = "SIGNALFX_STDOUT_LOG_TEMPLATE";

        /// <summary>
        /// Configuration key for a list of integrations to disable. All other integrations remain enabled.
        /// Default is empty (all integrations are enabled).
        /// Supports multiple values separated with comma.
        /// </summary>
        /// <seealso cref="TracerSettings.DisabledIntegrationNames"/>
        public const string DisabledIntegrations = "SIGNALFX_DISABLED_INTEGRATIONS";

        /// <summary>
        /// Configuration key for a list of AdoNet types that will be excluded from automatic instrumentation.
        /// Default is empty (all AdoNet types are included in automatic instrumentation).
        /// Supports multiple values separated with comma.
        /// </summary>
        /// <seealso cref="TracerSettings.AdoNetExcludedTypes"/>
        public const string AdoNetExcludedTypes = "SIGNALFX_TRACE_ADONET_EXCLUDED_TYPES";

        /// <summary>
        /// Configuration key for the Agent host where the Tracer can send traces.
        /// Overridden by <see cref="AgentUri"/> if present.
        /// Default value is "localhost".
        /// </summary>
        /// <seealso cref="TracerSettings.AgentUri"/>
        public const string AgentHost = "SIGNALFX_AGENT_HOST";

        /// <summary>
        /// Configuration key for the Agent port where the Tracer can send traces.
        /// Default value is 8126.
        /// </summary>
        /// <seealso cref="TracerSettings.AgentUri"/>
        public const string AgentPort = "SIGNALFX_TRACE_AGENT_PORT";

        /// <summary>
        /// Configuration key for the named pipe where the Tracer can send traces.
        /// Default value is <c>null</c>.
        /// </summary>
        /// <seealso cref="TracerSettings.TracesPipeName"/>
        public const string TracesPipeName = "SIGNALFX_TRACE_PIPE_NAME";

        /// <summary>
        /// Configuration key for setting the timeout in milliseconds for named pipes communication.
        /// Default value is <c>0</c>.
        /// </summary>
        /// <seealso cref="TracerSettings.TracesPipeTimeoutMs"/>
        public const string TracesPipeTimeoutMs = "SIGNALFX_TRACE_PIPE_TIMEOUT_MS";

        /// <summary>
        /// Configuration key for the named pipe that DogStatsD binds to.
        /// Default value is <c>null</c>.
        /// </summary>
        /// <seealso cref="TracerSettings.MetricsPipeName"/>
        public const string MetricsPipeName = "SIGNALFX_DOGSTATSD_PIPE_NAME";

        /// <summary>
        /// Sibling setting for <see cref="AgentPort"/>.
        /// Used to force a specific port binding for the Trace Agent.
        /// Default value is 8126.
        /// </summary>
        /// <seealso cref="TracerSettings.AgentUri"/>
        public const string TraceAgentPortKey = "SIGNALFX_APM_RECEIVER_PORT";

        /// <summary>
        /// Configuration key for the Agent URL where the Tracer can send traces.
        /// Overrides values in <see cref="AgentHost"/> and <see cref="AgentPort"/> if present.
        /// Default value is "http://localhost:8126".
        /// </summary>
        /// <seealso cref="TracerSettings.AgentUri"/>
        public const string AgentUri = "SIGNALFX_TRACE_AGENT_URL";

        /// <summary>
        /// Configuration key for the trace endpoint. Same as <see creg="AgentUri"/> created
        /// for compatibility of previous version of SignalFx .NET Tracing.
        /// </summary>
        /// <seealso cref="TracerSettings.AgentUri"/>
        public const string EndpointUrl = "SIGNALFX_ENDPOINT_URL";

        /// <summary>
        /// Configuration key for enabling or disabling default Analytics.
        /// </summary>
        /// <seealso cref="TracerSettings.AnalyticsEnabled"/>
        public const string GlobalAnalyticsEnabled = "SIGNALFX_TRACE_ANALYTICS_ENABLED";

        /// <summary>
        /// Configuration key for a list of tags to be applied globally to spans.
        /// </summary>
        /// <seealso cref="TracerSettings.GlobalTags"/>
        public const string GlobalTags = "SIGNALFX_TAGS";

        /// <summary>
        /// Configuration key for a map of header keys to tag names.
        /// Automatically apply header values as tags on traces.
        /// </summary>
        /// <seealso cref="TracerSettings.HeaderTags"/>
        public const string HeaderTags = "SIGNALFX_TRACE_HEADER_TAGS";

        /// <summary>
        /// Configuration key for a map of services to rename.
        /// </summary>
        /// <seealso cref="TracerSettings.ServiceNameMappings"/>
        public const string ServiceNameMappings = "SIGNALFX_TRACE_SERVICE_MAPPING";

        /// <summary>
        /// Configuration key for setting the size in bytes of the trace buffer
        /// </summary>
        public const string BufferSize = "SIGNALFX_TRACE_BUFFER_SIZE";

        /// <summary>
        /// Configuration key for setting the batch interval in milliseconds for the serialization queue
        /// </summary>
        public const string SerializationBatchInterval = "SIGNALFX_TRACE_BATCH_INTERVAL";

        /// <summary>
        /// Configuration key for enabling or disabling the automatic injection
        /// of correlation identifiers into the logging context.
        /// </summary>
        /// <seealso cref="TracerSettings.LogsInjectionEnabled"/>
        public const string LogsInjectionEnabled = "SIGNALFX_LOGS_INJECTION";

        /// <summary>
        /// Configuration key for setting the number of traces allowed
        /// to be submitted per second.
        /// </summary>
        /// <seealso cref="TracerSettings.MaxTracesSubmittedPerSecond"/>
        public const string MaxTracesSubmittedPerSecond = "SIGNALFX_MAX_TRACES_PER_SECOND";

        /// <summary>
        /// Configuration key for enabling or disabling the diagnostic log at startup
        /// </summary>
        /// <seealso cref="TracerSettings.StartupDiagnosticLogEnabled"/>
        public const string StartupDiagnosticLogEnabled = "SIGNALFX_TRACE_STARTUP_LOGS";

        /// <summary>
        /// Configuration key for setting custom sampling rules based on regular expressions.
        /// Comma separated list of sampling rules.
        /// The rule is matched in order of specification. The first match in a list is used.
        ///
        /// Per entry:
        ///   The item "sample_rate" is required in decimal format.
        ///   The item "service" is optional in regular expression format, to match on service name.
        ///   The item "name" is optional in regular expression format, to match on operation name.
        ///
        /// To give a rate of 50% to any traces in a service starting with the text "cart":
        ///   '[{"sample_rate":0.5, "service":"cart.*"}]'
        ///
        /// To give a rate of 20% to any traces which have an operation name of "http.request":
        ///   '[{"sample_rate":0.2, "name":"http.request"}]'
        ///
        /// To give a rate of 100% to any traces within a service named "background" and with an operation name of "sql.query":
        ///   '[{"sample_rate":1.0, "service":"background", "name":"sql.query"}]
        ///
        /// To give a rate of 10% to all traces
        ///   '[{"sample_rate":0.1}]'
        ///
        /// To configure multiple rules, separate by comma and order from most specific to least specific:
        ///   '[{"sample_rate":0.5, "service":"cart.*"}, {"sample_rate":0.2, "name":"http.request"}, {"sample_rate":1.0, "service":"background", "name":"sql.query"}, {"sample_rate":0.1}]'
        ///
        /// If no rules are specified, or none match, default internal sampling logic will be used.
        /// </summary>
        /// <seealso cref="TracerSettings.CustomSamplingRules"/>
        public const string CustomSamplingRules = "SIGNALFX_TRACE_SAMPLING_RULES";

        /// <summary>
        /// Configuration key for setting the global rate for the sampler.
        /// </summary>
        public const string GlobalSamplingRate = "SIGNALFX_TRACE_SAMPLE_RATE";

        /// <summary>
        /// Configuration key for the DogStatsd port where the Tracer can send metrics.
        /// Default value is 8125.
        /// </summary>
        public const string DogStatsdPort = "SIGNALFX_DOGSTATSD_PORT";

        /// <summary>
        /// Configuration key for enabling or disabling internal metrics sent to DogStatsD.
        /// Default value is <c>false</c> (disabled).
        /// </summary>
        public const string TracerMetricsEnabled = "SIGNALFX_TRACE_METRICS_ENABLED";

        /// <summary>
        /// Configuration key for enabling or disabling runtime metrics sent to DogStatsD.
        /// Default value is <c>false</c> (disabled).
        /// </summary>
        public const string RuntimeMetricsEnabled = "SIGNALFX_RUNTIME_METRICS_ENABLED";

        /// <summary>
        /// Configuration key for enabling or disabling tagging Redis
        /// commands as db.statement.
        /// </summary>
        /// <seealso cref="TracerSettings.TagRedisCommands"/>
        public const string TagRedisCommands = "SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS";

        /// <summary>
        /// Configuration key for setting the approximate maximum size,
        /// in bytes, for Tracer log files.
        /// Default value is 10 MB.
        /// </summary>
        public const string MaxLogFileSize = "SIGNALFX_MAX_LOGFILE_SIZE";

        /// <summary>
        /// Configuration key for setting the number of seconds between,
        /// identical log messages, for Tracer log files.
        /// Default value is 60s. Setting to 0 disables rate limiting.
        /// </summary>
        public const string LogRateLimit = "SIGNALFX_TRACE_LOGGING_RATE";

        /// <summary>
        /// Configuration key for setting the path to the .NET Tracer native log file.
        /// This also determines the output folder of the .NET Tracer managed log files.
        /// Overridden by <see cref="LogDirectory"/> if present.
        /// </summary>
        public const string ProfilerLogPath = "SIGNALFX_TRACE_LOG_PATH";

        /// <summary>
        /// Configuration key for setting the directory of the .NET Tracer logs.
        /// Overrides the value in <see cref="ProfilerLogPath"/> if present.
        /// Default value is "%ProgramData%"\SignalFx .NET Tracing\logs\" on Windows
        /// or "/var/log/signalfx/dotnet/" on Linux.
        /// </summary>
        public const string LogDirectory = "SIGNALFX_TRACE_LOG_DIRECTORY";

        /// <summary>
        /// Configuration key for when a standalone instance of the Trace Agent needs to be started.
        /// </summary>
        public const string TraceAgentPath = "SIGNALFX_TRACE_AGENT_PATH";

        /// <summary>
        /// Configuration key for arguments to pass to the Trace Agent process.
        /// </summary>
        public const string TraceAgentArgs = "SIGNALFX_TRACE_AGENT_ARGS";

        /// <summary>
        /// Configuration key for when a standalone instance of DogStatsD needs to be started.
        /// </summary>
        public const string DogStatsDPath = "SIGNALFX_DOGSTATSD_PATH";

        /// <summary>
        /// Configuration key for arguments to pass to the DogStatsD process.
        /// </summary>
        public const string DogStatsDArgs = "SIGNALFX_DOGSTATSD_ARGS";

        /// <summary>
        /// Configuration key for enabling or disabling the use of System.Diagnostics.DiagnosticSource.
        /// Default value is <c>true</c> (enabled).
        /// </summary>
        public const string DiagnosticSourceEnabled = "SIGNALFX_DIAGNOSTIC_SOURCE_ENABLED";

        /// <summary>
        /// Configuration key for the exporter to be used. The Tracer uses it to encode and
        /// dispatch traces.
        /// Default is <c>"Zipkin"</c>.
        /// </summary>
        /// <seealso cref="TracerSettings.Exporter"/>
        public const string Exporter = "SIGNALFX_EXPORTER";

        /// <summary>
        /// Configuration key for the semantic convention to be used.
        /// The Tracer uses it to define operation names, span tags, statuses etc.
        /// Default is <c>"Default"</c>.
        /// <seealso cref="TracerSettings.Convention"/>
        /// </summary>
        public const string Convention = "SIGNALFX_CONVENTION";

        /// <summary>
        /// Configuration key for the propagators to be used.
        /// Default is <c>B3</c>.
        /// Supports multiple values separated with comma.
        /// <seealso cref="TracerSettings.Propagators"/>
        /// </summary>
        public const string Propagators = "SIGNALFX_PROPAGATORS";

        /// <summary>
        /// Configuration key for setting the API key, used by the Agent.
        /// This key is here for troubleshooting purposes.
        /// </summary>
        public const string ApiKey = "SIGNALFX_API_KEY";

        /// <summary>
        /// Configuration key for overriding the transport to use for communicating with the trace agent.
        /// Default value is <c>null</c>.
        /// Override options available: <c>datadog-tcp</c>, <c>datadog-named-pipes</c>
        /// </summary>
        public const string TracesTransport = "SIGNALFX_TRACE_TRANSPORT";

        /// <summary>
        /// Configuration key for overriding which URLs are skipped by the tracer.
        /// </summary>
        /// <seealso cref="TracerSettings.HttpClientExcludedUrlSubstrings"/>
        public const string HttpClientExcludedUrlSubstrings = "SIGNALFX_TRACE_HTTP_CLIENT_EXCLUDED_URL_SUBSTRINGS";

        /// <summary>
        /// Configuration key for the application's server http statuses to set spans as errors by.
        /// </summary>
        /// <seealso cref="TracerSettings.HttpServerErrorStatusCodes"/>
        public const string HttpServerErrorStatusCodes = "SIGNALFX_HTTP_SERVER_ERROR_STATUSES";

        /// <summary>
        /// Configuration key for the application's client http statuses to set spans as errors by.
        /// </summary>
        /// <seealso cref="TracerSettings.HttpClientErrorStatusCodes"/>
        public const string HttpClientErrorStatusCodes = "SIGNALFX_HTTP_CLIENT_ERROR_STATUSES";

        /// <summary>
        /// Configuration key to enable sending partial traces to the agent
        /// </summary>
        /// <seealso cref="TracerSettings.PartialFlushEnabled"/>
        public const string PartialFlushEnabled = "SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED";

        /// <summary>
        /// Configuration key to set the minimum number of closed spans in a trace before it's partially flushed
        /// </summary>
        /// <seealso cref="TracerSettings.PartialFlushMinSpans"/>
        public const string PartialFlushMinSpans = "SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS";

        /// <summary>
        /// Configuration key to enable or disable the creation of a span context on exiting a successful Kafka
        /// Consumer.Consume() call, and closing the scope on entering Consumer.Consume().
        /// Default value is <c>true</c> (enabled).
        /// </summary>
        /// <seealso cref="TracerSettings.KafkaCreateConsumerScopeEnabled"/>
        public const string KafkaCreateConsumerScopeEnabled = "SIGNALFX_TRACE_KAFKA_CREATE_CONSUMER_SCOPE_ENABLED";

        /// <summary>
        /// Configuration key for enabling splunk context server timing header.
        /// </summary>
        public const string TraceResponseHeaderEnabled = "SIGNALFX_TRACE_RESPONSE_HEADER_ENABLED";

        /// <summary>
        /// Configuration key to set maximum length a tag/log value can have.
        /// Values are completely truncated when set to 0, and ignored when set to negative
        /// or non-integer string. The default value is 12000.
        /// </summary>
        /// <seealso cref="TracerSettings.RecordedValueMaxLength"/>
        public const string RecordedValueMaxLength = "SIGNALFX_RECORDED_VALUE_MAX_LENGTH";

        /// <summary>
        /// Configuration key for enabling or disabling CI Visibility.
        /// Default is value is false (disabled).
        /// </summary>
        public const string CIVisibilityEnabled = "SIGNALFX_CIVISIBILITY_ENABLED";

        /// <summary>
        /// Configuration key for enabling or disabling the tagging of
        /// a Mongo command BsonDocument as db.statement.
        /// Default value is true (enabled).
        /// </summary>
        /// <seealso cref="TracerSettings.TagMongoCommands"/>
        public const string TagMongoCommands = "SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS";

        /// <summary>
        /// Configuration key for enabling or disabling tagging Elasticsearch
        /// PostData as db.statement.
        /// </summary>
        /// <seealso cref="TracerSettings.TagElasticsearchQueries"/>
        public const string TagElasticsearchQueries = "SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES";

        /// <summary>
        /// String format patterns used to match integration-specific configuration keys.
        /// </summary>
        public static class Integrations
        {
            /// <summary>
            /// Configuration key pattern for enabling or disabling an integration.
            /// </summary>
            public const string Enabled = "SIGNALFX_TRACE_{0}_ENABLED";

            /// <summary>
            /// Configuration key pattern for enabling or disabling Analytics in an integration.
            /// </summary>
            public const string AnalyticsEnabled = "SIGNALFX_TRACE_{0}_ANALYTICS_ENABLED";

            /// <summary>
            /// Configuration key pattern for setting Analytics sampling rate in an integration.
            /// </summary>
            public const string AnalyticsSampleRate = "SIGNALFX_TRACE_{0}_ANALYTICS_SAMPLE_RATE";
        }

        /// <summary>
        /// String constants for debug configuration keys.
        /// </summary>
        internal static class Debug
        {
            /// <summary>
            /// Configuration key for forcing the automatic instrumentation to only use the mdToken method lookup mechanism.
            /// </summary>
            public const string ForceMdTokenLookup = "SIGNALFX_TRACE_DEBUG_LOOKUP_MDTOKEN";

            /// <summary>
            /// Configuration key for forcing the automatic instrumentation to only use the fallback method lookup mechanism.
            /// </summary>
            public const string ForceFallbackLookup = "SIGNALFX_TRACE_DEBUG_LOOKUP_FALLBACK";
        }

        internal static class FeatureFlags
        {
            /// <summary>
            /// Feature Flag: enables updated resource names on `aspnet.request`, `aspnet-mvc.request`,
            /// `aspnet-webapi.request`, and `aspnet_core.request` spans. Enables `aspnet_core_mvc.request` spans and
            /// additional features on `aspnet_core.request` spans.
            /// </summary>
            /// <seealso cref="TracerSettings.RouteTemplateResourceNamesEnabled"/>
            public const string RouteTemplateResourceNamesEnabled = "SIGNALFX_TRACE_ROUTE_TEMPLATE_RESOURCE_NAMES_ENABLED";

            /// <summary>
            /// Feature Flag: enables instrumenting calls to netstandard.dll (only applies to CallSite instrumentation)
            /// </summary>
            public const string NetStandardEnabled = "SIGNALFX_TRACE_NETSTANDARD_ENABLED";

            /// <summary>
            /// Configuration key to enable or disable the updated WCF instrumentation that delays execution
            /// until later in the WCF pipeline when the WCF server exception handling is established.
            /// </summary>
            /// <seealso cref="TracerSettings.DelayWcfInstrumentationEnabled"/>
            public const string DelayWcfInstrumentationEnabled = "SIGNALFX_TRACE_DELAY_WCF_INSTRUMENTATION_ENABLED";
        }
    }
}
