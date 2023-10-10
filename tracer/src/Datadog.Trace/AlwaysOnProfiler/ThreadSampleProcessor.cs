// Modified by Splunk Inc.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers;
using Datadog.Trace.Configuration;
using Datadog.Trace.Vendors.Newtonsoft.Json.Utilities;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;
using OtelProfile = Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1.Profile;
using PprofProfile = Datadog.Tracer.Pprof.Proto.Profile.Profile;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class ThreadSampleProcessor
    {
        private readonly TimeSpan _threadSamplingPeriod;

        public ThreadSampleProcessor(ImmutableTracerSettings tracerSettings)
        {
            _threadSamplingPeriod = tracerSettings.ThreadSamplingPeriod;
        }

        public OtelProfile ProcessThreadSamples(List<ThreadSample> threadSamples)
        {
            if (threadSamples == null || threadSamples.Count < 1)
            {
                return null;
            }

            var profileLookupTables = new ProfileLookupTables();

            var startTimeUnixNano = threadSamples[0].Timestamp.Nanoseconds;
            var endTimeUnixNano = threadSamples[0].Timestamp.Nanoseconds;

            // Arrays to create ProfileType after all samples are processed.
            var samplesCount = threadSamples.Count;
            var stackTracesIndices = new uint[samplesCount];
            var linkIndices = new uint[samplesCount];
            var attributeSetIndices = new uint[samplesCount];
            var timestamps = new ulong[samplesCount];

            for (int sampleIndex = 0; sampleIndex < samplesCount; sampleIndex++)
            {
                var threadSample = threadSamples[sampleIndex];

                // Check start and end times
                var sampleTimeUnixNano = threadSample.Timestamp.Nanoseconds;
                if (startTimeUnixNano > sampleTimeUnixNano)
                {
                    startTimeUnixNano = sampleTimeUnixNano;
                }
                else if (endTimeUnixNano < sampleTimeUnixNano)
                {
                    endTimeUnixNano = sampleTimeUnixNano;
                }

                // Process thread information
                var attributes = new[]
                {
                    new KeyValue { Key = "thread.name", Value = new AnyValue { IntValue = profileLookupTables.GetStringIndex(threadSample.ThreadName ?? string.Empty) } },
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
                timestamps[sampleIndex] = sampleTimeUnixNano;
            }

            var cpuProfileType = new ProfileType
            {
                SampleRate = (ulong)_threadSamplingPeriod.TotalMilliseconds,
                TypeIndex = profileLookupTables.GetStringIndex("cpu"),
                UnitIndex = profileLookupTables.GetStringIndex("ms"),
                StacktraceIndices = stackTracesIndices,
                LinkIndices = linkIndices,
                AttributeSetIndices = attributeSetIndices,
                Timestamps = timestamps
            };

            var profile = new OtelProfile
            {
                ProfileId = Guid.Empty.ToByteArray(),
                StartTimeUnixNano = startTimeUnixNano,
                EndTimeUnixNano = endTimeUnixNano
            };

            // Add profile types
            profile.ProfileTypes.Add(cpuProfileType);

            // Add lookup tables
            profileLookupTables.CopyLookupTablesToProfile(profile);

            return profile;
        }

        public OtelProfile ProcessAllocationSamples(List<AllocationSample> allocationSamples)
        {
            if (allocationSamples == null || allocationSamples.Count < 1)
            {
                return null;
            }

            // var allocationProfile = BuildPProfForAllocationProfile(allocationSamples);
            // var totalFrameCount = CountFrames(allocationSamples);

            // OTLP_PROFILES: TODO: Add actual data.
            return new OtelProfile();
        }

        private static int CountFrames(List<AllocationSample> samples)
        {
            var sum = 0;
            for (var i = 0; i < samples.Count; i++)
            {
                sum += samples[i].ThreadSample.Frames.Count;
            }

            return sum;
        }

        private static int CountFrames(List<ThreadSample> samples)
        {
            var sum = 0;
            for (var i = 0; i < samples.Count; i++)
            {
                sum += samples[i].Frames.Count;
            }

            return sum;
        }

        private static SampleBuilder CreatePprofProfileBuilder(Pprof pprof, ThreadSample threadSample)
        {
            var sampleBuilder = new SampleBuilder();

            pprof.AddLabel(sampleBuilder, "source.event.time", threadSample.Timestamp.Milliseconds);

            if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
            {
                pprof.AddLabel(sampleBuilder, "span_id", threadSample.SpanId.ToString("x16"));
                pprof.AddLabel(sampleBuilder, "trace_id", TraceIdHelper.ToString(threadSample.TraceIdHigh, threadSample.TraceIdLow));
            }

            for (var index = 0; index < threadSample.Frames.Count; index++)
            {
                var methodName = threadSample.Frames[index];
                sampleBuilder.AddLocationId(pprof.GetLocationId(methodName));
            }

            pprof.AddLabel(sampleBuilder, "thread.id", threadSample.ManagedId);
            pprof.AddLabel(sampleBuilder, "thread.name", threadSample.ThreadName);
            return sampleBuilder;
        }

        private static PprofProfile BuildPProfForAllocationProfile(List<AllocationSample> allocationSamples)
        {
            var pprof = new Pprof();
            for (var index = 0; index < allocationSamples.Count; index++)
            {
                var allocationSample = allocationSamples[index];
                var profileBuilder = CreatePprofProfileBuilder(pprof, allocationSample.ThreadSample);

                profileBuilder.SetValue(allocationSample.AllocationSizeBytes);
                pprof.Profile.Samples.Add(profileBuilder.Build());
            }

            return pprof.Profile;
        }

        private static OtelProfile BuildOtelProfileForAllocationProfile(List<AllocationSample> allocationSamples)
        {
            // var startTimeUnixNano = ulong.MaxValue;
            // var endTimeUnixNano = ulong.MinValue;
            var profile = new OtelProfile();

            /*
            for (var index = 0; index < allocationSamples.Count; index++)
            {
                var allocationSample = allocationSamples[index];
                var threadSample = allocationSample.ThreadSample;
                if (threadSample.Timestamp.Nanoseconds < startTimeUnixNano)
                {
                    startTimeUnixNano = threadSample.Timestamp.Nanoseconds;
                }

                if (threadSample.Timestamp.Nanoseconds > endTimeUnixNano)
                {
                    endTimeUnixNano = threadSample.Timestamp.Nanoseconds;
                }

                var stackTrace = new Stacktrace();
                if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
                {
                    // OTLP_PROFILES: TODO: Add trace context information
                    // pprof.AddLabel(sampleBuilder, "span_id", threadSample.SpanId.ToString("x16"));
                    // pprof.AddLabel(sampleBuilder, "trace_id", TraceIdHelper.ToString(threadSample.TraceIdHigh, threadSample.TraceIdLow));
                }

                for (var index = 0; index < threadSample.Frames.Count; index++)
                {
                    var methodName = threadSample.Frames[index];
                    sampleBuilder.AddLocationId(pprof.GetLocationId(methodName));
                }

                pprof.AddLabel(sampleBuilder, "thread.id", threadSample.ManagedId);
                pprof.AddLabel(sampleBuilder, "thread.name", threadSample.ThreadName);
                return sampleBuilder;

                profileBuilder.SetValue(allocationSample.AllocationSizeBytes);
                pprof.Profile.Samples.Add(profileBuilder.Build());
            }
            */

            return profile;
        }

        private PprofProfile BuildPprofCpuProfile(List<ThreadSample> threadSamples)
        {
            var pprof = new Pprof();
            for (var index = 0; index < threadSamples.Count; index++)
            {
                var threadSample = threadSamples[index];
                var sampleBuilder = CreatePprofProfileBuilder(pprof, threadSample);

                pprof.AddLabel(sampleBuilder, "source.event.period", (long)_threadSamplingPeriod.TotalMilliseconds);
                pprof.Profile.Samples.Add(sampleBuilder.Build());
            }

            return pprof.Profile;
        }
    }
}
