using Datadog.Tracer.Pprof.Proto.Profile.V1;

namespace Datadog.Trace.AlwaysOnProfiler.Builder
{
    internal class SampleBuilder
    {
        private readonly Sample _sample = new();

        public SampleBuilder AddLabel(Label label)
        {
            _sample.Labels.Add(label);
            return this;
        }

        public Sample Build() => _sample;
    }
}
