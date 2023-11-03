using System;
using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers;
using Datadog.Trace.ClrProfiler;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

internal class AllocationProfileTypeBuilder : ProfileTypeBuilder<AllocationSample>
{
    private readonly byte[] _buffer;
    private readonly Func<List<AllocationSample>> _retrieveSamplesFunc;

    public AllocationProfileTypeBuilder(
        byte[] sharedUnparsedSamplesBuffer,
        Func<List<AllocationSample>> retrieveSamplesFunc = null) // For testing purposes.
    {
        _buffer = sharedUnparsedSamplesBuffer;
        _retrieveSamplesFunc = retrieveSamplesFunc ?? DefaultRetrieveSamples;
    }

    protected override long CalculateSampleValue(AllocationSample threadSample) => threadSample.AllocationSizeBytes;

    protected override void SetProfileTypeInformation(ProfileLookupTables profileLookupTables, ProfileType profileType)
    {
        profileType.SampleRate = 100_000;
        profileType.TypeIndex = profileLookupTables.GetStringIndex("allocation");
        profileType.UnitIndex = profileLookupTables.GetStringIndex("bytes");
    }

    protected override List<AllocationSample> RetrieveSamples() => _retrieveSamplesFunc();

    internal List<AllocationSample> DefaultRetrieveSamples()
    {
        // Managed-side calls to this method dictate buffer changeover, no catch up call needed
        var read = NativeMethods.SignalFxReadAllocationSamples(_buffer.Length, _buffer);
        if (read <= 0)
        {
            // No data just return.
            return null;
        }

        return SampleNativeFormatParser.ParseAllocationSamples(_buffer, read);
    }
}
