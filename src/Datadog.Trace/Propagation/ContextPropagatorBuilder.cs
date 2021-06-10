// Modified by SignalFx

using System;
using SignalFx.Tracing.Configuration;

namespace SignalFx.Tracing.Propagation
{
    internal static class ContextPropagatorBuilder
    {
        public static IPropagator BuildPropagator(PropagatorType propagator)
        {
            switch (propagator)
            {
                case PropagatorType.B3:
                case PropagatorType.Default:
                    return new B3SpanContextPropagator();
                case PropagatorType.W3C:
                    return W3CSpanContextPropagator.Instance;
                default:
                    throw new InvalidOperationException($"There is no propagator registered for type '{propagator}'");
            }
        }
    }
}
