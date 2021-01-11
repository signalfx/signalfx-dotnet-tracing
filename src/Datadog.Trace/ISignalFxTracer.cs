using System;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.Sampling;

namespace SignalFx.Tracing
{
    internal interface ISignalFxTracer
    {
        string DefaultServiceName { get; }

        IScopeManager ScopeManager { get; }

        ISampler Sampler { get; }

        TracerSettings Settings { get; }

        Span StartSpan(string operationName);

        Span StartSpan(string operationName, ISpanContext parent);

        Span StartSpan(string operationName, ISpanContext parent, string serviceName, DateTimeOffset? startTime, bool ignoreActiveScope);

        void Write(Span[] span);

        /// <summary>
        /// Make a span the active span and return its new scope.
        /// </summary>
        /// <param name="span">The span to activate.</param>
        /// <returns>A Scope object wrapping this span.</returns>
        Scope ActivateSpan(Span span);

        /// <summary>
        /// Make a span the active span and return its new scope.
        /// </summary>
        /// <param name="span">The span to activate.</param>
        /// <param name="finishOnClose">Determines whether closing the returned scope will also finish the span.</param>
        /// <returns>A Scope object wrapping this span.</returns>
        Scope ActivateSpan(Span span, bool finishOnClose);
    }
}
