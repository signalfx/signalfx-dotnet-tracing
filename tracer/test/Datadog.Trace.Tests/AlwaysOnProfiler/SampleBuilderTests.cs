// Modified by Splunk Inc.

using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Tracer.Pprof.Proto.Profile;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler
{
    public class SampleBuilderTests
    {
        [Fact]
        public void AddLocationId()
        {
            SampleBuilder sampleBuilder = new();
            sampleBuilder.AddLocationId(100);

            var sample = sampleBuilder.Build();

            sample.LocationIds.Should().HaveCount(1);
            sample.LocationIds.Should().Contain(100);
        }

        [Fact]
        public void AddLabel()
        {
            var label = new Label();
            SampleBuilder sampleBuilder = new();
            sampleBuilder.AddLabel(label);

            var sample = sampleBuilder.Build();

            sample.Labels.Should().HaveCount(1);
            sample.Labels[0].Should().Be(label);
        }

        [Fact]
        public void SetValue()
        {
            var builder = new SampleBuilder();
            builder.SetValue(1);
            var sample = builder.Build();

            sample.Values.Should().HaveCount(1);
            sample.Values[0].Should().Be(1);
        }

        [Fact]
        public void DefaultValue()
        {
            var builder = new SampleBuilder();
            var sample = builder.Build();

            sample.Values.Should().BeEmpty();
        }
    }
}
