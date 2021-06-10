// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using SignalFx.Tracing.Logging;

namespace SignalFx.Tracing.Propagation
{
    /// <summary>
    /// Class that handles B3 style context propagation.
    /// </summary>
    internal class B3SpanContextPropagator : IPropagator
    {
        private const NumberStyles NumberStyle = NumberStyles.HexNumber;

        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        private static readonly Vendors.Serilog.ILogger Log = SignalFxLogging.For<B3SpanContextPropagator>();

        public void Inject<T>(SpanContext context, T carrier, Action<T, string, string> setter)
        {
            if (context == null) { throw new ArgumentNullException(nameof(context)); }

            if (carrier == null) { throw new ArgumentNullException(nameof(carrier)); }

            if (setter == null) { throw new ArgumentNullException(nameof(setter)); }

            // lock sampling priority when span propagates.
            context.TraceContext?.LockSamplingPriority();

            setter(carrier, B3HttpHeaderNames.B3TraceId, context.TraceId.ToString());
            setter(carrier, B3HttpHeaderNames.B3SpanId, context.SpanId.ToString("x16", InvariantCulture));

            if (context.ParentId != null)
            {
                setter(carrier, B3HttpHeaderNames.B3ParentId, context.ParentId?.ToString("x16", InvariantCulture));
            }

            var samplingHeader = GetSamplingHeader(context);
            if (samplingHeader.HasValue)
            {
                setter(carrier, samplingHeader.Value.Key, samplingHeader.Value.Value);
            }
        }

        public SpanContext Extract<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            if (carrier == null) { throw new ArgumentNullException(nameof(carrier)); }

            if (getter == null) { throw new ArgumentNullException(nameof(getter)); }

            var traceId = PropagationHelpers.ParseTraceId(carrier, getter, B3HttpHeaderNames.B3TraceId, Log);

            if (traceId == TraceId.Zero)
            {
                // a valid traceId is required to use distributed tracing
                return null;
            }

            var spanId = ParseHexUInt64(carrier, getter, B3HttpHeaderNames.B3SpanId);
            var samplingPriority = ParseB3FlagsAndSampled(carrier, getter);

            return new SpanContext(traceId, spanId, samplingPriority);
        }

        private static ulong ParseHexUInt64<T>(T carrier, Func<T, string, IEnumerable<string>> getter, string headerName)
        {
            var enumerableHeaderValues = getter(carrier, headerName);
            if (enumerableHeaderValues == Enumerable.Empty<string>())
            {
                return 0;
            }

            var headerValues = enumerableHeaderValues.ToArray();
            if (headerValues.Length == 0)
            {
                return 0;
            }

            for (var i = 0; i < headerValues.Length; ++i)
            {
                if (ulong.TryParse(headerValues[i], NumberStyle, InvariantCulture, out var result))
                {
                    return result;
                }
            }

            Log.Debug("Could not parse {0} headers: {1}", headerName, string.Join(",", headerValues));
            return 0;
        }

        private static SamplingPriority? ParseB3FlagsAndSampled<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            var enumerableDebugged = getter(carrier, B3HttpHeaderNames.B3Flags);
            if (enumerableDebugged != Enumerable.Empty<string>())
            {
                var debugged = enumerableDebugged.ToArray();
                if (debugged.Length != 0 && (debugged[0] == "0" || debugged[0] == "1"))
                {
                    return debugged[0] == "1" ? SamplingPriority.UserKeep : (SamplingPriority?)null;
                }
            }

            // Failed to "parse" debugged try sampling.
            return ParseB3Sampled(carrier, getter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SamplingPriority? ParseB3Sampled<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            var enumerableSampled = getter(carrier, B3HttpHeaderNames.B3Sampled);
            if (enumerableSampled != Enumerable.Empty<string>())
            {
                var sampled = enumerableSampled.ToArray();
                if (sampled.Length != 0 && (sampled[0] == "0" || sampled[0] == "1"))
                {
                    return sampled[0] == "1" ? SamplingPriority.AutoKeep : SamplingPriority.AutoReject;
                }
            }

            return (SamplingPriority?)null;
        }

        private KeyValuePair<string, string>? GetSamplingHeader(SpanContext context)
        {
            var samplingPriority = (int?)(context.TraceContext?.SamplingPriority ?? context.SamplingPriority);
            if (samplingPriority != null)
            {
                var value = samplingPriority < (int)SamplingPriority.AutoKeep ? "0" : "1";
                var header = samplingPriority == (int)SamplingPriority.UserKeep ? B3HttpHeaderNames.B3Flags : B3HttpHeaderNames.B3Sampled;

                return new KeyValuePair<string, string>(header, value);
            }

            return null;
        }
    }
}
