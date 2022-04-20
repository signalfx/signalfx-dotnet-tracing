// Modified by Splunk Inc.

using System;
using Datadog.Trace.Propagators;

namespace Datadog.Trace
{
    /// <summary>
    /// Adds Server-Timing (and Access-Control-Expose-Headers) header to the HTTP
    /// response. The Server-Timing header contains the traceId and spanId of the server span.
    /// </summary>
    public static class ServerTimingHeader
    {
        /// <summary>
        /// Key of the "Server-Timing" header.
        /// </summary>
        public const string Key = "Server-Timing";

        private const string ExposeHeadersHeaderName = "Access-Control-Expose-Headers";
        private const string ServerTimingFormat = "traceparent;desc=\"{0}\"";

        /// <summary>
        /// Sets the Server-Timing (and Access-Control-Expose-Headers) headers.
        /// </summary>
        /// <param name="context">Current <see cref="SpanContext"/></param>
        /// <param name="carrier">Object on which the headers will be set</param>
        /// <param name="setter">Action for how to set the header</param>
        /// <typeparam name="T">Type of the carrier</typeparam>
        public static void SetHeaders<T>(SpanContext context, T carrier, Action<T, string, string> setter)
        {
            if (Tracer.Instance.Settings.TraceResponseHeaderEnabled)
            {
                setter(carrier, Key, ToHeaderValue(context));
                setter(carrier, ExposeHeadersHeaderName, Key);
            }
        }

        private static string ToHeaderValue(SpanContext context)
        {
            return string.Format(ServerTimingFormat, W3CContextPropagator.GetFormattedTraceParent(context));
        }
    }
}
