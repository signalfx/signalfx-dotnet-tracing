// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Tracer.Pprof.Proto.Profile;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class SampleBuilder
    {
        private readonly Sample _sample = new();
        private readonly IList<ulong> _locationIds = new List<ulong>();
        private long? _value;

        public SampleBuilder AddLabel(Label label)
        {
            _sample.Labels.Add(label);
            return this;
        }

        public SampleBuilder SetValue(long val)
        {
            _value = val;
            return this;
        }

        public SampleBuilder AddLocationId(ulong locationId)
        {
            _locationIds.Add(locationId);
            return this;
        }

        public Sample Build()
        {
            _sample.LocationIds = _locationIds.ToArray();
            _sample.Values = _value.HasValue ? new[] { _value.Value } : Array.Empty<long>();

            return _sample;
        }
    }
}
