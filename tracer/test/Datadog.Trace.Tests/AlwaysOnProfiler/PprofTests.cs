using Datadog.Trace.AlwaysOnProfiler;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler
{
    public class PprofTests
    {
        [Fact]
        public void Test()
        {
            var pprof = new Pprof();
            var locationId1 = pprof.GetLocationId("unknown", "A()", 0);
            var locationId2 = pprof.GetLocationId("unknown", "A()", 0);
            var locationId3 = pprof.GetLocationId("unknown", "B()", 0);

            locationId1.Should().Be(1);
            locationId2.Should().Be(1);
            locationId3.Should().Be(2);
        }
    }
}
