// Modified by SignalFx
namespace SignalFx.Tracing.Configuration
{
    /// <summary>
    /// String constants for standard SignalFx configuration keys.
    /// </summary>
    public static class ConfigurationKeys
    {
        /// <summary>
        /// Configuration key for the path to the configuration file.
        /// Can only be set with an environment variable
        /// or in the <c>app.config</c>/<c>web.config</c> file.
        /// </summary>
        public const string ConfigurationFileName = "SIGNALFX_DOTNET_TRACER_CONFIG_FILE";

        /// <summary>
        /// Configuration key for the application's environment. Sets the "environment" tag on every <see cref="Span"/>.
        /// </summary>
        /// <seealso cref="TracerSettings.Environment"/>
        public const string Environment = "SIGNALFX_ENV";

        /// <summary>
        /// Configuration key for the application's default service name.
        /// Used as the service name for top-level spans,
        /// and used to determine service name of some child spans.
        /// </summary>
        /// <seealso cref="TracerSettings.ServiceName"/>
        public const string ServiceName = "SIGNALFX_SERVICE_NAME";

        /// <summary>
        /// Configuration key for the application's to allow spans to specify
        /// different service names then the default one using the "service.name"
        /// OpenTracing semantic convention.
        /// </summary>
        /// <seealso cref="TracerSettings.ServiceName"/>
        public const string ServiceNamePerSpan = "SIGNALFX_SERVICE_NAME_PER_SPAN_ENABLED";

        /// <summary>
        /// Configuration key for enabling or disabling the Tracer.
        /// Default is value is true (enabled).
        /// </summary>
        /// <seealso cref="TracerSettings.TraceEnabled"/>
        public const string TraceEnabled = "SIGNALFX_TRACING_ENABLED";

        /// <summary>
        /// Gets or sets a value indicating whether the operation to send spans is
        /// going to be synchronous when the root span is closed.
        /// Default is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// Typically synchronous sending is not desired but for tests and some
        /// special scenarios it can be useful.
        /// </remarks>
        /// <seealso cref="TracerSettings.SynchronousSend"/>
        public const string SynchronousSend = "SIGNALFX_SYNC_SEND";

        /// <summary>
        /// Configuration key for enabling or disabling the Tracer's debug mode.
        /// Default is value is false (disabled).
        /// </summary>
        /// <seealso cref="TracerSettings.DebugEnabled"/>
        public const string DebugEnabled = "SIGNALFX_TRACE_DEBUG";

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
        /// Configuration key for a list of integrations to disable. All other integrations remain enabled.
        /// Default is empty (all integrations are enabled).
        /// Supports multiple values separated with semi-colons.
        /// </summary>
        /// <seealso cref="TracerSettings.DisabledIntegrationNames"/>
        public const string DisabledIntegrations = "SIGNALFX_DISABLED_INTEGRATIONS";

        /// <summary>
        /// Configuration key for the Agent host where the Tracer can send traces.
        /// Overriden by <see cref="AgentUri"/> if present.
        /// Default value is "localhost".
        /// </summary>
        /// <seealso cref="TracerSettings.AgentUri"/>
        public const string AgentHost = "SIGNALFX_AGENT_HOST";

        /// <summary>
        /// Configuration key for the Agent port where the Tracer can send traces.
        /// Default value is 9080.
        /// </summary>
        /// <seealso cref="TracerSettings.AgentUri"/>
        public const string AgentPort = "SIGNALFX_TRACE_AGENT_PORT";

        /// <summary>
        /// Sibling setting for <see cref="AgentPort"/>.
        /// Used to force a specific port binding for the Trace Agent.
        /// Default value is 8126.
        /// </summary>
        /// <seealso cref="TracerSettings.AgentUri"/>
        public const string TraceAgentPortKey = "DD_APM_RECEIVER_PORT";

        /// <summary>
        /// Configuration key for the Agent URL where the Tracer can send traces.
        /// Overrides values in <see cref="AgentHost"/> and <see cref="AgentPort"/> if present.
        /// Default value is "http://localhost:9080".
        /// </summary>
        /// <seealso cref="TracerSettings.AgentUri"/>
        public const string AgentUri = "SIGNALFX_TRACE_AGENT_URL";

        /// <summary>
        /// Configuration key for the Zipkin Api Collector path.
        /// Default value is "/v1/trace".
        /// </summary>
        /// <seealso cref="TracerSettings.EndpointUrl"/>
        public const string AgentPath = "SIGNALFX_TRACE_AGENT_PATH";

        /// <summary>
        /// Configuration key for the Zipkin API collector endpoint.
        /// Default value is "http://localhost:9080/v1/trace".
        /// </summary>
        /// <seealso cref="TracerSettings.EndpointUrl"/>
        public const string EndpointUrl = "SIGNALFX_ENDPOINT_URL";

        /// <summary>
        /// Configuration key for the formatting API the Tracer uses to encode traces.
        /// Default value is "dd".
        /// </summary>
        /// <seealso cref="TracerSettings.ApiType"/>
        public const string ApiType = "SIGNALFX_API_TYPE";

        /// <summary>
        /// Configuration key for enabling or disabling default Analytics.
        /// </summary>
        /// <seealso cref="TracerSettings.AnalyticsEnabled"/>
        public const string GlobalAnalyticsEnabled = "SIGNALFX_TRACE_ANALYTICS_ENABLED";

        /// <summary>
        /// Configuration key for a list of tags to be applied globally to spans.
        /// </summary>
        public const string GlobalTags = "SIGNALFX_TRACE_GLOBAL_TAGS";

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
        /// Configuration key for setting custom sampling rules based on regular expressions.
        /// Semi-colon separated list of sampling rules.
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
        /// To configure multiple rules, separate by semi-colon and order from most specific to least specific:
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
        /// Configuration key for enabling or disabling the tagging of
        /// a Mongo command BsonDocument as db.statement.
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
        /// Configuration key for enabling or disabling tagging Redis
        /// commands as db.statement.
        /// </summary>
        /// <seealso cref="TracerSettings.TagRedisCommands"/>
        public const string TagRedisCommands = "SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS";

        /// <summary>
        /// Configuration key for disabling sanitizing SQL db.statement.
        /// </summary>
        /// <seealso cref="TracerSettings.SanitizeSqlStatements"/>
        public const string SanitizeSqlStatements = "SIGNALFX_SANITIZE_SQL_STATEMENTS";

        /// <summary>
        /// Comma-separated list of <see cref="System.Diagnostics.DiagnosticListener"/> to subscribe to the
        /// ASP.NET Core instrumentation's observer.
        /// </summary>
        /// <seealso cref="TracerSettings.AdditionalDiagnosticListeners"/>
        public const string AdditionalDiagnosticListeners = "SIGNALFX_INSTRUMENTATION_ASPNETCORE_DIAGNOSTIC_LISTENERS";

        /// <summary>
        /// Configuration key for setting the approximate maximum size,
        /// in bytes, for Tracer log files.
        /// Default value is 10 MB.
        /// </summary>
        public const string MaxLogFileSize = "SIGNALFX_MAX_LOGFILE_SIZE";

        /// <summary>
        /// Configuration key for setting the path to the profiler log file.
        /// Default value is "%ProgramData%"\SignalFx .NET Tracing\logs\dotnet-profiler.log" on Windows
        /// or "/var/log/signalfx/dotnet/dotnet-profiler.log" on Linux.
        /// </summary>
        public const string ProfilerLogPath = "SIGNALFX_TRACE_LOG_PATH";

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
        /// Configuration key for enabling or disabling the use of <see cref="System.Diagnostics.DiagnosticSource"/>.
        /// Default value is <c>true</c> (enabled).
        /// </summary>
        public const string DiagnosticSourceEnabled = "SIGNALFX_DIAGNOSTIC_SOURCE_ENABLED";

        /// <summary>
        /// Configuration key for enabling or disabling appending the absolute URI path to the span name.
        /// Default is value is false (disabled).
        /// </summary>
        /// <seealso cref="TracerSettings.AppendUrlPathToName"/>
        public const string AppendUrlPathToName = "SIGNALFX_APPEND_URL_PATH_TO_NAME";

        /// <summary>
        /// Configuration key to control whether the resource name is going to be used as the span name.
        /// This applies to "AspNetMvc" and "AspNetWebApi" instrumentations.
        /// Default is value is <c>true</c>.
        /// </summary>
        /// <seealso cref="TracerSettings.UseWebServerResourceAsOperationName"/>
        public const string UseWebServerResourceAsOperationName = "SIGNALFX_USE_WEBSERVER_RESOURCE_AS_OPERATION_NAME";

        /// <summary>
        /// Configuration key to control whether the instrumentations creating server spans, eg.:
        /// "AspNetMvc", "AspNetWebApi", etc, are going to try to add the client IP as tag using
        /// the keys "peer.ipv4" or "peer.ipv6" according to the type of client IP (if available).
        /// Default is value is <c>false</c>.
        /// </summary>
        /// <seealso cref="TracerSettings.AddClientIpToServerSpans"/>
        public const string AddClientIpToServerSpans = "SIGNALFX_ADD_CLIENT_IP_TO_SERVER_SPANS";

        /// <summary>
        /// Configuration key to set the SignalFx access token. This is to be used when sending data
        /// directly to ingestion URL, ie.: no agent or collector is being used.
        /// </summary>
        /// <seealso cref="TracerSettings.SignalFxAccessToken"/>
        public const string SignalFxAccessToken = "SIGNALFX_ACCESS_TOKEN";

        /// <summary>
        /// Configuration key to set maximum length a tag/log value can have.
        /// Values are completely truncated when set to 0, and ignored when set to negative
        /// or non-integer string. The default value is 1200.
        /// </summary>
        /// <seealso cref="TracerSettings.RecordedValueMaxLength"/>
        public const string RecordedValueMaxLength = "SIGNALFX_RECORDED_VALUE_MAX_LENGTH";

        /// <summary>
        /// String format patterns used to match integration-specific configuration keys.
        /// </summary>
        public static class Integrations
        {
            /// <summary>
            /// Configuration key pattern for enabling or disabling an integration.
            /// </summary>
            public const string Enabled = "SIGNALFX_{0}_ENABLED";

            /// <summary>
            /// Configuration key pattern for enabling or disabling Analytics in an integration.
            /// </summary>
            public const string AnalyticsEnabled = "SIGNALFX_{0}_ANALYTICS_ENABLED";

            /// <summary>
            /// Configuration key pattern for setting Analytics sampling rate in an integration.
            /// </summary>
            public const string AnalyticsSampleRate = "SIGNALFX_{0}_ANALYTICS_SAMPLE_RATE";
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
    }
}
