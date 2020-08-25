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
        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.For<SpanContext>();
        private static ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class
        /// from a propagated context. <see cref="Parent"/> will be null
        /// since this is a root context locally.
        /// </summary>
        /// <param name="traceId">The propagated trace id.</param>
        /// <param name="spanId">The propagated span id.</param>
        /// <param name="samplingPriority">The propagated sampling priority.</param>
        /// <param name="serviceName">The service name to propagate to child spans.</param>
        public SpanContext(Guid traceId, ulong spanId, SamplingPriority? samplingPriority, string serviceName = null)
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
            : this(parent != null ? parent.TraceId : Guid.Empty, serviceName)
        {
            SpanId = _random.Value.NextUInt63();
            Parent = parent;
            TraceContext = traceContext;
        }

        private SpanContext(Guid traceId, string serviceName)
        {
            TraceId = traceId != Guid.Empty
                          ? traceId
                          : Guid.NewGuid();

            ServiceName = serviceName;
        }

        /// <summary>
        /// Gets the parent context.
        /// </summary>
        public ISpanContext Parent { get; }

        /// <summary>
        /// Gets the trace id
        /// </summary>
        public Guid TraceId { get; }

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
    }
}
