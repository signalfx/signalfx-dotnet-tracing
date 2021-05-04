// Modified by SignalFx
using System;
using System.Globalization;
using OpenTracing.Propagation;
using SignalFx.Tracing.Headers;
using SignalFx.Tracing.Propagation;

namespace SignalFx.Tracing.OpenTracing
{
    internal class HttpHeadersCodec : ICodec
    {
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        private readonly IPropagator _propagator;

        public HttpHeadersCodec(IPropagator propagator)
        {
            _propagator = propagator;
        }

        public global::OpenTracing.ISpanContext Extract(object carrier)
        {
            var map = carrier as ITextMap;

            if (map == null)
            {
                throw new ArgumentException("Carrier should have type ITextMap", nameof(carrier));
            }

            IHeadersCollection headers = new TextMapHeadersCollection(map);
            var propagationContext = _propagator.Extract(headers);
            return new OpenTracingSpanContext(propagationContext);
        }

        public void Inject(global::OpenTracing.ISpanContext context, object carrier)
        {
            var map = carrier as ITextMap;

            if (map == null)
            {
                throw new ArgumentException("Carrier should have type ITextMap", nameof(carrier));
            }

            IHeadersCollection headers = new TextMapHeadersCollection(map);

            if (context is OpenTracingSpanContext otSpanContext && otSpanContext.Context is SpanContext spanContext)
            {
                // this is a SignalFx context
                _propagator.Inject(spanContext, headers);
            }
            else
            {
                // any other OpenTracing.ISpanContext
                // TODO: this assumes that the IDs are on a B3 compatible format.
                headers.Set(B3HttpHeaderNames.B3TraceId, context.TraceId);
                headers.Set(B3HttpHeaderNames.B3ParentId, context.SpanId);
            }
        }
    }
}
