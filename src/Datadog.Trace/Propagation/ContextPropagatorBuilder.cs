// Modified by SignalFx

using System;
using System.Collections.Generic;
using SignalFx.Tracing.Configuration;

namespace SignalFx.Tracing.Propagation
{
    internal static class ContextPropagatorBuilder
    {
        private static readonly IReadOnlyDictionary<PropagatorType, Func<IPropagator>> _propagatorSelector =
            new Dictionary<PropagatorType, Func<IPropagator>>()
            {
                { PropagatorType.B3, () => new B3SpanContextPropagator() },
                { PropagatorType.Default, () => new B3SpanContextPropagator() },
                { PropagatorType.W3C, () => W3CSpanContextPropagator.Instance }
            };

        public static IPropagator BuildPropagator(PropagatorType propagator)
        {
            if (_propagatorSelector.TryGetValue(propagator, out var getter))
            {
                return getter();
            }

            throw new InvalidOperationException($"There is no propagator registered for type '{propagator}'");
        }
    }
}
