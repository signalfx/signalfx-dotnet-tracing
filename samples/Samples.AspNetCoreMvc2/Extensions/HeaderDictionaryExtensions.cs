// Modified by SignalFx
using System;
using System.Globalization;
using System.Linq;
using Datadog.Trace;
using Microsoft.AspNetCore.Http;

namespace Samples.AspNetCoreMvc2.Extensions
{
    public static class HeaderDictionaryExtensions
    {
        public static SpanContext Extract(this IHeaderDictionary headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            ulong traceId = 0;
            ulong parentId = 0;
            SamplingPriority? samplingPriority = null;

            if (headers.TryGetValue(HttpHeaderNames.B3TraceId, out var traceIdHeaders))
            {
                ulong.TryParse(traceIdHeaders.FirstOrDefault(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceId);
            }

            if (traceId == 0)
            {
                // a valid traceId is required to use distributed tracing
                return null;
            }

            if (headers.TryGetValue(HttpHeaderNames.B3SpanId, out var parentIdHeaders))
            {
                ulong.TryParse(parentIdHeaders.FirstOrDefault(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parentId);
            }

            if (headers.TryGetValue(HttpHeaderNames.B3Sampled, out var sampledHeaders) &&
                int.TryParse(sampledHeaders.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sampledValue))
            {
                // There is no 1:1 mapping between B3 sampled/debug flags and SamplingPriority.
                // Will map to AutoReject or AutoKeep.
                samplingPriority = (SamplingPriority?)sampledValue;
            }
            else if (headers.TryGetValue(HttpHeaderNames.B3Flags, out var debuggedHeaders) &&
                int.TryParse(debuggedHeaders.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var debugValue))
            {
                // Add 1 to coerce to UserKeep.
                samplingPriority = (SamplingPriority?)debugValue++;
            }

            return new SpanContext(traceId, parentId, samplingPriority);
        }
    }
}
