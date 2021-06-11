// Modified by SignalFx
using System;
using System.Threading;
using SignalFx.Tracing.Abstractions;
using SignalFx.Tracing.ExtensionMethods;
using SignalFx.Tracing.Logging;

namespace SignalFx.Tracing
{
    /// <summary>
    /// The SpanContext contains all the information needed to express relationships between spans inside or outside the process boundaries.
    /// </summary>
    public class SpanContext : ISpanContext
    {
        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.For<SpanContext>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class
        /// from a propagated context. <see cref="Parent"/> will be null
        /// since this is a root context locally.
        /// </summary>
        /// <param name="traceId">The propagated trace id.</param>
        /// <param name="spanId">The propagated span id.</param>
        /// <param name="samplingPriority">The propagated sampling priority.</param>
        /// <param name="serviceName">The service name to propagate to child spans.</param>
        /// <param name="traceState">The W3C tracestate.</param>
        public SpanContext(TraceId? traceId, ulong spanId, SamplingPriority? samplingPriority, string serviceName = null, string traceState = null)
            : this(traceId, serviceName)
        {
            SpanId = spanId;
            SamplingPriority = samplingPriority;
            TraceState = traceState;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class
        /// that is the child of the specified parent context.
        /// </summary>
        /// <param name="parent">The parent context.</param>
        /// <param name="traceContext">The trace context.</param>
        /// <param name="serviceName">The service name to propagate to child spans.</param>
        /// <param name="spanId">The span ID.</param>
        internal SpanContext(ISpanContext parent, ITraceContext traceContext, string serviceName, ulong? spanId = null)
            : this(parent?.TraceId, serviceName)
        {
            Parent = parent;
            TraceContext = traceContext;

            // If spanId was provided this means that a span context already existed but no span was created for it.
            // This can happen when a span context is propagated or created for a network call but no actual span
            // was created for it yet. See WebRequest.GetRequestStream for an example.
            if (spanId != null)
            {
                SpanId = spanId.Value;
            }

            if (SpanId == 0)
            {
                SpanId = RandomNumberGenerator.Current.Next();
            }
        }

        private SpanContext(TraceId? traceId, string serviceName)
        {
            ServiceName = serviceName;
            if (traceId.HasValue)
            {
                TraceId = traceId.Value;
                return;
            }

            TraceId = TraceId.CreateRandom();
            SpanId = RandomNumberGenerator.Current.Next();
        }

        /// <summary>
        /// Gets the parent context.
        /// </summary>
        public ISpanContext Parent { get; }

        /// <summary>
        /// Gets the trace id
        /// </summary>
        public TraceId TraceId { get; }

        /// <summary>
        /// Gets the span id of the parent span
        /// </summary>
        public ulong? ParentId => Parent?.SpanId;

        /// <summary>
        /// Gets the span id
        /// </summary>
        public ulong SpanId { get; }

        /// <summary>
        /// Gets the trace state for W3C propagation
        /// </summary>
        public string TraceState { get; }

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
    }
}
