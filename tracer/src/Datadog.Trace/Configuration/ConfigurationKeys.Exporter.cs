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
        /// Overridden by <see cref="AgentUri"/> if present.
        /// Default value is "localhost".
        /// </summary>
        /// <seealso cref="ExporterSettings.AgentUri"/>
        public const string AgentHost = "SINGALFX_AGENT_HOST";

        /// <summary>
        /// Configuration key for the Agent port where the Tracer can send traces.
        /// Default value is 8126.
        /// </summary>
        /// <seealso cref="ExporterSettings.AgentUri"/>
        public const string AgentPort = "SINGALFX_TRACE_AGENT_PORT";

        /// <summary>
        /// Configuration key for the named pipe where the Tracer can send traces.
        /// Default value is <c>null</c>.
        /// </summary>
        /// <seealso cref="ExporterSettings.TracesPipeName"/>
        public const string TracesPipeName = "SINGALFX_TRACE_PIPE_NAME";

        /// <summary>
        /// Configuration key for setting the timeout in milliseconds for named pipes communication.
        /// Default value is <c>0</c>.
        /// </summary>
        /// <seealso cref="ExporterSettings.TracesPipeTimeoutMs"/>
        public const string TracesPipeTimeoutMs = "SINGALFX_TRACE_PIPE_TIMEOUT_MS";

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
        public const string TraceAgentPortKey = "SINGALFX_APM_RECEIVER_PORT";

        /// <summary>
        /// Configuration key for the URL where the Tracer can send metrics.
        /// </summary>
        /// <seealso cref="ExporterSettings.MetricsEndpointUrl"/>
        public const string MetricsEndpointUrl = "SIGNALFX_METRICS_ENDPOINT_URL";

        /// <summary>
        /// Configuration key for the Agent URL where the Tracer can send traces.
        /// Overrides values in <see cref="AgentHost"/> and <see cref="AgentPort"/> if present.
        /// Default value is "http://localhost:8126".
        /// </summary>
        /// <seealso cref="ExporterSettings.AgentUri"/>
        public const string AgentUri = "SINGNALFX_TRACE_AGENT_URL";

        /// <summary>
        /// Configuration key for the trace endpoint. Same as <see creg="AgentUri"/> created
        /// for compatibility of previous version of SignalFx .NET Tracing.
        /// </summary>
        /// <seealso cref="AgentUri"/>
        public const string EndpointUrl = "SIGNALFX_ENDPOINT_URL";

        /// <summary>
        /// Configuration key for the DogStatsd port where the Tracer can send metrics.
        /// Default value is 8125.
        /// </summary>
        /// <seealso cref="ExporterSettings.DogStatsdPort"/>
        public const string DogStatsdPort = "SINGALFX_DOGSTATSD_PORT";
    }
}
