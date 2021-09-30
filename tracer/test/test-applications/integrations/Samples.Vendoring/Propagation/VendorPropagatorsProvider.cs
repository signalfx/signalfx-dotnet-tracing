using System;
using System.Collections.Generic;
using Datadog.Trace.Propagation;
using Samples.Vendoring.Types;

namespace Samples.Vendoring.Propagation
{
    public class VendorPropagatorsProvider : IPropagatorsProvider
    {
        private static readonly IReadOnlyDictionary<string, Func<IPropagator>> PropagatorSelector =
            new Dictionary<string, Func<IPropagator>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { VendorPropagatorTypes.VendorPropagator, () => new VendorSpanContextPropagator() }
            };

        public bool CanProvide(string propagatorId)
        {
            return PropagatorSelector.ContainsKey(propagatorId);
        }

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
