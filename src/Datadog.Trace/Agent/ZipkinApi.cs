// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Datadog.Trace.Configuration;
using Datadog.Trace.DogStatsd;
using Datadog.Trace.Logging;
using Datadog.Trace.Vendors.StatsdClient;
using MsgPack.Serialization;
using Newtonsoft.Json;

namespace Datadog.Trace.Agent
{
    internal class ZipkinApi : IApi
    {
        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.For<ZipkinApi>();

        private readonly HttpClient _client;
        private readonly TracerSettings _settings;

        public ZipkinApi(TracerSettings settings, DelegatingHandler delegatingHandler, IStatsd statsd)
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
#if DEBUG
                    if (ex.InnerException is InvalidOperationException ioe)
                    {
                        Log.Error("An error occurred while sending traces to {Endpoint}\n{Exception}", ex, _settings.EndpointUrl, ex.ToString());
                        return;
                    }
#endif
                    if (retryCount >= retryLimit)
                    {
                        // stop retrying
                        Log.Error("An error occurred while sending traces to {Endpoint}", ex, _settings.EndpointUrl);
                        return;
                    }

                    // retry
                    await Task.Delay(sleepDuration).ConfigureAwait(false);
                    retryCount++;
                    sleepDuration *= 2;
                }
            }
        }
    }
}
