// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Datadog.Trace.Configuration;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;
using Datadog.Tracer.Pprof.Proto.Profile;
using Profile = Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1.Profile;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class ThreadSampleProcessor
    {
        private readonly TimeSpan _threadSamplingPeriod;

        public ThreadSampleProcessor(ImmutableTracerSettings tracerSettings)
        {
            _threadSamplingPeriod = tracerSettings.ThreadSamplingPeriod;
        }

        public Profile ProcessThreadSamples(List<ThreadSample> threadSamples)
        {
            if (threadSamples == null || threadSamples.Count < 1)
            {
                return null;
            }

            // var cpuProfile = BuildPprofCpuProfile(threadSamples);
            // var totalFrameCount = CountFrames(threadSamples);

            // OTLP_PROFILES: TODO: Add actual data.
            return new Profile();
        }

        public Profile ProcessAllocationSamples(List<AllocationSample> allocationSamples)
        {
            if (allocationSamples == null || allocationSamples.Count < 1)
            {
                return null;
            }

            // var allocationProfile = BuildAllocationProfile(allocationSamples);
            // var totalFrameCount = CountFrames(allocationSamples);

            // OTLP_PROFILES: TODO: Add actual data.
            return new Profile();
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

        private static string Serialize(Datadog.Tracer.Pprof.Proto.Profile.Profile profile)
        {
            using var memoryStream = new MemoryStream();
            using (var compressionStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                Serializer.Serialize(compressionStream, profile);
                compressionStream.Flush();
            }

            var byteArray = memoryStream.ToArray();
            return Convert.ToBase64String(byteArray);
        }

        private static SampleBuilder CreateSampleBuilder(Pprof pprof, ThreadSample threadSample)
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

        private static string BuildAllocationProfile(List<AllocationSample> allocationSamples)
        {
            var pprof = new Pprof();
            for (var index = 0; index < allocationSamples.Count; index++)
            {
                var allocationSample = allocationSamples[index];
                var sampleBuilder = CreateSampleBuilder(pprof, allocationSample.ThreadSample);

                // TODO Splunk: export typename
                sampleBuilder.SetValue(allocationSample.AllocationSizeBytes);
                pprof.Profile.Samples.Add(sampleBuilder.Build());
            }

            return Serialize(pprof.Profile);
        }

        private string BuildPprofCpuProfile(List<ThreadSample> threadSamples)
        {
            var pprof = new Pprof();
            for (var index = 0; index < threadSamples.Count; index++)
            {
                var threadSample = threadSamples[index];
                var sampleBuilder = CreateSampleBuilder(pprof, threadSample);

                pprof.AddLabel(sampleBuilder, "source.event.period", (long)_threadSamplingPeriod.TotalMilliseconds);
                pprof.Profile.Samples.Add(sampleBuilder.Build());
            }

            return Serialize(pprof.Profile);
        }
    }
}
