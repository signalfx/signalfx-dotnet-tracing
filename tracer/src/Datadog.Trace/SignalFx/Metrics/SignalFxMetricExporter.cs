// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
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
        private const int MaxAttempts = 3;

        private static readonly HashSet<HttpStatusCode> TransientErrorStatusCodes = new HashSet<HttpStatusCode>(new[]
        {
            HttpStatusCode.RequestTimeout,
#if NETCOREAPP
            HttpStatusCode.TooManyRequests
#endif
        });

        private static readonly HashSet<WebExceptionStatus> TransientExceptionStatuses = new HashSet<WebExceptionStatus>(new[]
        {
            WebExceptionStatus.ConnectionClosed,
            WebExceptionStatus.Timeout,
            WebExceptionStatus.RequestCanceled
        });

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(SignalFxMetricExporter));

        private readonly Uri _metricsEndpointAddress;

        private readonly string _apiToken;

        private readonly int _webRequestTimeoutMs;
        private readonly TimeSpan _waitBeforeRetries;

        public SignalFxMetricExporter(Uri metricsEndpointAddress, string apiToken)
        {
            if (metricsEndpointAddress == null)
            {
                throw new ArgumentNullException(nameof(metricsEndpointAddress));
            }

            _metricsEndpointAddress = metricsEndpointAddress;
            _apiToken = apiToken;
            _webRequestTimeoutMs = 500;
            _waitBeforeRetries = TimeSpan.FromMilliseconds(200);
        }

        public void Send(DataPointUploadMessage msg)
        {
            if (msg?.datapoints == null || msg.datapoints.Count < 1)
            {
                return;
            }

            var attemptNumber = 1;

            while (true)
            {
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
                    // if request failed (statusCode < 200 || statusCode >= 300), exception will be thrown
                    Log.Debug($"Sent {msg.datapoints.Count} metric data points to: {_metricsEndpointAddress}.");
                    return;
                }
                catch (Exception e)
                {
                    if (attemptNumber >= MaxAttempts || !IsTransient(e))
                    {
                        Log.Error(e, $"Dropping metrics after {attemptNumber} unsuccessful attempts sending to: {_metricsEndpointAddress}.");
                        return;
                    }

                    Log.Debug(e, "Transient exception encountered. Retrying sending metric data.");
                }

                attemptNumber++;
                // short wait before retrying
                Thread.Sleep(_waitBeforeRetries);
            }
        }

        private static bool IsTransient(Exception ex)
        {
            var webException = ex as WebException;
            if (webException != null)
            {
                var webExceptionStatus = webException.Status;
                if (webExceptionStatus != WebExceptionStatus.ProtocolError)
                {
                    // these are considered to be transient
                    return TransientExceptionStatuses.Contains(webExceptionStatus);
                }

                using var response = (HttpWebResponse)webException.Response;
                return TransientErrorStatusCodes.Contains(response.StatusCode);
            }

            return false;
        }
    }
}
