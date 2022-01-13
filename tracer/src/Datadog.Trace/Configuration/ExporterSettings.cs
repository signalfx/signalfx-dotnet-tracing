// <copyright file="ExporterSettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using Datadog.Trace.Vendors.StatsdClient.Transport;

namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Contains exporter settings.
    /// </summary>
    public class ExporterSettings
    {
        private int _partialFlushMinSpans;

        /// <summary>
        /// The default host value for <see cref="AgentUri"/>.
        /// </summary>
        public const string DefaultAgentHost = "localhost";

        /// <summary>
        /// The default port value for <see cref="AgentUri"/>.
        /// </summary>
        public const int DefaultAgentPort = 9411;

        /// <summary>
        /// The default port value for dogstatsd.
        /// </summary>
        internal const int DefaultDogstatsdPort = 8125;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExporterSettings"/> class with default values.
        /// </summary>
        public ExporterSettings()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExporterSettings"/> class
        /// using the specified <see cref="IConfigurationSource"/> to initialize values.
        /// </summary>
        /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
        public ExporterSettings(IConfigurationSource source)
        {
            var isWindows = FrameworkDescription.Instance.OSPlatform == OSPlatform.Windows;
            ConfigureTraceTransport(source, isWindows);
            ConfigureMetricsTransport(source, isWindows);
            ConfigureLogsTransport(source);

            PartialFlushEnabled = source?.GetBool(ConfigurationKeys.PartialFlushEnabled)
                // default value
                ?? false;

            var partialFlushMinSpans = source?.GetInt32(ConfigurationKeys.PartialFlushMinSpans);

            if ((partialFlushMinSpans ?? 0) <= 0)
            {
                PartialFlushMinSpans = 500;
            }
        }

        /// <summary>
        /// Gets or sets the Uri where the Tracer can connect to the Agent.
        /// Default is <c>"http://localhost:9411/api/v2/spans"</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.AgentHost"/>
        /// <seealso cref="ConfigurationKeys.AgentPort"/>
        public Uri AgentUri { get; set; }

        /// <summary>
        /// Gets or sets the windows pipe name where the Tracer can connect to the Agent.
        /// Default is <c>null</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TracesPipeName"/>
        public string TracesPipeName { get; set; }

        /// <summary>
        /// Gets or sets the Uri where the Tracer can send metrics.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.MetricsEndpointUrl"/>
        public Uri MetricsEndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the Uri where the logs are exported.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.LogsEndpointUrl"/>
        public Uri LogsEndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the timeout in milliseconds for the windows named pipe requests.
        /// Default is <c>100</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TracesPipeTimeoutMs"/>
        public int TracesPipeTimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the windows pipe name where the Tracer can send stats.
        /// Default is <c>null</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.MetricsPipeName"/>
        public string MetricsPipeName { get; set; }

        /// <summary>
        /// Gets or sets the port where the DogStatsd server is listening for connections.
        /// Default is <c>8125</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.DogStatsdPort"/>
        public int DogStatsdPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether partial flush is enabled
        /// </summary>
        public bool PartialFlushEnabled { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of closed spans in a trace before it's partially flushed
        /// </summary>
        public int PartialFlushMinSpans
        {
            get => _partialFlushMinSpans;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("The value must be strictly greater than 0", nameof(PartialFlushMinSpans));
                }

                _partialFlushMinSpans = value;
            }
        }

        /// <summary>
        /// Gets or sets the transport used to send traces to the Agent.
        /// </summary>
        internal TracesTransportType TracesTransport { get; set; }

        /// <summary>
        /// Gets or sets the transport used to connect to the DogStatsD.
        /// Default is <c>TransportStrategy.Tcp</c>.
        /// </summary>
        internal TransportType MetricsTransport { get; set; }

        private void ConfigureMetricsTransport(IConfigurationSource source, bool isWindows)
        {
            var metricsTransport = TransportType.UDP; // default

            var dogStatsdPort = source?.GetInt32(ConfigurationKeys.DogStatsdPort);

            // Agent port is set to zero in places like AAS where it's needed to prevent port conflict
            // The agent will fail to start if it can not bind a port
            if (dogStatsdPort == 0)
            {
                MetricsPipeName = source?.GetString(ConfigurationKeys.MetricsPipeName);

                if (MetricsPipeName != null)
                {
                    metricsTransport = TransportType.NamedPipe;
                }
            }
            else
            {
                DogStatsdPort = dogStatsdPort ?? DefaultDogstatsdPort;
            }

            MetricsTransport = metricsTransport;

            var metricsEndpointUrl = source?.GetString(ConfigurationKeys.MetricsEndpointUrl) ??
                             // default value
                             "http://localhost:9943/v2/datapoint";
            MetricsEndpointUrl = new Uri(metricsEndpointUrl);
        }

        private void ConfigureLogsTransport(IConfigurationSource source)
        {
            var logsEndpointUrl = source?.GetString(ConfigurationKeys.LogsEndpointUrl) ??
                                  // default value
                                  "http://localhost:4318/v1/logs";
            LogsEndpointUrl = new Uri(logsEndpointUrl);
        }

        private void ConfigureTraceTransport(IConfigurationSource source, bool isWindows)
        {
            TracesTransportType? traceTransport = null;

            var agentHost = source?.GetString(ConfigurationKeys.AgentHost) ??
                            // backwards compatibility for names used in the past
                            source?.GetString("SIGNALFX_TRACE_AGENT_HOSTNAME") ??
                            // default value
                            DefaultAgentHost;

            var agentPort = source?.GetInt32(ConfigurationKeys.AgentPort) ??
                            // backwards compatibility for names used in the past
                            source?.GetInt32("SIGNALFX_TRACE_AGENT_PORT") ??
                            // default value
                            DefaultAgentPort;

            var agentUri = source?.GetString(ConfigurationKeys.EndpointUrl) ??
                           // default value
                           $"http://{agentHost}:{agentPort}/api/v2/spans";

            AgentUri = new Uri(agentUri);

            if (string.Equals(AgentUri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Replace localhost with 127.0.0.1 to avoid DNS resolution.
                // When ipv6 is enabled, localhost is first resolved to ::1, which fails
                // because the trace agent is only bound to ipv4.
                // This causes delays when sending traces.
                var builder = new UriBuilder(agentUri) { Host = "127.0.0.1" };
                AgentUri = builder.Uri;
            }

            // Agent port is set to zero in places like AAS where it's needed to prevent port conflict
            // The agent will fail to start if it can not bind a port
            var hasExplicitTcpConfig = agentPort != 0 && agentHost != null;

            if (hasExplicitTcpConfig)
            {
                // If there is any explicit configuration for TCP, prioritize it
                traceTransport = TracesTransportType.Default;
            }
            else if (isWindows)
            {
                // Check for explicit windows named pipe config
                TracesPipeName = source?.GetString(ConfigurationKeys.TracesPipeName);

                if (TracesPipeName != null)
                {
                    traceTransport = TracesTransportType.WindowsNamedPipe;
                }

                TracesPipeTimeoutMs = source?.GetInt32(ConfigurationKeys.TracesPipeTimeoutMs)
#if DEBUG
                    ?? 20_000;
#else
                    ?? 500;
#endif
            }

            TracesTransport = traceTransport ?? TracesTransportType.Default;
        }
    }
}
