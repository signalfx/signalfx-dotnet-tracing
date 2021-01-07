using System;

namespace SignalFx.Tracing.Abstractions
{
    internal interface IScope : IDisposable
    {
        ISpan Span { get; }
    }
}
