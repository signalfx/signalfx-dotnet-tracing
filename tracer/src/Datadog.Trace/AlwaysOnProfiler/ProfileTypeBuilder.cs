using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

internal abstract class ProfileTypeBuilder<TThreadSample> : IProfileTypeBuilder
    where TThreadSample : ThreadSample
{
    protected abstract long CalculateSampleValue(TThreadSample threadSample);

    protected abstract void SetProfileTypeInformation(ProfileLookupTables profileLookupTables, ProfileType profileType);

    protected abstract List<TThreadSample> RetrieveSamples();

    public ProfileTypeBuildResult Build(ProfileLookupTables profileLookupTables)
    {
        var threadSamples = RetrieveSamples();
        if (threadSamples == null || threadSamples.Count < 1)
        {
            return new ProfileTypeBuildResult();
        }

        var startTimestamp = threadSamples[0].Timestamp;
        var endTimestamp = threadSamples[0].Timestamp;

        // Arrays to create ProfileType after all samples are processed.
        var samplesCount = threadSamples.Count;
        var stackTracesIndices = new uint[samplesCount];
        var linkIndices = new uint[samplesCount];
        var attributeSetIndices = new uint[samplesCount];
        var sampleValues = new long[samplesCount];
        var timestamps = new ulong[samplesCount];

        for (int sampleIndex = 0; sampleIndex < samplesCount; sampleIndex++)
        {
            var threadSample = threadSamples[sampleIndex];

            // Check start and end times
            var sampleTimestamp = threadSample.Timestamp;
            if (startTimestamp.Nanoseconds > sampleTimestamp.Nanoseconds)
            {
                startTimestamp = sampleTimestamp;
            }
            else if (endTimestamp.Nanoseconds < sampleTimestamp.Nanoseconds)
            {
                endTimestamp = sampleTimestamp;
            }

            // Process thread information
            var attributes = new[]
            {
                new KeyValue
                {
                    Key = "thread.name",
                    Value = new AnyValue
                    {
                        IntValue = profileLookupTables.GetStringIndex(threadSample.ThreadName ??
                                                                      string.Empty)
                    }
                },
                new KeyValue { Key = "thread.id", Value = new AnyValue { IntValue = threadSample.ManagedId } }
            };
            var attributeSetIndex = profileLookupTables.GetAttributeSetIndex(attributes);

            // Process trace context
            var linkIndex = profileLookupTables.GetLinkIndex(threadSample);

            // Process the stack trace itself
            var stackTraceIndex = profileLookupTables.GetStacktraceIndex(threadSample.Frames);

            // Update profile type arrays for current thread sample
            stackTracesIndices[sampleIndex] = stackTraceIndex;
            linkIndices[sampleIndex] = linkIndex;
            attributeSetIndices[sampleIndex] = attributeSetIndex;
            sampleValues[sampleIndex] = CalculateSampleValue(threadSample);
            timestamps[sampleIndex] = sampleTimestamp.Nanoseconds;
        }

        var profileType = new ProfileType
        {
            StacktraceIndices = stackTracesIndices,
            LinkIndices = linkIndices,
            AttributeSetIndices = attributeSetIndices,
            Values = sampleValues,
            Timestamps = timestamps
        };

        SetProfileTypeInformation(profileLookupTables, profileType);

        return new ProfileTypeBuildResult
        {
            ContainsData = true,
            StartTimestamp = startTimestamp,
            EndTimestamp = endTimestamp,
            ProfileType = profileType
        };
    }
}
