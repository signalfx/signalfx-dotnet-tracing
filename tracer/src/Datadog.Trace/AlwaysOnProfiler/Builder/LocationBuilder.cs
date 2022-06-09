// Modified by Splunk Inc.

using Datadog.Tracer.Pprof.Proto.Profile;

namespace Datadog.Trace.AlwaysOnProfiler.Builder
{
    internal class LocationBuilder
    {
        private readonly Location _location = new();

        public ulong Id
        {
            set
            {
                _location.Id = value;
            }
        }

        public LocationBuilder AddLine(Line line)
        {
            _location.Lines.Add(line);
            return this;
        }

        public Location Build() => _location;
    }
}
