// <copyright file="ConfigurationKeys.Exporter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// String constants for standard Datadog configuration keys.
    /// </summary>
    internal static partial class ConfigurationKeys
    {
        /// <summary>
        /// Configuration key for the Agent host where the Tracer can send traces.
        /// Default value is "localhost".
        /// </summary>
        /// <seealso cref="ExporterSettings.AgentUri"/>
        public const string AgentHost = "SIGNALFX_AGENT_HOST";

        /// <summary>
        /// Configuration key for the SignalFx ingest realm, where the Tracer can send telemetry signals.
        /// </summary>
        /// <seealso cref="ExporterSettings.MetricsEndpointUrl"/>
        /// <seealso cref="ExporterSettings.AgentUri"/>
        public const string IngestRealm = "SIGNALFX_REALM";

        /// <summary>
        /// Configuration key for the Agent port where the Tracer can send traces.
        /// Default value is 8126.
        /// </summary>
        /// <seealso cref="ExporterSettings.AgentUri"/>
        public const string AgentPort = "SIGNALFX_TRACE_AGENT_PORT";

        /// <summary>
        /// Configuration key for the named pipe where the Tracer can send traces.
        /// Default value is <c>null</c>.
        /// </summary>
        /// <seealso cref="ExporterSettings.TracesPipeName"/>
        public const string TracesPipeName = "SIGNALFX_TRACE_PIPE_NAME";

        /// <summary>
        /// Configuration key for setting the timeout in milliseconds for named pipes communication.
        /// Default value is <c>0</c>.
        /// </summary>
        /// <seealso cref="ExporterSettings.TracesPipeTimeoutMs"/>
        public const string TracesPipeTimeoutMs = "SIGNALFX_TRACE_PIPE_TIMEOUT_MS";

        /// <summary>
        /// Configuration key for the named pipe that DogStatsD binds to.
        /// Default value is <c>null</c>.
        /// </summary>
        /// <seealso cref="ExporterSettings.MetricsPipeName"/>
        public const string MetricsPipeName = "SIGNALFX_DOGSTATSD_PIPE_NAME";

        /// <summary>
        /// Sibling setting for <see cref="AgentPort"/>.
        /// Used to force a specific port binding for the Trace Agent.
        /// Default value is 8126.
        /// </summary>
        /// <seealso cref="ExporterSettings.AgentUri"/>
        public const string TraceAgentPortKey = "SIGNALFX_APM_RECEIVER_PORT";

        /// <summary>
        /// Configuration key for the URL where the Tracer can send metrics.
        /// </summary>
        /// <seealso cref="ExporterSettings.MetricsEndpointUrl"/>
        public const string MetricsEndpointUrl = "SIGNALFX_METRICS_ENDPOINT_URL";

        /// <summary>
        /// Configuration key for the URL where the logs are exported.
        /// Currently the Thread Sampler sends stack traces as logs.
        /// </summary>
        /// <seealso cref="ExporterSettings.LogsEndpointUrl"/>
        public const string LogsEndpointUrl = "SIGNALFX_PROFILER_LOGS_ENDPOINT";

        /// <summary>
        /// Configuration key for the trace endpoint. Same as <see creg="AgentUri"/> created
        /// for compatibility of previous version of SignalFx .NET Tracing.
        /// </summary>
        public const string EndpointUrl = "SIGNALFX_ENDPOINT_URL";

        /// <summary>
        /// Configuration key for the DogStatsd port where the Tracer can send metrics.
        /// Default value is 8125.
        /// </summary>
        /// <seealso cref="ExporterSettings.DogStatsdPort"/>
        public const string DogStatsdPort = "SIGNALFX_DOGSTATSD_PORT";

        /// <summary>
        /// Configuration key to enable sending partial traces to the agent
        /// </summary>
        /// <seealso cref="ExporterSettings.PartialFlushEnabled"/>
        public const string PartialFlushEnabled = "SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED";

        /// <summary>
        /// Configuration key to set the minimum number of closed spans in a trace before it's partially flushed
        /// </summary>
        /// <seealso cref="ExporterSettings.PartialFlushMinSpans"/>
        public const string PartialFlushMinSpans = "SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS";

        /// <summary>
        /// Configuration key to do synchronous export of traces.
        /// Default is <c>false</c>
        /// </summary>
        public const string TraceSynchExport = "SIGNALFX_TRACE_SYNCH_EXPORT";
    }
}
