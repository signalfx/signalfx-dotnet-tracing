using System;

namespace SignalFx.Tracing
{
    internal interface ITraceContext
    {
        DateTimeOffset UtcNow { get; }

        SamplingPriority? SamplingPriority { get; set; }

        Span RootSpan { get; }

        void AddSpan(Span span);

        void CloseSpan(Span span);

        void LockSamplingPriority();
    }
}
