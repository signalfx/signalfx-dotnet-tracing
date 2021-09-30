using System;
using System.Collections.Generic;
using Datadog.Trace.Configuration.Types;

namespace Datadog.Trace.Propagation
{
    internal class OTelPropagatorsProvider : IPropagatorsProvider
    {
        private static readonly IReadOnlyDictionary<string, Func<IPropagator>> PropagatorSelector =
            new Dictionary<string, Func<IPropagator>>(StringComparer.InvariantCultureIgnoreCase)
            {
                        { PropagatorTypes.W3C, () => new W3CSpanContextPropagator() },
                        { PropagatorTypes.B3, () => new B3SpanContextPropagator() },
                        { PropagatorTypes.Datadog, () => new DDSpanContextPropagator() },
            };

        public bool CanProvide(string propagatorId)
        {
            return PropagatorSelector.ContainsKey(propagatorId);
        }

        /// <summary>
        /// Builds the propagator with given spec.
        /// </summary>
        /// <param name="propagatorId">Propagator id.</param>
        /// <returns>Context propagator.</returns>
        public IPropagator GetPropagator(string propagatorId)
        {
            if (PropagatorSelector.TryGetValue(propagatorId, out Func<IPropagator> getter))
            {
                return getter();
            }

            throw new InvalidOperationException($"There is no propagator registered for type '{propagatorId}'.");
        }
    }
}
