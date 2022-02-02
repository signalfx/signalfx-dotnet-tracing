// Modified by Splunk Inc.

using System;
using System.Net;
using Datadog.Trace.Logging;
using Datadog.Trace.Propagation;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.SignalFx.Metrics
{
    /// <summary>
    /// Simple exporter that exports metric in SignalFx proto format over http.
    /// </summary>
    internal class SignalFxMetricExporter : ISignalFxMetricExporter
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(SignalFxMetricExporter));
        private readonly Uri _metricsEndpointAddress;
        private readonly string _apiToken;
        private readonly int _webRequestTimeoutMs;

        public SignalFxMetricExporter(Uri metricsEndpointAddress, string apiToken)
        {
            if (metricsEndpointAddress == null)
            {
                throw new ArgumentNullException(nameof(metricsEndpointAddress));
            }

            _metricsEndpointAddress = metricsEndpointAddress;
            _apiToken = apiToken;
            _webRequestTimeoutMs = 500;
        }

        public void Send(DataPointUploadMessage msg)
        {
            if (msg == null || msg.datapoints.Count < 1)
            {
                return;
            }

            try
            {
                var webRequest = WebRequest.CreateHttp(_metricsEndpointAddress);
                webRequest.ContentType = "application/x-protobuf";
                webRequest.Method = "POST";
                webRequest.Headers.Add(CommonHttpHeaderNames.TracingEnabled, "false");

                if (!string.IsNullOrWhiteSpace(_apiToken))
                {
                    webRequest.Headers.Add("X-Sf-Token", _apiToken);
                }

                webRequest.Timeout = _webRequestTimeoutMs;

                using (var requestStream = webRequest.GetRequestStream())
                {
                    Serializer.Serialize(requestStream, msg);
                    requestStream.Flush();
                }

                using var webResponse = (HttpWebResponse)webRequest.GetResponse();
                if (IndicatesSuccess(webResponse.StatusCode))
                {
                    Log.Debug($"Sent {msg.datapoints.Count} metric data points to: {_metricsEndpointAddress}.");
                }
                else
                {
                    Log.Error($"HTTP error sending metric data to {_metricsEndpointAddress}: {webResponse.StatusCode}.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Metric data could not be sent to {_metricsEndpointAddress}.");
            }
        }

        private static bool IndicatesSuccess(HttpStatusCode webResponseStatusCode)
        {
            return webResponseStatusCode >= HttpStatusCode.OK && webResponseStatusCode < HttpStatusCode.MultipleChoices;
        }
    }
}
