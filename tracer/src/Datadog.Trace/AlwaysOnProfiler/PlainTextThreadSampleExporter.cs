// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Datadog.Trace.Configuration;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class PlainTextThreadSampleExporter : ThreadSampleExporter
    {
        private readonly KeyValue _samplingPeriodAttribute;

        internal PlainTextThreadSampleExporter(ImmutableTracerSettings tracerSettings, ILogSender logSender)
        : base(tracerSettings, logSender, "text")
        {
            _samplingPeriodAttribute = GdiProfilingConventions.LogRecord.Attributes.Period((long)tracerSettings.ThreadSamplingPeriod.TotalMilliseconds);
        }

        protected override void ProcessThreadSamples(List<ThreadSample> samples)
        {
            foreach (var threadSample in samples)
            {
                var logRecord = AddLogRecord(BuildStackTrace(threadSample));
                DecorateLogRecord(logRecord, threadSample);
            }
        }

        private static byte[] ToBigEndianBytes(long high, long low)
        {
            var highBytes = ToBigEndianBytes(high);
            var lowBytes = ToBigEndianBytes(low);

            var finalBytes = new byte[16];

            highBytes.CopyTo(finalBytes, 0);
            lowBytes.CopyTo(finalBytes, 8);

            return finalBytes;
        }

        private static byte[] ToBigEndianBytes(long val)
        {
            var bytes = BitConverter.GetBytes(val);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        private static string BuildStackTrace(ThreadSample threadSample)
        {
            var stackTraceBuilder = new StringBuilder();

            stackTraceBuilder.AppendFormat(
                CultureInfo.InvariantCulture,
                "\"{0}\" #{1} prio=0 os_prio=0 cpu=0 elapsed=0 tid=0x{2:x} nid=0x{3:x}\n",
                threadSample.ThreadName,
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

        private void DecorateLogRecord(LogRecord logRecord, ThreadSample threadSample)
        {
            if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
            {
                logRecord.SpanId = ToBigEndianBytes(threadSample.SpanId);
                logRecord.TraceId = ToBigEndianBytes(threadSample.TraceIdHigh, threadSample.TraceIdLow);
            }

            logRecord.TimeUnixNano = threadSample.Timestamp.Nanoseconds;
            logRecord.Attributes.Add(_samplingPeriodAttribute);
        }
    }
}
