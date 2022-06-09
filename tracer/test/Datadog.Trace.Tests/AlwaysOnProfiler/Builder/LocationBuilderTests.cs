// Modified by Splunk Inc.

using Datadog.Trace.AlwaysOnProfiler.Builder;
using Datadog.Tracer.Pprof.Proto.Profile;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler.Builder
{
    public class LocationBuilderTests
    {
        private readonly LocationBuilder _locationBuilder = new();

        [Fact]
        public void Id()
        {
            _locationBuilder.Id = 10;

            var location = _locationBuilder.Build();

            location.Id.Should().Be(10);
        }

        [Fact]
        public void AddLine()
        {
            var line = new Line();
            _locationBuilder.AddLine(line);

            var location = _locationBuilder.Build();

            location.Lines.Should().HaveCount(1);
            location.Lines[0].Should().Be(line);
        }
    }
}
