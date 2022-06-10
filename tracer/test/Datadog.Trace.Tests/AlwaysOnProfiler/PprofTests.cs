// Modified by Splunk Inc.

using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.AlwaysOnProfiler.Builder;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler
{
    public class PprofTests
    {
        private readonly Pprof _pprof = new();

        [Fact]
        public void GetLocationId()
        {
            var pprof = new Pprof();
            var locationId1 = pprof.GetLocationId("unknown", "A()", 0);
            var locationId2 = pprof.GetLocationId("unknown", "A()", 0);
            var locationId3 = pprof.GetLocationId("unknown", "B()", 0);

            var profile = pprof.ProfileBuilder.Build();

            locationId1.Should().Be(1);
            locationId2.Should().Be(1);
            locationId3.Should().Be(2);
            profile.StringTables.Should().HaveCount(4);
            profile.StringTables.Should().ContainInOrder(new List<string> { string.Empty, "unknown", "A()", "B()" });
        }

        [Fact]
        public void AddLabel()
        {
            var sampleBuilder = new SampleBuilder();

            _pprof.AddLabel(sampleBuilder, "long1", 100L);
            _pprof.AddLabel(sampleBuilder, "bool", true);
            _pprof.AddLabel(sampleBuilder, "long2", 32132L);
            _pprof.AddLabel(sampleBuilder, "string", "value");

            var sample = sampleBuilder.Build();
            var profile = _pprof.ProfileBuilder.Build();

            sample.Labels.Should().HaveCount(4);
            sample.Labels.Should().Contain(pair => pair.Key == 1 && pair.Num == 100L);
            sample.Labels.Should().Contain(pair => pair.Key == 2 && pair.Str == 3);
            sample.Labels.Should().Contain(pair => pair.Key == 4 && pair.Num == 32132L);
            sample.Labels.Should().Contain(pair => pair.Key == 5 && pair.Str == 6);
            profile.StringTables.Should().HaveCount(7);
            profile.StringTables.Should().ContainInOrder(new List<string> { string.Empty, "long1", "bool", "True", "long2", "string", "value" });
        }

        [Fact]
        public void GetStringId()
        {
            var id1 = _pprof.GetStringId("New string");
            var id2 = _pprof.GetStringId("Second string");
            var id3 = _pprof.GetStringId("New string");

            var profile = _pprof.ProfileBuilder.Build();

            id1.Should().Be(1);
            id2.Should().Be(2);
            id3.Should().Be(id1);
            profile.StringTables.Should().HaveCount(3);
            profile.StringTables.Should().ContainInOrder(new List<string> { string.Empty, "New string", "Second string" });
        }
    }
}
