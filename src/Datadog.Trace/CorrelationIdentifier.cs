// Modified by SignalFx
using System;

namespace Datadog.Trace
{
    /// <summary>
    /// An API to access the active trace and span ids.
    /// </summary>
    public static class CorrelationIdentifier
    {
        internal static readonly string TraceIdKey = "signalfx.trace_id";
        internal static readonly string SpanIdKey = "signalfx.span_id";

        /// <summary>
        /// Gets the trace id
        /// </summary>
        public static Guid TraceId
        {
            get
            {
                return Tracer.Instance.ActiveScope?.Span != null ? Tracer.Instance.ActiveScope.Span.TraceId : Guid.Empty;
            }
        }

        /// <summary>
        /// Gets the span id
        /// </summary>
        public static ulong SpanId
        {
            get
            {
                return Tracer.Instance.ActiveScope?.Span?.SpanId ?? 0;
            }
        }
    }
}
