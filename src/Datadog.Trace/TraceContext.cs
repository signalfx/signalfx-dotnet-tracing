// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.PlatformHelpers;

namespace SignalFx.Tracing
{
    internal class TraceContext : ITraceContext
    {
        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.For<TraceContext>();
        private static readonly Tracing.SamplingPriority? DefaultSamplingPriority = new Tracing.SamplingPriority?(Tracing.SamplingPriority.AutoKeep);
        private static readonly double TickFrequency = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

        private readonly DateTimeOffset _utcStart = DateTimeOffset.UtcNow;
        private readonly long _startTimestamp = Stopwatch.GetTimestamp();
        private readonly List<Span> _spans = new List<Span>();

        private int _openSpans;
        private SamplingPriority? _samplingPriority;
        private bool _samplingPriorityLocked;

        public TraceContext(ISignalFxTracer tracer)
        {
            Tracer = tracer;
        }

        public Span RootSpan { get; private set; }

        public DateTimeOffset UtcNow => _utcStart.AddTicks((long)((Stopwatch.GetTimestamp() - _startTimestamp) * TickFrequency));

        public ISignalFxTracer Tracer { get; }

        /// <summary>
        /// Gets or sets sampling priority.
        /// Once the sampling priority is locked with <see cref="LockSamplingPriority"/>,
        /// further attempts to set this are ignored.
        /// </summary>
        public SamplingPriority? SamplingPriority
        {
            get => _samplingPriority;
            set
            {
                if (!_samplingPriorityLocked)
                {
                    _samplingPriority = value;
                }
            }
        }

        public void AddSpan(Span span)
        {
            lock (_spans)
            {
                if (RootSpan == null)
                {
                    // first span added is the root span
                    RootSpan = span;
                    DecorateRootSpan(span);

                    if (_samplingPriority == null)
                    {
                        if (span.Context.Parent is SpanContext context && context.SamplingPriority != null)
                        {
                            // this is a root span created from a propagated context that contains a sampling priority.
                            // lock sampling priority when a span is started from a propagated trace.
                            _samplingPriority = context.SamplingPriority;
                            LockSamplingPriority();
                        }
                        else
                        {
                            // this is a local root span (i.e. not propagated).
                            // determine an initial sampling priority for this trace, but don't lock it yet
                            _samplingPriority = Tracer.Sampler == null
                                ? DefaultSamplingPriority
                                : Tracer.Sampler.GetSamplingPriority(RootSpan);
                        }
                    }
                }

                _spans.Add(span);
                _openSpans++;
            }
        }

        public void CloseSpan(Span span)
        {
            if (span == RootSpan)
            {
                // lock sampling priority and set metric when root span finishes
                LockSamplingPriority();
            }

            Span[] spansToWrite = null;

            lock (_spans)
            {
                _openSpans--;

                if (_openSpans == 0)
                {
                    spansToWrite = _spans.ToArray();
                    _spans.Clear();
                }
            }

            if (spansToWrite != null)
            {
                Tracer.Write(spansToWrite);
            }
        }

        public void LockSamplingPriority()
        {
            if (_samplingPriority == null)
            {
                Log.Warning("Cannot lock sampling priority before it has been set.");
            }
            else
            {
                _samplingPriorityLocked = true;
            }
        }

        private void DecorateRootSpan(Span span)
        {
            if (AzureAppServices.Metadata?.IsRelevant ?? false)
            {
                span.SetTag(Tags.AzureAppServicesSiteName, AzureAppServices.Metadata.SiteName);
                span.SetTag(Tags.AzureAppServicesResourceGroup, AzureAppServices.Metadata.ResourceGroup);
                span.SetTag(Tags.AzureAppServicesSubscriptionId, AzureAppServices.Metadata.SubscriptionId);
                span.SetTag(Tags.AzureAppServicesResourceId, AzureAppServices.Metadata.ResourceId);
            }
        }
    }
}
