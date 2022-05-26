// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.IO;
using Datadog.Trace.Configuration;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.Pprof.Proto.Profile.V1;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class PprofThreadSampleExporter : ThreadSampleExporter
    {
        public PprofThreadSampleExporter(ImmutableTracerSettings tracerSettings)
            : base(tracerSettings)
        {
        }

        public override void ExportThreadSamples(List<ThreadSample> threadSamples)
        {
            var profileBuilder = new ProfileBuilder();

            foreach (var threadSample in threadSamples)
            {
                var sample = new Sample();

                if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
                {
                    sample.Labels.Add(LabelBuilder.BuildSpanIdLabel(profileBuilder, threadSample.SpanId));
                    sample.Labels.Add(LabelBuilder.BuildTraceIdLabel(profileBuilder, threadSample.TraceIdHigh, threadSample.TraceIdLow));
                }

                profileBuilder.AddSample(sample);
            }

            var data = Serialize(profileBuilder.Build());
        }

        private static string Serialize(Profile profile)
        {
            using var memoryStream = new MemoryStream();
            Serializer.Serialize(memoryStream, profile);
            var byteArray = memoryStream.ToArray();

            return Convert.ToBase64String(byteArray);
        }

        private void Export(string body)
        {
            // TODO implement
        }
    }
}
