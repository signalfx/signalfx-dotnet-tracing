using System;
using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers;
using Datadog.Trace.ClrProfiler;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

internal sealed class CpuProfileTypeBuilder : ProfileTypeBuilder<ThreadSample>
{
    private readonly TimeSpan _samplingPeriod;
    private readonly byte[] _buffer;
    private readonly Func<List<ThreadSample>> _retrieveSamplesFunc;

    public CpuProfileTypeBuilder(
        TimeSpan samplingPeriod,
        byte[] sharedUnparsedSamplesBuffer,
        Func<List<ThreadSample>> retrieveSamplesFunc = null) // For testing purposes.
    {
        _samplingPeriod = samplingPeriod;
        _buffer = sharedUnparsedSamplesBuffer;
        _retrieveSamplesFunc = retrieveSamplesFunc ?? DefaultRetrieveSamples;
    }

    protected override long CalculateSampleValue(ThreadSample threadSample) => 0;

    protected override void SetProfileTypeInformation(ProfileLookupTables profileLookupTables, ProfileType profileType)
    {
        profileType.SampleRate = (ulong)_samplingPeriod.TotalMilliseconds;
        profileType.TypeIndex = profileLookupTables.GetStringIndex("cpu");
        profileType.UnitIndex = profileLookupTables.GetStringIndex("ms");
    }

    protected override List<ThreadSample> RetrieveSamples() => _retrieveSamplesFunc();

    private List<ThreadSample> DefaultRetrieveSamples()
    {
        var read = NativeMethods.SignalFxReadThreadSamples(_buffer.Length, _buffer);
        if (read <= 0)
        {
            // No data just return.
            return null;
        }

        return SampleNativeFormatParser.ParseThreadSamples(_buffer, read);
    }
}
