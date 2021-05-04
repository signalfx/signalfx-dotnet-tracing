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
            var headerValues = getter(carrier, headerName).ToList();

            foreach (var traceId in headerValues.Select(TraceId.CreateFromString).Where(traceId => traceId != TraceId.Zero))
            {
                return traceId;
            }

            logger.Warning("Could not parse {HeaderName} headers: {HeaderValues}", headerName, string.Join(",", headerValues));
            return TraceId.Zero;
        }

        public static string ParseString<T>(T carrier, Func<T, string, IEnumerable<string>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName);

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
