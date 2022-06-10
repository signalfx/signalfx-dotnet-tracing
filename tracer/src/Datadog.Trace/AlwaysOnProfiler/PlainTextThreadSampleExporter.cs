// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;

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
                var logRecord = CreateLogRecord(GetPlainTextStackTrace(threadSample), threadSample.Timestamp.Nanoseconds);

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

        private static string GetPlainTextStackTrace(ThreadSample threadSample)
        {
            // The stack follows the experimental GDI conventions described at
            // https://github.com/signalfx/gdi-specification/blob/29cbcbc969531d50ccfd0b6a4198bb8a89cedebb/specification/semantic_conventions.md#logrecord-message-fields

            var stackTraceBuilder = new StringBuilder();

            stackTraceBuilder.AppendFormat(
                CultureInfo.InvariantCulture,
                "\"{0}\" #{1} prio=0 os_prio=0 cpu=0 elapsed=0 tid=0x{2:x} nid=0x{3:x}\n",
                threadSample.Timestamp,
                threadSample.ThreadIndex,
                threadSample.ManagedId,
                threadSample.NativeId);

            // TODO Splunk: APMI-2565 here should go Thread state, equivalent of"    java.lang.Thread.State: TIMED_WAITING (sleeping)"
            stackTraceBuilder.Append("\n");

            foreach (var frame in threadSample.Frames)
            {
                stackTraceBuilder.Append("\tat ");
                stackTraceBuilder.Append(frame);
                stackTraceBuilder.Append('\n');
            }

            return stackTraceBuilder.ToString();
        }
    }
}
