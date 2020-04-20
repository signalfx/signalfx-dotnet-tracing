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
        private readonly ZipkinSerializer serializer = new ZipkinSerializer();

        public ZipkinContent(Span[][] value)
        {
            Value = value;
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        public Span[][] Value { get; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return Task.Factory.StartNew(() =>
                {
                    serializer.Serialize(stream, Value);
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
