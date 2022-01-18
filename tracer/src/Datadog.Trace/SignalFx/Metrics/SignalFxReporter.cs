using System;
using System.IO;
using System.Net;
using System.Security;
using Datadog.Trace.Logging;
using Datadog.Trace.Propagation;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.SignalFx.Metrics
{
    /// <summary>
    /// Simple reporter that sends metrics in proto format over http.
    /// </summary>
    // TODO: make async
    internal class SignalFxReporter : ISignalFxReporter
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(SignalFxReporter));
        private readonly WebRequestorFactory _requestorFactory;

        public SignalFxReporter(Uri metricsEndpointAddress, string apiToken)
        {
            if (metricsEndpointAddress == null)
            {
                throw new ArgumentNullException(nameof(metricsEndpointAddress));
            }

            _requestorFactory = new WebRequestorFactory()
                               .WithUri(metricsEndpointAddress)
                               .WithMethod("POST")
                               .WithContentType("application/x-protobuf")
                                // Disable automatic instrumentation
                               .WithHeader(CommonHttpHeaderNames.TracingEnabled, "false")
                                // TODO: make configurable
                               .WithTimeout(timeoutInMilliseconds: 500);
            // Add SignalFx Access Token if configured
            if (!string.IsNullOrWhiteSpace(apiToken))
            {
                _requestorFactory.WithHeader("X-Sf-Token", apiToken);
            }
        }

        public void Send(DataPointUploadMessage msg)
        {
            if (msg == null)
            {
                throw new ArgumentNullException(nameof(msg));
            }

            try
            {
                var requestor = _requestorFactory.GetRequestor();
                using (var rs = requestor.GetWriteStream())
                {
                    Serializer.Serialize(rs, msg);
                    // flush the message before disposing
                    rs.Flush();
                }

                using var webResponse = (HttpWebResponse)requestor.Send();
                if (IndicatesSuccess(webResponse.StatusCode))
                {
                    Log.Debug("Sent metrics");
                }
                else
                {
                    Log.Error($"HTTP error sending metrics: {webResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception sending metrics.");
            }
        }

        private static bool IndicatesSuccess(HttpStatusCode webResponseStatusCode)
        {
            return webResponseStatusCode >= HttpStatusCode.OK && webResponseStatusCode < HttpStatusCode.MultipleChoices;
        }
    }
}
