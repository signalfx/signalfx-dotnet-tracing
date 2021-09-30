using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Propagation
{
    internal static class PropagationHelpers
    {
        public static TraceId ParseTraceId<T>(T carrier, Func<T, string, IEnumerable<string>> getter, string headerName, IDatadogLogger logger)
        {
            var enumerableHeaderValues = getter(carrier, headerName) ?? Enumerable.Empty<string>();
            if (enumerableHeaderValues == Enumerable.Empty<string>())
            {
                return TraceId.Zero;
            }

            var headerValues = enumerableHeaderValues.ToArray();
            if (headerValues.Length == 0)
            {
                return TraceId.Zero;
            }

            foreach (var headerValue in headerValues)
            {
                if (TraceId.TryParse(headerValue, out var traceId))
                {
                    return traceId;
                }
            }

            logger.Debug("Could not parse {HeaderName} headers: {HeaderValues}", headerName, string.Join(",", headerValues));
            return TraceId.Zero;
        }

        public static string ParseString<T>(T carrier, Func<T, string, IEnumerable<string>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName) ?? Enumerable.Empty<string>();

            foreach (var headerValue in headerValues)
            {
                if (!string.IsNullOrEmpty(headerValue))
                {
                    return headerValue;
                }
            }

            return null;
        }
    }
}
