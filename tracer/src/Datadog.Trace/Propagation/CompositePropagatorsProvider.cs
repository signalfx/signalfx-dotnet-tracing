using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Propagation
{
    internal class CompositePropagatorsProvider
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<CompositePropagatorsProvider>();

        private readonly ICollection<IPropagatorsProvider> _providers;

        public CompositePropagatorsProvider()
        {
            _providers = new List<IPropagatorsProvider>();
        }

        public void RegisterProvider(IPropagatorsProvider provider)
        {
            _providers.Add(provider);
        }

        public IEnumerable<IPropagator> GetPropagators(IEnumerable<string> propagatorIds)
        {
            return propagatorIds.Select(GetPropagator).ToList();
        }

        private IPropagator GetPropagator(string propagatorId)
        {
            var propagator = _providers
                .Where(x => x.CanProvide(propagatorId))
                .Select(x => x.GetPropagator(propagatorId))
                .FirstOrDefault();

            if (propagator == null)
            {
                string msg = $"There is no propagator registered for type '{propagatorId}'.";

                Log.Error(msg);

                throw new InvalidOperationException(msg);
            }

            return propagator;
        }
    }
}
