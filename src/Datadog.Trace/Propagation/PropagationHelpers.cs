// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Linq;

namespace SignalFx.Tracing.Propagation
{
    internal static class PropagationHelpers
    {
        public static TraceId ParseTraceId<T>(T carrier, Func<T, string, IEnumerable<string>> getter, string headerName, Vendors.Serilog.ILogger logger)
        {
            var enumerableHeaderValues = getter(carrier, headerName);
            if (enumerableHeaderValues == Enumerable.Empty<string>())
            {
                return TraceId.Zero;
            }

            var headerValues = enumerableHeaderValues.ToList();
            if (headerValues.Count == 0)
            {
                return TraceId.Zero;
            }

            for (var i = 0; i < headerValues.Count; ++i)
            {
                var traceId = TraceId.CreateFromString(headerValues[i]);
                if (traceId != TraceId.Zero)
                {
                    return traceId;
                }
            }

            logger.Debug("Could not parse {HeaderName} headers: {HeaderValues}", headerName, string.Join(",", headerValues));
            return TraceId.Zero;
        }
    }
}
