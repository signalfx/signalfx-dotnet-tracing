// Modified by SignalFx
using System;

namespace SignalFx.Tracing
{
    /// <summary>
    /// An API to access the active trace and span ids.
    /// </summary>
    public static class CorrelationIdentifier
    {
        /// <summary>
        /// Key used to correlate the trace ID on logs.
        /// </summary>
        public static readonly string TraceIdKey = "signalfx.trace_id";

        /// <summary>
        /// Key used to correlate the span ID on logs.
        /// </summary>
        public static readonly string SpanIdKey = "signalfx.span_id";

        /// <summary>
        /// Key used to correlate the environment on logs.
        /// </summary>
        public static readonly string ServiceNameKey = "signalfx.service";

        /// <summary>
        /// Key used to correlate the environment on logs.
        /// </summary>
        public static readonly string ServiceEnvironmentKey = "signalfx.environment";

        /// <summary>
        /// Gets the trace id
        /// </summary>
        public static ulong TraceId
        {
            get
            {
                return Tracer.Instance.ActiveScope?.Span?.TraceId ?? 0;
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
