using Datadog.Tracer.Pprof.Proto.Profile.V1;

namespace Datadog.Trace.AlwaysOnProfiler.Builder
{
    internal class ProfileBuilder
    {
        private readonly Profile _profile = new();

        public ProfileBuilder AddStringTable(string str)
        {
            _profile.StringTables.Add(str);
            return this;
        }

        public ProfileBuilder AddFunction(Function function)
        {
            _profile.Functions.Add(function);
            return this;
        }

        public ProfileBuilder AddLocation(Location location)
        {
            _profile.Locations.Add(location);
            return this;
        }

        public ProfileBuilder AddSample(Sample sample)
        {
            _profile.Samples.Add(sample);
            return this;
        }

        public Profile Build() => _profile;
    }
}
