// Modified by Splunk Inc.

using System;
using System.Net;
using System.Threading.Tasks;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Trace.Propagation;

namespace Datadog.Trace.Agent.Zipkin
{
    internal class ZipkinExporter : IExporter
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(ZipkinExporter));

        private readonly Uri _tracesEndpoint;
        private readonly ImmutableTracerSettings _settings;

        public ZipkinExporter(ImmutableTracerSettings settings)
        {
            Log.Debug("Creating new Zipkin exporter");

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _tracesEndpoint = _settings.ExporterSettings.AgentUri;
        }

        public async Task<bool> SendTracesAsync(Span[][] traces)
        {
            if (traces == null || traces.Length == 0)
            {
                // Nothing to send, no ping for Zipkin.
                return true;
            }

            // retry up to 5 times with exponential back-off
            var retryLimit = 5;
            var retryCount = 1;
            var sleepDuration = 100; // in milliseconds

            while (true)
            {
                // TODO: Initially same code for Fx and Core.
                var request = WebRequest.CreateHttp(_tracesEndpoint);
                request.Method = "POST";
                request.ContentType = "application/json";

                // Disable automatic instrumentation for Zipkin exporter
                request.Headers.Add(CommonHttpHeaderNames.TracingEnabled, "false");

                // Add SignalFx Access Token if configured
                if (!string.IsNullOrWhiteSpace(_settings.SignalFxAccessToken))
                {
                    request.Headers.Add("X-Sf-Token", _settings.SignalFxAccessToken);
                }

                using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                {
                    var serializer = new ZipkinSerializer(_settings);
                    serializer.Serialize(requestStream, traces);
                }

                Exception requestException = null;
                HttpStatusCode requestStatusCode = 0;
                try
                {
                    using var httpWebResponse = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);

                    // Zipkin specifies only "Accepted" as valid response, the code is more tolerant here.
                    // Following a criteria equivalent to HttpResponseMessage.EnsureSuccessStatusCode as
                    // done by the OpenTelemetry .NET SDK for their Zipkin exporter:
                    // See https://github.com/open-telemetry/opentelemetry-dotnet/blob/8cda9ef394a1b075fd156d73dace48e48f5b3c9b/src/OpenTelemetry.Exporter.Zipkin/ZipkinExporter.cs#L86
                    if (httpWebResponse.StatusCode >= HttpStatusCode.OK && httpWebResponse.StatusCode < HttpStatusCode.MultipleChoices)
                    {
                        return true;
                    }

                    requestStatusCode = httpWebResponse.StatusCode;
                    Log.Debug("HTTP error sending traces to {0}: {1}", _tracesEndpoint, httpWebResponse.StatusCode);
                }
                catch (Exception ex)
                {
                    requestException = ex;
                    Log.Debug("Exception sending traces to {0}: {1}", _tracesEndpoint, ex.Message);
                }

                if (retryCount >= retryLimit)
                {
                    if (requestException != null)
                    {
                        Log.Error("No more retries, dropping spans. Last exception sending traces to {0}: {1}", _tracesEndpoint, requestException.Message);
                    }
                    else
                    {
                        Log.Error("No more retries, dropping spans. Last HTTP error sending traces to {0}: {1}", _tracesEndpoint, requestStatusCode);
                    }

                    return false;
                }

                // retry
                await Task.Delay(sleepDuration).ConfigureAwait(false);
                retryCount++;
                sleepDuration *= 2;
            }
        }
    }
}
