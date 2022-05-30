// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Net;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Propagation;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class PlainTextThreadSampleExporter : ThreadSampleExporter
    {
        internal PlainTextThreadSampleExporter(ImmutableTracerSettings tracerSettings)
        : base(tracerSettings)
        {
        }

        public override void ExportThreadSamples(List<ThreadSample> threadSamples)
        {
            if (threadSamples == null || threadSamples.Count < 1)
            {
                return;
            }

            // The same _logsData instance is used on all export messages. With the exception of the list of
            // LogRecords, the Logs property, all other fields are prepopulated. At this point the code just`
            // need to create a LogRecord for each thread sample and add it to the Logs list.
            var logRecords = LogsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;

            for (var i = 0; i < threadSamples.Count; i++)
            {
                var threadSample = threadSamples[i];
                var logRecord = new LogRecord
                {
                    Attributes =
                    {
                        FixedLogRecordAttributes[0],
                        FixedLogRecordAttributes[1],
                        new KeyValue
                        {
                            Key = "profiling.data.format",
                            Value = new AnyValue { StringValue = "text" }
                        }
                    },
                    Body = new AnyValue { StringValue = threadSample.StackTrace },
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
    }
}
