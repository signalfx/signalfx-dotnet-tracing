using System;
using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.Vendors.Newtonsoft.Json.Utilities;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

public class AllocationProfileTypeBuilderTests
{
    [Fact]
    public void ValidateProfileTypeInformation()
    {
        var sharedBuffer = Array.Empty<byte>();
        var allocationSample = new AllocationSample
        {
            ThreadName = "test.thread",
            ManagedId = 42,
            Timestamp = new ThreadSample.Time(1234),
            AllocationSizeBytes = 4321,
            TypeName = "TestType",
        };
        allocationSample.Frames.AddRange(new List<string> { "test_function_0", "test_function_1" });

        var cpuProfileTypeBuilder = new AllocationProfileTypeBuilder(
            sharedBuffer,
            () => new List<AllocationSample> { allocationSample });

        var profileTypeBuilder = new ProfileBuilder(new List<IProfileTypeBuilder> { cpuProfileTypeBuilder });
        var profile = profileTypeBuilder.Build();

        profile.ProfileTypes.Should().HaveCount(1);
        profile.StartTimeUnixNano.Should().Be(allocationSample.Timestamp.Nanoseconds);
        profile.EndTimeUnixNano.Should().Be(allocationSample.Timestamp.Nanoseconds);
        profile.Functions.Should().HaveCount(2);
        profile.Stacktraces.Should().HaveCount(1);

        var allocationProfileType = profile.ProfileTypes[0];
        profile.StringTables[(int)allocationProfileType.TypeIndex].Should().Be("allocation");
        profile.StringTables[(int)allocationProfileType.UnitIndex].Should().Be("bytes");
        allocationProfileType.SampleRate.Should().Be(100_000);
        allocationProfileType.Timestamps.Should().HaveCount(1);
        allocationProfileType.Timestamps[0].Should().Be(allocationSample.Timestamp.Nanoseconds);
        allocationProfileType.Values.Should().HaveCount(1);
        allocationProfileType.Values[0].Should().Be(4321);
    }
}
