// Modified by Splunk Inc.

using System.Collections.Generic;
using Datadog.Tracer.Pprof.Proto.Profile.V1;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class ProfileBuilder
    {
        public const int TraceIdIndex = 0;
        public const int SpanIdIndex = 1;

        private readonly Profile _profile;

        public ProfileBuilder()
        {
            _profile = new Profile { StringTables = { "TRACE_ID", "SPAN_ID" } };
        }

        public List<string> StringTable => _profile.StringTables;

        public void AddSample(Sample sample)
        {
            _profile.Samples.Add(sample);
        }

        public Profile Build() => _profile;
    }
}
