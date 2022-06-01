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

                // TODO get allocationSize
                // sampleBuilder.AddValue(allocationSize);

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

                // TODO Add StackTrace data to the Profile object based on java implementation below

                // String eventName = event.getEventType().getName();
                // pprof.addLabel(sample, SOURCE_EVENT_NAME, eventName);
                // Instant time = event.getStartTime();
                // pprof.addLabel(sample, SOURCE_EVENT_TIME, time.toEpochMilli());
                //
                // RecordedThread thread = event.getThread();
                // if (thread != null) {
                //     if (thread.getJavaThreadId() != -1)
                //     {
                //         pprof.addLabel(sample, THREAD_ID, thread.getJavaThreadId());
                //         pprof.addLabel(sample, THREAD_NAME, thread.getJavaName());
                //     }
                //     pprof.addLabel(sample, THREAD_OS_ID, thread.getOSThreadId());
                // }
                // pprof.addLabel(sample, THREAD_STATE, "RUNNABLE");
                //
                // if (spanContext != null && spanContext.isValid()) {
                //     pprof.addLabel(sample, TRACE_ID, spanContext.getTraceId());
                //     pprof.addLabel(sample, SPAN_ID, spanContext.getSpanId());
                // }
                // if (sampler != null) {
                //     sampler.addAttributes(
                //         (k, v)->pprof.addLabel(sample, k, v), (k, v)->pprof.addLabel(sample, k, v));
                // }
                //
                // pprof.getProfileBuilder().addSample(sample);
                // }
                //
                // private static Pprof createPprof()
                // {
                //     Pprof pprof = new Pprof();
                //     Profile.Builder profile = pprof.getProfileBuilder();
                //     profile.addSampleType(
                //         ProfileProto.ValueType.newBuilder()
                //             .setType(pprof.getStringId("allocationSize"))
                //             .setUnit(pprof.getStringId("bytes"))
                //             .build());

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
