// Modified by SignalFx
using System;
using System.Globalization;
using System.Linq;
using Datadog.Trace.Headers;
using Datadog.Trace.Logging;

namespace Datadog.Trace
{
    // Modeled from SpanContextPropagator
    internal class B3SpanContextPropagator
    {
        private const NumberStyles NumberStyle = System.Globalization.NumberStyles.HexNumber;
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.For<B3SpanContextPropagator>();
        private static readonly string UserKeep = ((int)SamplingPriority.UserKeep).ToString(CultureInfo.InvariantCulture);

        private B3SpanContextPropagator()
        {
        }

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

            headers.Set(HttpHeaderNames.B3TraceId, context.TraceId.ToString("N"));
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

            var traceId = ParseGuid(headers, HttpHeaderNames.B3TraceId);

            if (traceId == Guid.Empty)
            {
                // a valid traceId is required to use distributed tracing
                return null;
            }

            var spanId = ParseHexUInt64(headers, HttpHeaderNames.B3SpanId);
            var samplingPriority = ParseB3Sampling<SamplingPriority>(headers);
            return new SpanContext(traceId, spanId, samplingPriority);
        }

        private static Guid ParseGuid(IHeadersCollection headers, string headerName)
        {
            var headerValues = headers.GetValues(headerName).ToList();

            if (headerValues.Count > 0)
            {
                foreach (string headerValue in headerValues)
                {
                    var candidate = headerValue;
                    if (candidate.Length == 16)
                    {
                        candidate = "0000000000000000" + headerValue;
                    }

                    if (Guid.TryParse(candidate, out var result))
                    {
                        return result;
                    }
                }

                Log.Information("Could not parse {0} headers: {1}", headerName, string.Join(",", headerValues));
            }

            return Guid.Empty;
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

        private static T? ParseB3Sampling<T>(IHeadersCollection headers)
            where T : struct, Enum
        {
            string priority = string.Empty;
            var debugged = headers.GetValues(HttpHeaderNames.B3Flags).ToList();
            var sampled = headers.GetValues(HttpHeaderNames.B3Sampled).ToList();
            if (debugged.Count != 0 && debugged[0] == "1")
            {
                priority = UserKeep;
            }
            else if (sampled.Count != 0)
            {
                priority = sampled.First();
            }

            if (Enum.TryParse<T>(priority, out var result) && Enum.IsDefined(typeof(T), result))
            {
                return result;
            }

            Log.Information(
                "Could not parse headers: {0}: {1} or {2}: {3}",
                HttpHeaderNames.B3Flags,
                string.Join(",", debugged),
                HttpHeaderNames.B3Sampled,
                string.Join(",", sampled));

            return default;
        }
    }
}
