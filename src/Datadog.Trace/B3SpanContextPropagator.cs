// Modified by SignalFx
using System;
using System.Globalization;
using System.Linq;
using SignalFx.Tracing.Headers;
using SignalFx.Tracing.Logging;

namespace SignalFx.Tracing
{
    /// <summary>
    /// Class that hanbles B3 style context propagation.
    /// </summary>
    public class B3SpanContextPropagator
    {
        private const NumberStyles NumberStyle = System.Globalization.NumberStyles.HexNumber;
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.For<B3SpanContextPropagator>();
        private static readonly string UserKeep = ((int)SamplingPriority.UserKeep).ToString(CultureInfo.InvariantCulture);

        private B3SpanContextPropagator()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the propagator.
        /// </summary>
        public static B3SpanContextPropagator Instance { get; } = new B3SpanContextPropagator();

        /// <summary>
        /// Propagates the specified context by adding new headers to a <see cref="IHeadersCollection"/>.
        /// This locks the sampling priority for <paramref name="context"/>.
        /// </summary>
        /// <param name="context">A <see cref="SpanContext"/> value that will be propagated into <paramref name="headers"/>.</param>
        /// <param name="headers">A <see cref="IHeadersCollection"/> to add new headers to.</param>
        public void Inject(SpanContext context, IHeadersCollection headers)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }

            if (headers == null) { throw new ArgumentNullException(nameof(headers)); }

            // lock sampling priority when span propagates.
            context.TraceContext?.LockSamplingPriority();

            headers.Set(HttpHeaderNames.B3TraceId, context.TraceId.ToString("x16", InvariantCulture));
            headers.Set(HttpHeaderNames.B3SpanId, context.SpanId.ToString("x16", InvariantCulture));
            if (context.ParentId != null)
            {
                headers.Set(HttpHeaderNames.B3ParentId, context.ParentId?.ToString("x16", InvariantCulture));
            }

            var samplingPriority = (int?)(context.TraceContext?.SamplingPriority ?? context.SamplingPriority);
            if (samplingPriority != null)
            {
                var value = samplingPriority < (int)SamplingPriority.AutoKeep ? "0" : "1";
                var header = samplingPriority == (int)SamplingPriority.UserKeep ? HttpHeaderNames.B3Flags : HttpHeaderNames.B3Sampled;
                headers.Set(header, value);
            }
        }

        /// <summary>
        /// Extracts a <see cref="SpanContext"/> from the values found in the specified headers.
        /// </summary>
        /// <param name="headers">The headers that contain the values to be extracted.</param>
        /// <returns>A new <see cref="SpanContext"/> that contains the values obtained from <paramref name="headers"/>.</returns>
        public SpanContext Extract(IHeadersCollection headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            var traceId = ParseHexUInt64(headers, HttpHeaderNames.B3TraceId);

            if (traceId == 0)
            {
                // a valid traceId is required to use distributed tracing
                return null;
            }

            var spanId = ParseHexUInt64(headers, HttpHeaderNames.B3SpanId);
            var samplingPriority = ParseB3Sampling(headers);
            return new SpanContext(traceId, spanId, samplingPriority);
        }

        private static ulong ParseHexUInt64(IHeadersCollection headers, string headerName)
        {
            var headerValues = headers.GetValues(headerName).ToList();

            if (headerValues.Count > 0)
            {
                foreach (string headerValue in headerValues)
                {
                    if (ulong.TryParse(headerValue, NumberStyle, InvariantCulture, out var result))
                    {
                        return result;
                    }
                }

                Log.Information("Could not parse {0} headers: {1}", headerName, string.Join(",", headerValues));
            }

            return 0;
        }

        private static SamplingPriority? ParseB3Sampling(IHeadersCollection headers)
        {
            var debugged = headers.GetValues(HttpHeaderNames.B3Flags).ToList();
            var sampled = headers.GetValues(HttpHeaderNames.B3Sampled).ToList();
            if (debugged.Count != 0 && (debugged[0] == "0" || debugged[0] == "1"))
            {
                 return debugged[0] == "1" ? SamplingPriority.UserKeep : null;
            }
            else if (sampled.Count != 0 && (sampled[0] == "0" || sampled[0] == "1"))
            {
                return sampled[0] == "1" ? SamplingPriority.AutoKeep : SamplingPriority.AutoReject;
            }

            Log.Information(
                "Could not parse headers: {0}: {1} or {2}: {3}",
                HttpHeaderNames.B3Flags,
                string.Join(",", debugged),
                HttpHeaderNames.B3Sampled,
                string.Join(",", sampled));

            return null;
        }
    }
}
