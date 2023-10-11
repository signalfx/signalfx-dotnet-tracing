using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers;
using Datadog.Trace.ClrProfiler;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

internal class AllocationProfileTypeBuilder : ProfileTypeBuilder<AllocationSample>
{
    private readonly byte[] _buffer;

    public AllocationProfileTypeBuilder(byte[] sharedUnparsedSamplesBuffer)
    {
        _buffer = sharedUnparsedSamplesBuffer;
    }

    protected override long CalculateSampleValue(AllocationSample threadSample) => threadSample.AllocationSizeBytes;

    protected override void SetProfileTypeInformation(ProfileLookupTables profileLookupTables, ProfileType profileType)
    {
        profileType.SampleRate = 100_000; // OTLP_PROFILES: TODO: Get this value from config and validate it.
        profileType.TypeIndex = profileLookupTables.GetStringIndex("allocation");
        profileType.UnitIndex = profileLookupTables.GetStringIndex("bytes");
    }

    protected override List<AllocationSample> RetrieveSamples()
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
