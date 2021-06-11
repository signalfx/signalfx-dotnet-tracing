// Modified by SignalFx

using System.Collections.Generic;
using SignalFx.Tracing.Headers;

namespace SignalFx.Tracing.Propagation
{
    internal static class PropagationExtensions
    {
        public static void Inject(this IPropagator propagator, SpanContext context, IHeadersCollection headers)
        {
            propagator.Inject(context, headers, InjectToHeadersCollection);
        }

        public static SpanContext Extract(this IPropagator propagator, IHeadersCollection headers)
        {
            return propagator.Extract(headers, ExtractFromHeadersCollection);
        }

        private static void InjectToHeadersCollection(IHeadersCollection carrier, string header, string value)
        {
            carrier.Set(header, value);
        }

        private static IEnumerable<string> ExtractFromHeadersCollection(IHeadersCollection carrier, string header)
        {
            return carrier.GetValues(header);
        }
    }
}
