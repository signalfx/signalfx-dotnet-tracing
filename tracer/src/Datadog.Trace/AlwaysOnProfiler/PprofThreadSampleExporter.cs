// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Datadog.Trace.AlwaysOnProfiler.Builder;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;
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
            var logRecords = LogsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;

            foreach (var threadSample in threadSamples)
            {
                var pprof = new Pprof();
                var sampleBuilder = new SampleBuilder();

                pprof.AddLabel(sampleBuilder, "source.event.time", threadSample.Timestamp.Milliseconds);

                if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
                {
                    pprof.AddLabel(sampleBuilder, "span_id", threadSample.SpanId);
                    pprof.AddLabel(sampleBuilder, "trace_id", $"{threadSample.TraceIdHigh:x16}{threadSample.TraceIdLow:x16}");
                }

                foreach (var methodName in threadSample.Frames)
                {
                    sampleBuilder.AddLocationId(pprof.GetLocationId("unknown", methodName, 0));
                }

                pprof.AddLabel(sampleBuilder, "thread.id", threadSample.ManagedId);
                pprof.AddLabel(sampleBuilder, "thread.name", threadSample.ThreadName);
                pprof.AddLabel(sampleBuilder, "thread.os.id", threadSample.NativeId);
                pprof.AddLabel(sampleBuilder, "thread.state", "RUNNABLE");

                pprof.ProfileBuilder.AddSample(sampleBuilder.Build());
                var data = Serialize(pprof.ProfileBuilder.Build());
                var logRecord = new LogRecord
                {
                    Attributes =
                    {
                        FixedLogRecordAttributes[0],
                        FixedLogRecordAttributes[1],
                        new KeyValue
                        {
                            Key = "profiling.data.format",
                            Value = new AnyValue { StringValue = "pprof-gzip-base64" }
                        },
                        new KeyValue
                        {
                            Key = "profiling.data.type",
                            Value = new AnyValue { StringValue = "cpu" }
                        }
                    },
                    Body = new AnyValue { StringValue = data },
                    TimeUnixNano = threadSample.Timestamp.Nanoseconds,
                };

                if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
                {
                    // TODO Splunk: Add tests and validate.
                    logRecord.SpanId = BitConverter.GetBytes(threadSample.SpanId);
                    logRecord.TraceId = BitConverter.GetBytes(threadSample.TraceIdHigh).Concat(BitConverter.GetBytes(threadSample.TraceIdLow));
                }

                logRecords.Add(logRecord);
            }

            SendLogsData();
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
    }
}