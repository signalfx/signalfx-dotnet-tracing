// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Datadog.Trace.Agent
{
    internal class ZipkinContent : HttpContent
    {
        private readonly ZipkinSerializer _serializer = new ZipkinSerializer();
        private readonly Span[][] _spans;

        public ZipkinContent(Span[][] spans, string signalFxAccessToken)
        {
            _spans = spans;
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
            if (!string.IsNullOrWhiteSpace(signalFxAccessToken))
            {
                Headers.Add("X-Sf-Token", signalFxAccessToken);
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return Task.Factory.StartNew(() =>
                {
                    _serializer.Serialize(stream, _spans);
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
