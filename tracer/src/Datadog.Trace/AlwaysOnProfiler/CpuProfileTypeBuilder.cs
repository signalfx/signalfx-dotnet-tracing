using System;
using Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

internal sealed class CpuProfileTypeBuilder : ProfileTypeBuilder<ThreadSample>
{
    private readonly TimeSpan _samplingPeriod;

    public CpuProfileTypeBuilder(TimeSpan samplingPeriod)
    {
        _samplingPeriod = samplingPeriod;
    }

    protected override long CalculateSampleValue(ThreadSample threadSample) => 0;

    protected override void SetProfileTypeInformation(ProfileLookupTables profileLookupTables, ProfileType profileType)
    {
        profileType.SampleRate = (ulong)_samplingPeriod.TotalMilliseconds;
        profileType.TypeIndex = profileLookupTables.GetStringIndex("cpu");
        profileType.UnitIndex = profileLookupTables.GetStringIndex("ms");
    }
}
