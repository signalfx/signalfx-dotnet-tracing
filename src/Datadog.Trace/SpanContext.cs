// Modified by SignalFx
using System;
using System.Threading;
using Datadog.Trace.Abstractions;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Logging;

namespace Datadog.Trace
{
    /// <summary>
    /// The SpanContext contains all the information needed to express relationships between spans inside or outside the process boundaries.
    /// </summary>
    public class SpanContext : ISpanContext
    {
        /// <summary>
        /// This bit mask is also the maximum value of an ID. It is used to remove fixed bits from
        /// the generated IDs.
        /// </summary>
        internal const ulong RandomIdBitMask = 0x0fffffffffffffff;

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.For<SpanContext>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class
        /// from a propagated context. <see cref="Parent"/> will be null
        /// since this is a root context locally.
        /// </summary>
        /// <param name="traceId">The propagated trace id.</param>
        /// <param name="spanId">The propagated span id.</param>
        /// <param name="samplingPriority">The propagated sampling priority.</param>
        /// <param name="serviceName">The service name to propagate to child spans.</param>
        public SpanContext(ulong? traceId, ulong spanId, SamplingPriority? samplingPriority, string serviceName = null)
            : this(traceId, serviceName)
        {
            SpanId = spanId;
            SamplingPriority = samplingPriority;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class
        /// that is the child of the specified parent context.
        /// </summary>
        /// <param name="parent">The parent context.</param>
        /// <param name="traceContext">The trace context.</param>
        /// <param name="serviceName">The service name to propagate to child spans.</param>
        internal SpanContext(ISpanContext parent, ITraceContext traceContext, string serviceName)
            : this(parent?.TraceId, serviceName)
        {
            Parent = parent;
            TraceContext = traceContext;

            if (SpanId == 0)
            {
                SpanId = GenerateId();
            }
        }

        private SpanContext(ulong? traceId, string serviceName)
        {
            ServiceName = serviceName;
            if (traceId > 0)
            {
                TraceId = traceId.Value;
                return;
            }

            // This is the root span.
            TraceId = GenerateId();
            SpanId = TraceId;
        }

        /// <summary>
        /// Gets the parent context.
        /// </summary>
        public ISpanContext Parent { get; }

        /// <summary>
        /// Gets the trace id
        /// </summary>
        public ulong TraceId { get; }

        /// <summary>
        /// Gets the span id of the parent span
        /// </summary>
        public ulong? ParentId => Parent?.SpanId;

        /// <summary>
        /// Gets the span id
        /// </summary>
        public ulong SpanId { get; }

        /// <summary>
        /// Gets or sets the service name to propagate to child spans.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets the trace context.
        /// Returns null for contexts created from incoming propagated context.
        /// </summary>
        internal ITraceContext TraceContext { get; }

        /// <summary>
        /// Gets the sampling priority for contexts created from incoming propagated context.
        /// Returns null for local contexts.
        /// </summary>
        internal SamplingPriority? SamplingPriority { get; }

        /// <summary>
        /// Gets or sets the span associated with this context.
        /// </summary>
        internal ISpan Span { get; set; }

        private static ulong GenerateId()
        {
            return BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0) & RandomIdBitMask;
        }
    }
}
