// Modified by Splunk Inc.

using Datadog.Trace.AlwaysOnProfiler.Builder;
using Datadog.Tracer.Pprof.Proto.Profile.V1;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler.Builder
{
    public class SampleBuilderTests
    {
        private readonly SampleBuilder _sampleBuilder = new();

        [Fact]
        public void AddLocationId()
        {
            _sampleBuilder.AddLocationId(100);

            var sample = _sampleBuilder.Build();

            sample.LocationIds.Should().HaveCount(1);
            sample.LocationIds.Should().Contain(100);
        }

        [Fact]
        public void AddLabel()
        {
            var label = new Label();
            _sampleBuilder.AddLabel(label);

            var sample = _sampleBuilder.Build();

            sample.Labels.Should().HaveCount(1);
            sample.Labels[0].Should().Be(label);
        }

        [Fact]
        public void AddValue()
        {
            _sampleBuilder.AddValue(100);

            var sample = _sampleBuilder.Build();

            sample.Values.Should().HaveCount(1);
            sample.Values[0].Should().Be(100);
        }
    }
}
