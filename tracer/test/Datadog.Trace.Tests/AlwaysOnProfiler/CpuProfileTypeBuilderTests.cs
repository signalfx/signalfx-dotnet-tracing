using System;
using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers;
using Datadog.Trace.Vendors.Newtonsoft.Json.Utilities;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

public class CpuProfileTypeBuilderTests
{
    [Fact]
    public void ValidateProfileTypeInformation()
    {
        var samplingPeriod = TimeSpan.FromSeconds(10);
        var sharedBuffer = Array.Empty<byte>();
        var threadSample = new ThreadSample { ThreadName = "test.thread", ManagedId = 42, Timestamp = new ThreadSample.Time(1234) };
        threadSample.Frames.AddRange(new List<string> { "test_function_0", "test_function_1" });

        var cpuProfileTypeBuilder = new CpuProfileTypeBuilder(
            samplingPeriod,
            sharedBuffer,
            () => new List<ThreadSample> { threadSample });

        var profileTypeBuilder = new ProfileBuilder(new List<IProfileTypeBuilder> { cpuProfileTypeBuilder });
        var profile = profileTypeBuilder.Build();

        profile.ProfileTypes.Should().HaveCount(1);
        profile.StartTimeUnixNano.Should().Be(threadSample.Timestamp.Nanoseconds);
        profile.EndTimeUnixNano.Should().Be(threadSample.Timestamp.Nanoseconds);
        profile.Functions.Should().HaveCount(2);
        profile.Stacktraces.Should().HaveCount(1);

        var cpuProfileType = profile.ProfileTypes[0];
        profile.StringTables[(int)cpuProfileType.TypeIndex].Should().Be("cpu");
        profile.StringTables[(int)cpuProfileType.UnitIndex].Should().Be("ms");
        cpuProfileType.SampleRate.Should().Be(10_000);
        cpuProfileType.Timestamps.Should().HaveCount(1);
        cpuProfileType.Timestamps[0].Should().Be(threadSample.Timestamp.Nanoseconds);
        cpuProfileType.Values.Should().HaveCount(1);
        cpuProfileType.Values[0].Should().Be(0);
    }
}
