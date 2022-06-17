// Modified by Splunk Inc.

using System.Collections.Generic;
using System.Linq;
using Datadog.Tracer.Pprof.Proto.Profile;

namespace Datadog.Trace.AlwaysOnProfiler.Builder
{
    internal class SampleBuilder
    {
        private readonly Sample _sample = new();
        private readonly IList<long> _values = new List<long>();
        private readonly IList<ulong> _locationIds = new List<ulong>();

        public SampleBuilder AddLabel(Label label)
        {
            _sample.Labels.Add(label);
            return this;
        }

        public SampleBuilder AddValue(long value)
        {
            _values.Add(value);
            return this;
        }

        public SampleBuilder AddLocationId(ulong locationId)
        {
            _locationIds.Add(locationId);
            return this;
        }

        public Sample Build()
        {
            _sample.Values = _values.ToArray();
            _sample.LocationIds = _locationIds.ToArray();

            return _sample;
        }
    }
}
