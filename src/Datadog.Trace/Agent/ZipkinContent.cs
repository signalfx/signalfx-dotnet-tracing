// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using SignalFx.Tracing.Configuration;

namespace SignalFx.Tracing.Agent
{
    internal class ZipkinContent : HttpContent
    {
        private readonly ZipkinSerializer _serializer = new ZipkinSerializer();
        private readonly Span[][] _spans;
        private readonly TracerSettings _settings;

        public ZipkinContent(Span[][] spans, TracerSettings settings)
        {
            _spans = spans;
            _settings = settings;
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
            if (!string.IsNullOrWhiteSpace(_settings.SignalFxAccessToken))
            {
                Headers.Add("X-Sf-Token", _settings.SignalFxAccessToken);
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return Task.Factory.StartNew(() =>
                {
                    _serializer.Serialize(stream, _spans, _settings);
                });
        }

        protected override bool TryComputeLength(out long length)
        {
            // We can't compute the length beforehand
            length = -1;
            return false;
        }
    }
}
