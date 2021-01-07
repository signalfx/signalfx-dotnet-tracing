// Modified by SignalFx
using System;
using System.Net.Http;
using System.Threading.Tasks;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.Logging;

namespace SignalFx.Tracing.Agent
{
    internal class ZipkinApi : IApi
    {
        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.GetLogger(typeof(ZipkinApi));

        private readonly HttpClient _client;
        private readonly TracerSettings _settings;

        public ZipkinApi(TracerSettings settings, DelegatingHandler delegatingHandler)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _client = delegatingHandler == null
                          ? new HttpClient()
                          : new HttpClient(delegatingHandler);
            _client.DefaultRequestHeaders.Add(HttpHeaderNames.TracingEnabled, "false");
        }

        public async Task SendTracesAsync(Span[][] traces)
        {
            // retry up to 5 times with exponential back-off
            var retryLimit = 5;
            var retryCount = 1;
            var sleepDuration = 100; // in milliseconds

            while (true)
            {
                HttpResponseMessage responseMessage;

                try
                {
                    // re-create content on every retry because some versions of HttpClient always dispose of it, so we can't reuse.
                    using (var content = new ZipkinContent(traces, _settings))
                    {
                        responseMessage = await _client.PostAsync(_settings.EndpointUrl, content).ConfigureAwait(false);
                        responseMessage.EnsureSuccessStatusCode();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException is InvalidOperationException ioe)
                    {
                        Log.Error("A fatal error occurred while sending traces to {Endpoint}\n{Exception}", _settings.EndpointUrl, ex.Message);
                        return;
                    }

                    if (retryCount >= retryLimit)
                    {
                        // stop retrying
                        Log.Error("No more retries to send traces to {Endpoint}\n{Exception}", _settings.EndpointUrl, ex.Message);
                        return;
                    }

                    Log.Debug("Error sending traces to {Endpoint}\n{Exception}", _settings.EndpointUrl, ex.Message);

                    // retry
                    await Task.Delay(sleepDuration).ConfigureAwait(false);
                    retryCount++;
                    sleepDuration *= 2;
                }
            }
        }
    }
}
