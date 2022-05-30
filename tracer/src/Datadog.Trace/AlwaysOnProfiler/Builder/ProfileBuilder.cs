using Datadog.Tracer.Pprof.Proto.Profile.V1;

namespace Datadog.Trace.AlwaysOnProfiler.Builder
{
    internal class ProfileBuilder
    {
        private Profile _profile = new Profile();

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
    }
}
