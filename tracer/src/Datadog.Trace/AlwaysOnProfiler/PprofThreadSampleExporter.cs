// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                pprof.AddLabel(sampleBuilder, "source.event.time", threadSample.Timestamp);

                if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
                {
                    pprof.AddLabel(sampleBuilder, "span_id", threadSample.SpanId);
                    pprof.AddLabel(sampleBuilder, "trace_id", $"{threadSample.TraceIdHigh:x16}{threadSample.TraceIdLow:x16}");
                }

                // if (stackTrace.isTruncated() || stackTrace.getFrames().size() > stackDepth)
                // {
                pprof.AddLabel(sampleBuilder, "thread.stack.truncated", true);
                // }

                var methodNames = threadSample.StackTrace
                                              .Split('\n')
                                              .Select(fullName => fullName.Trim())
                                              .Where(fullName => fullName.StartsWith("at"))
                                              .Select(fullName => fullName.Split(' ')[1]);

                foreach (var methodName in methodNames)
                {
                    sampleBuilder.AddLocationId(pprof.GetLocationId("unknown", methodName, 0));
                }

                var threadInfo = threadSample.StackTrace
                                             .Split('\n')[0]
                                             .Trim()
                                             .Split(' ')
                                             .Select(pair => pair.Split('='))
                                             .Where(keyValue => keyValue.Length == 2)
                                             .ToDictionary(keyValue => keyValue[0], keyValue => keyValue[1]);

                pprof.AddLabel(sampleBuilder, "thread.id", threadInfo["tid"]);
                pprof.AddLabel(sampleBuilder, "thread.os.id", threadInfo["nid"]);
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
                        }
                    },
                    Body = new AnyValue { StringValue = data },
                    TimeUnixNano = threadSample.Timestamp,
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
            Serializer.Serialize(memoryStream, profile);
            var byteArray = memoryStream.ToArray();

            return Convert.ToBase64String(byteArray);
        }
    }
}
