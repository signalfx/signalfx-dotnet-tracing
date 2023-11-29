using System.Collections.Generic;
using System.Threading;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers;
using Datadog.Trace.Vendors.Newtonsoft.Json.Utilities;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

public class ProfileTypeBuilderTests
{
    [Fact]
    public void ValidateBuildNoSamples()
    {
        var profileLookupTables = new ProfileLookupTables();
        var mockBuilder = new MockConcreteBuilder();

        mockBuilder.NextSamples = null;
        var buildNullSamplesResult = mockBuilder.Build(profileLookupTables);
        buildNullSamplesResult.ContainsData.Should().BeFalse();
        buildNullSamplesResult.ProfileType.Should().BeNull();
        buildNullSamplesResult.StartTimestamp.Should().BeNull();
        buildNullSamplesResult.EndTimestamp.Should().BeNull();

        mockBuilder.NextSamples = new List<ThreadSample>();
        var buildEmptySamplesResult = mockBuilder.Build(profileLookupTables);
        buildEmptySamplesResult.ContainsData.Should().BeFalse();
        buildEmptySamplesResult.ProfileType.Should().BeNull();
        buildEmptySamplesResult.StartTimestamp.Should().BeNull();
        buildEmptySamplesResult.EndTimestamp.Should().BeNull();
    }

    [Fact]
    public void ValidateBuild()
    {
        var threadSamples = new List<ThreadSample>
        {
            new() { ThreadName = "thread.1", ManagedId = 1, Timestamp = new ThreadSample.Time(2) },
            new() { ThreadName = "thread.2", ManagedId = 2, Timestamp = new ThreadSample.Time(4) },
            new() { ThreadName = "thread.1", ManagedId = 1, Timestamp = new ThreadSample.Time(6) }
        };
        threadSamples.ForEach(ts => ts.Frames.AddRange(new List<string> { "test_function_0", "test_function_1" }));

        var profileLookupTables = new ProfileLookupTables();
        var mockBuilder = new MockConcreteBuilder();

        mockBuilder.NextSamples = threadSamples;
        var buildResult = mockBuilder.Build(profileLookupTables);
        buildResult.ContainsData.Should().BeTrue();
        buildResult.StartTimestamp.Nanoseconds.Should().Be(2_000_000);
        buildResult.EndTimestamp.Nanoseconds.Should().Be(6_000_000);
        buildResult.ProfileType.Should().NotBeNull();

        var builtProfileType = buildResult.ProfileType;
        builtProfileType.SampleRate.Should().Be(42);
        profileLookupTables.GetStringIndex("test_type").Should().Be(builtProfileType.TypeIndex);
        profileLookupTables.GetStringIndex("test_unit").Should().Be(builtProfileType.UnitIndex);

        builtProfileType.StacktraceIndices.Should().HaveCount(3);
        builtProfileType.StacktraceIndices.Should().AllBeEquivalentTo(0);
        builtProfileType.Timestamps.Should().BeEquivalentTo(new List<ulong> { 2_000_000, 4_000_000, 6_000_000 });
        builtProfileType.Values.Should().AllBeEquivalentTo(13);
    }

    private class MockConcreteBuilder : ProfileTypeBuilder<ThreadSample>
    {
        internal long NextSampleValue { private get; set; } = 13;

        internal List<ThreadSample> NextSamples { private get; set; }

        protected override long CalculateSampleValue(ThreadSample threadSample) => NextSampleValue;

        protected override List<ThreadSample> RetrieveSamples() => NextSamples;

        protected override void SetProfileTypeInformation(
            ProfileLookupTables profileLookupTables,
            ProfileType profileType)
        {
            profileType.SampleRate = 42;
            profileType.TypeIndex = profileLookupTables.GetStringIndex("test_type");
            profileType.UnitIndex = profileLookupTables.GetStringIndex("test_unit");
        }
    }
}
