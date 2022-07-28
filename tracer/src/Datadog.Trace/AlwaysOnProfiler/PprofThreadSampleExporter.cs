// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Datadog.Trace.AlwaysOnProfiler.Builder;
using Datadog.Trace.Configuration;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.Pprof.Proto.Profile;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class PprofThreadSampleExporter : ThreadSampleExporter
    {
        private readonly TimeSpan _threadSamplingPeriod;

        public PprofThreadSampleExporter(ImmutableTracerSettings tracerSettings, ILogSender logSender)
            : base(tracerSettings, logSender, "pprof-gzip-base64")
        {
            _threadSamplingPeriod = tracerSettings.ThreadSamplingPeriod;
        }

        protected override void ProcessThreadSamples(List<ThreadSample> samples)
        {
            var profile = BuildProfile(samples);
            // all of the samples in the batch have the same timestamp, pick from the first sample
            AddLogRecord(samples[0].Timestamp.Nanoseconds, Serialize(profile));
        }

        private static string Serialize(Profile profile)
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

        private Profile BuildProfile(List<ThreadSample> threadSamples)
        {
            var pprof = new Pprof();
            foreach (var threadSample in threadSamples)
            {
                var sampleBuilder = new SampleBuilder();

                pprof.AddLabel(sampleBuilder, "source.event.time", threadSample.Timestamp.Milliseconds);
                pprof.AddLabel(sampleBuilder, "source.event.period", (long)_threadSamplingPeriod.TotalMilliseconds);

                if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
                {
                    pprof.AddLabel(sampleBuilder, "span_id", threadSample.SpanId.ToString("x16"));
                    pprof.AddLabel(sampleBuilder, "trace_id", TraceIdHelper.ToString(threadSample.TraceIdHigh, threadSample.TraceIdLow));
                }

                foreach (var methodName in threadSample.Frames)
                {
                    sampleBuilder.AddLocationId(pprof.GetLocationId(methodName));
                }

                pprof.AddLabel(sampleBuilder, "thread.id", threadSample.ManagedId);
                pprof.AddLabel(sampleBuilder, "thread.name", threadSample.ThreadName);
                pprof.AddLabel(sampleBuilder, "thread.os.id", threadSample.NativeId);

                pprof.Profile.Samples.Add(sampleBuilder.Build());
            }

            return pprof.Profile;
        }
    }
}
