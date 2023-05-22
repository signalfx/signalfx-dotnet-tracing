// <copyright file="ExporterSettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration.Helpers;
using Datadog.Trace.SignalFx.Metrics;
using Datadog.Trace.Vendors.Serilog;
using MetricsTransportType = Datadog.Trace.Vendors.StatsdClient.Transport.TransportType;

namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Contains exporter settings.
    /// </summary>
    public class ExporterSettings
    {
        private int _partialFlushMinSpans;
        private Uri _agentUri;

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
        /// Initializes a new instance of the <see cref="ExporterSettings"/> class with default values.
        /// </summary>
        public ExporterSettings()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExporterSettings"/> class.
        /// Direct use in tests only.
        /// </summary>
        /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
        public ExporterSettings(IConfigurationSource source)
        {
            ValidationWarnings = new List<string>();

            // Get values from the config
            var endpointUrl = source?.GetString(ConfigurationKeys.EndpointUrl);

            var tracesPipeName = source?.GetString(ConfigurationKeys.TracesPipeName);

            var tracesPipeTimeoutMs = source?.GetInt32(ConfigurationKeys.TracesPipeTimeoutMs) ?? 0;

            var agentPort = source?.GetInt32(ConfigurationKeys.AgentPort);

            var ingestRealm = source?.GetString(ConfigurationKeys.IngestRealm) ??
                              LocalIngestRealm;

            var dogStatsdPort = source?.GetInt32(ConfigurationKeys.DogStatsdPort) ?? 0;
            var metricsPipeName = source?.GetString(ConfigurationKeys.MetricsPipeName);

            ConfigureTraceTransport(endpointUrl, tracesPipeName, agentPort, ingestRealm);

            MetricsExporter = source.GetTypedValue<MetricsExporterType>(ConfigurationKeys.MetricsExporter);
            if (MetricsExporter == MetricsExporterType.SignalFx)
            {
                MetricsEndpointUrl = source.SafeReadUri(
                    key: ConfigurationKeys.MetricsEndpointUrl,
                    defaultTo: GetConfiguredMetricsEndpoint(ingestRealm),
                    out _);
            }
            else
            {
                ConfigureStatsdMetricsTransport(endpointUrl, dogStatsdPort, metricsPipeName);
            }

            ConfigureLogsTransport(source);

            TracesPipeTimeoutMs = tracesPipeTimeoutMs > 0 ? tracesPipeTimeoutMs : 500;
            PartialFlushEnabled = source?.GetBool(ConfigurationKeys.PartialFlushEnabled) ?? false;

            PartialFlushMinSpans = source.SafeReadInt32(
                key: ConfigurationKeys.PartialFlushMinSpans,
                defaultTo: 500,
                validators: (val) => val > 0);
            SyncExport = source?.GetBool(ConfigurationKeys.TraceSyncExport) ?? false;

            TraceBufferSize = source.SafeReadInt32(
                key: ConfigurationKeys.TraceBufferSize,
                defaultTo: 1000,
                validators: configuredSize => configuredSize > 0);
        }

        /// <summary>
        /// Gets or sets the Uri where the Tracer can connect to the Agent.
        /// Default is <c>"http://localhost:9411/api/v2/spans"</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.AgentHost"/>
        /// <seealso cref="ConfigurationKeys.AgentPort"/>
        public Uri AgentUri
        {
            get => _agentUri;
            set
            {
                SetAgentUriAndTransport(value);
                // In the case the url was a UDS one, we do not change anything.
                if (TracesTransport == TracesTransportType.Default)
                {
                    MetricsTransport = MetricsTransportType.UDP;
                }
            }
        }

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
        /// Default is <c>9943</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.DogStatsdPort"/>
        public int DogStatsdPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether partial flush is enabled
        /// </summary>
        public bool PartialFlushEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to do synchronous export.
        /// </summary>
        public bool SyncExport { get; set; }

        /// <summary>
        /// Gets or sets the size of the trace buffer.
        /// </summary>
        public int TraceBufferSize { get; set; }

        /// <summary>
        /// Gets or sets the type of metrics exporter.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.MetricsExporter"/>
        public MetricsExporterType MetricsExporter { get; set; }

        internal List<string> ValidationWarnings { get; }

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

        private static Uri GetConfiguredMetricsEndpoint(string ingestRealm)
        {
            return IsDefaultIngestRealm(ingestRealm) ?
                       // local collector
                       new Uri("http://localhost:9943/v2/datapoint") :
                       // direct ingest
                       new Uri($"https://ingest.{ingestRealm}.signalfx.com/v2/datapoint");
        }

        private static bool IsDefaultIngestRealm(string ingestRealm)
        {
            return string.Equals(ingestRealm, LocalIngestRealm, StringComparison.OrdinalIgnoreCase);
        }

        private void ConfigureStatsdMetricsTransport(string traceAgentUrl, int dogStatsdPort, string metricsPipeName)
        {
            // Agent port is set to zero in places like AAS where it's needed to prevent port conflict
            // The agent will fail to start if it can not bind a port, so we need to override 8126 to prevent port conflict
            // Port 0 means it will pick some random available port
            if (dogStatsdPort < 0)
            {
                ValidationWarnings.Add("The provided dogStatsD port isn't valid, it should be positive.");
            }

            if (!string.IsNullOrWhiteSpace(traceAgentUrl) && Uri.TryCreate(traceAgentUrl, UriKind.Absolute, out var _))
            {
                // No need to set AgentHost, it is taken from the AgentUri and set in ConfigureTrace
                MetricsTransport = MetricsTransportType.UDP;
            }
            else if (dogStatsdPort > 0)
            {
                // No need to set AgentHost, it is taken from the AgentUri and set in ConfigureTrace
                MetricsTransport = MetricsTransportType.UDP;
            }
            else if (!string.IsNullOrWhiteSpace(metricsPipeName))
            {
                MetricsTransport = MetricsTransportType.NamedPipe;
                MetricsPipeName = metricsPipeName;
            }
            else
            {
                MetricsTransport = MetricsTransportType.UDP;
                DogStatsdPort = DefaultDogstatsdPort;
            }

            DogStatsdPort = dogStatsdPort > 0 ? dogStatsdPort : DefaultDogstatsdPort;
        }

        private void ConfigureLogsTransport(IConfigurationSource source)
        {
            var logsEndpointUrl = source?.GetString(ConfigurationKeys.LogsEndpointUrl) ??
                                  // default value
                                  "http://localhost:4318/v1/logs";
            LogsEndpointUrl = new Uri(logsEndpointUrl);
        }

        private void ConfigureTraceTransport(string endpointUrl, string tracesPipeName, int? agentPort, string ingestRealm)
        {
            // Check the parameters in order of precedence
            // For some cases, we allow falling back on another configuration (eg invalid url as the application will need to be restarted to fix it anyway).
            // For other cases (eg a configured unix domain socket path not found), we don't fallback as the problem could be fixed outside the application.
            if (!string.IsNullOrWhiteSpace(endpointUrl))
            {
                if (TrySetAgentUriAndTransport(endpointUrl))
                {
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(tracesPipeName))
            {
                TracesTransport = TracesTransportType.WindowsNamedPipe;
                TracesPipeName = tracesPipeName;

                // The Uri isn't needed anymore in that case, just populating it for retro compatibility.
                if (Uri.TryCreate($"http://{DefaultAgentHost}:{agentPort ?? DefaultAgentPort}", UriKind.Absolute, out var uri))
                {
                    SetAgentUriReplacingLocalhost(uri);
                }

                return;
            }

            if (!IsDefaultIngestRealm(ingestRealm))
            {
                if (TrySetAgentUriAndTransport($"https://ingest.{ingestRealm}.signalfx.com/v2/trace"))
                {
                    return;
                }
            }

            if (agentPort != null && agentPort != 0)
            {
                // Agent port is set to zero in places like AAS where it's needed to prevent port conflict
                // The agent will fail to start if it can not bind a port, so we need to override 8126 to prevent port conflict
                // Port 0 means it will pick some random available port

                if (TrySetAgentUriAndTransport(DefaultAgentHost, agentPort.Value))
                {
                    return;
                }
            }

            ValidationWarnings.Add("No transport configuration found, using default values");
            TrySetAgentUriAndTransport(DefaultAgentHost, DefaultAgentPort);
        }

        private bool TrySetAgentUriAndTransport(string host, int port)
        {
            return TrySetAgentUriAndTransport($"http://{host}:{port}/api/v2/spans");
        }

        private bool TrySetAgentUriAndTransport(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                ValidationWarnings.Add($"The Uri: '{url}' is not valid. It won't be taken into account to send traces. Note that only absolute urls are accepted.");
                return false;
            }

            SetAgentUriAndTransport(uri);
            return true;
        }

        private void SetAgentUriAndTransport(Uri uri)
        {
            TracesTransport = TracesTransportType.Default;

            SetAgentUriReplacingLocalhost(uri);
        }

        private void SetAgentUriReplacingLocalhost(Uri uri)
        {
            if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Replace localhost with 127.0.0.1 to avoid DNS resolution.
                // When ipv6 is enabled, localhost is first resolved to ::1, which fails
                // because the trace agent is only bound to ipv4.
                // This causes delays when sending traces.
                var builder = new UriBuilder(uri) { Host = "127.0.0.1" };
                _agentUri = builder.Uri;
            }
            else
            {
                _agentUri = uri;
            }
        }
    }
}
