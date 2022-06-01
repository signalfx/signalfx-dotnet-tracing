using Datadog.Trace.AlwaysOnProfiler.Builder;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler.Builder
{
    public class SampleBuilderTests
    {
        [Fact]
        public void Test()
        {
            var sampleBuilder = new SampleBuilder();
            sampleBuilder.AddLocationId(100);

            var sample = sampleBuilder.Build();

            sample.LocationIds.Should().Contain(100);
        }
    }
}
