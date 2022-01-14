// <copyright file="ExporterSettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.IO;
using Datadog.Trace.Agent;
using MetricsTransportType = Datadog.Trace.Vendors.StatsdClient.Transport.TransportType;

namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Contains exporter settings.
    /// </summary>
    public class ExporterSettings
    {
        private int _partialFlushMinSpans;

        /// <summary>
        /// Allows overriding of file system access for tests.
        /// </summary>
        private Func<string, bool> _fileExists;

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
        internal const int DefaultDogstatsdPort = 9943;

        private const string LocalIngestRealm = "none";

        /// <summary>
        /// Prefix for unix domain sockets.
        /// </summary>
        internal const string UnixDomainSocketPrefix = "unix://";

        /// <summary>
        /// Default traces UDS path.
        /// </summary>
        internal const string DefaultTracesUnixDomainSocket = "/var/run/datadog/apm.socket";

        /// <summary>
        /// Default metrics UDS path.
        /// </summary>
        internal const string DefaultMetricsUnixDomainSocket = "/var/run/datadog/dsd.socket";

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
            : this(source, File.Exists)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExporterSettings"/> class.
        /// Direct use in tests only.
        /// </summary>
        internal ExporterSettings(IConfigurationSource source, Func<string, bool> fileExists)
        {
            _fileExists = fileExists;

            var ingestRealm = source?.GetString(ConfigurationKeys.IngestRealm) ??
                              LocalIngestRealm;

            ConfigureTraceTransport(source, ingestRealm, out var shouldUseUdpForMetrics);
            ConfigureMetricsTransport(source, shouldUseUdpForMetrics, ingestRealm);
            ConfigureLogsTransport(source);

            PartialFlushEnabled = source?.GetBool(ConfigurationKeys.PartialFlushEnabled)
                // default value
                ?? false;

            var partialFlushMinSpans = source?.GetInt32(ConfigurationKeys.PartialFlushMinSpans);

            if ((partialFlushMinSpans ?? 0) <= 0)
            {
                partialFlushMinSpans = 500;
            }

            PartialFlushMinSpans = partialFlushMinSpans.Value;
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
        /// Gets or sets the unix domain socket path where the Tracer can connect to the Agent.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TracesUnixDomainSocketPath"/>
        public string TracesUnixDomainSocketPath { get; set; }

        /// <summary>
        /// Gets or sets the unix domain socket path where the Tracer can send stats.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.MetricsUnixDomainSocketPath"/>
        public string MetricsUnixDomainSocketPath { get; set; }

        /// <summary>
        /// Gets or sets the port where the DogStatsd server is listening for connections.
        /// Default is <c>9943</c>.
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
        internal MetricsTransportType MetricsTransport { get; set; }

        private static string GetConfiguredMetricsEndpoint(string ingestRealm)
        {
            return IsDefaultIngestRealm(ingestRealm) ?
                       // local collector
                       "http://localhost:9943/v2/datapoint" :
                       // direct ingest
                       $"https://ingest.{ingestRealm}.signalfx.com/v2/datapoint";
        }

        private static string GetConfiguredTracesEndpoint(string ingestRealm, int agentPort)
        {
            return IsDefaultIngestRealm(ingestRealm) ?
                       // local collector
                       $"http://localhost:{agentPort}/api/v2/spans" :
                       // direct ingest
                       $"https://ingest.{ingestRealm}.signalfx.com/v2/trace";
        }

        private static bool IsDefaultIngestRealm(string ingestRealm)
        {
            return string.Equals(ingestRealm, LocalIngestRealm, StringComparison.OrdinalIgnoreCase);
        }

        private void ConfigureMetricsTransport(IConfigurationSource source, bool forceMetricsOverUdp, string ingestRealm)
        {
            MetricsTransportType? metricsTransport = null;

            var dogStatsdPort = source?.GetInt32(ConfigurationKeys.DogStatsdPort);

            MetricsPipeName = source?.GetString(ConfigurationKeys.MetricsPipeName);

            // Agent port is set to zero in places like AAS where it's needed to prevent port conflict.
            // The agent will fail to start if it can not bind a port.
            // If the dogstatsd port isn't explicitly configured, check for pipes or sockets.
            if (!forceMetricsOverUdp && (dogStatsdPort == 0 || dogStatsdPort == null))
            {
                if (!string.IsNullOrWhiteSpace(MetricsPipeName))
                {
                    metricsTransport = MetricsTransportType.NamedPipe;
                }
                else
                {
                    // Check for UDS
                    var metricsUnixDomainSocketPath = source?.GetString(ConfigurationKeys.MetricsUnixDomainSocketPath);
                    if (metricsUnixDomainSocketPath != null)
                    {
                        metricsTransport = MetricsTransportType.UDS;
                        MetricsUnixDomainSocketPath = metricsUnixDomainSocketPath;
                    }
                    else if (_fileExists(DefaultMetricsUnixDomainSocket))
                    {
                        metricsTransport = MetricsTransportType.UDS;
                        MetricsUnixDomainSocketPath = DefaultMetricsUnixDomainSocket;
                    }
                }
            }

            if (metricsTransport == null)
            {
                // UDP if nothing explicit was configured or a port is set
                DogStatsdPort = dogStatsdPort ?? DefaultDogstatsdPort;
                metricsTransport = MetricsTransportType.UDP;
            }

            MetricsTransport = metricsTransport.Value;

            var metricsEndpointUrl = source?.GetString(ConfigurationKeys.MetricsEndpointUrl) ??
                                     GetConfiguredMetricsEndpoint(ingestRealm);
            MetricsEndpointUrl = new Uri(metricsEndpointUrl);
        }

        private void ConfigureLogsTransport(IConfigurationSource source)
        {
            var logsEndpointUrl = source?.GetString(ConfigurationKeys.LogsEndpointUrl) ??
                                  // default value
                                  "http://localhost:4318/v1/logs";
            LogsEndpointUrl = new Uri(logsEndpointUrl);
        }

        private void ConfigureTraceTransport(IConfigurationSource source, string ingestRealm, out bool forceMetricsOverUdp)
        {
            // Assume false, as we'll go through typical checks if this is false
            forceMetricsOverUdp = false;

            TracesTransportType? traceTransport = null;

            var agentPort = source?.GetInt32(ConfigurationKeys.AgentPort) ??
                            // default value
                            DefaultAgentPort;

            var agentUri = source?.GetString(ConfigurationKeys.EndpointUrl) ?? GetConfiguredTracesEndpoint(ingestRealm, agentPort);

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

            if (string.Equals(AgentUri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Replace localhost with 127.0.0.1 to avoid DNS resolution.
                // When ipv6 is enabled, localhost is first resolved to ::1, which fails
                // because the trace agent is only bound to ipv4.
                // This causes delays when sending traces.
                var builder = new UriBuilder(agentUri) { Host = "127.0.0.1" };
                AgentUri = builder.Uri;
            }

            TracesPipeName = source?.GetString(ConfigurationKeys.TracesPipeName);

            // Agent port is set to zero in places like AAS where it's needed to prevent port conflict
            // The agent will fail to start if it can not bind a port, so we need to override 8126 to prevent port conflict
            // Port 0 means it will pick some random available port
            var hasExplicitHostOrPortSettings = (agentPort != 0) || AgentUri.Host != null;

            if (hasExplicitHostOrPortSettings)
            {
                if (AgentUri.Host?.StartsWith(UnixDomainSocketPrefix) ?? false)
                {
                    traceTransport = TracesTransportType.UnixDomainSocket;
                    TracesUnixDomainSocketPath = AgentUri.Host;
                }
                else
                {
                    // The agent host is explicitly configured, we should assume UDP for metrics
                    forceMetricsOverUdp = true;
                    traceTransport = TracesTransportType.Default;
                }
            }
            else if (!string.IsNullOrWhiteSpace(TracesPipeName))
            {
                traceTransport = TracesTransportType.WindowsNamedPipe;

                TracesPipeTimeoutMs = source?.GetInt32(ConfigurationKeys.TracesPipeTimeoutMs)
#if DEBUG
                    ?? 20_000;
#else
                    ?? 500;
#endif
            }

            if (traceTransport == null)
            {
                // Check for UDS
                var traceSocket = source?.GetString(ConfigurationKeys.TracesUnixDomainSocketPath);

                if (traceSocket != null)
                {
                    traceTransport = TracesTransportType.UnixDomainSocket;
                    TracesUnixDomainSocketPath = traceSocket;
                }
                else
                {
                    // check for default file
                    if (_fileExists(DefaultTracesUnixDomainSocket))
                    {
                        traceTransport = TracesTransportType.UnixDomainSocket;
                        TracesUnixDomainSocketPath = DefaultTracesUnixDomainSocket;
                    }
                }
            }

            TracesTransport = traceTransport ?? TracesTransportType.Default;
        }
    }
}
