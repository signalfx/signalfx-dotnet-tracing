// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Datadog.Trace.Logging;
using Datadog.Trace.Vendors.Serilog.Events;

namespace Datadog.Trace.ThreadSampling
{
    /// <summary>
    /// Parser the native code's pause-time-optimized format.
    /// </summary>
    internal class ThreadSampleNativeFormatParser
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<ThreadSampleNativeFormatParser>();

        private readonly byte[] buf;
        private readonly int len;
        private readonly Dictionary<int, string> codes;
        private int pos;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSampleNativeFormatParser"/> class.
        /// </summary>
        /// <param name="buf">byte array containing native thread samples format data</param>
        /// <param name="len">how much of the buffer is actually used</param>
        internal ThreadSampleNativeFormatParser(byte[] buf, int len)
        {
            this.buf = buf;
            this.len = len;
            this.pos = 0;
            codes = new Dictionary<int, string>();
        }

        /// <summary>
        /// Parses the thread sample batch.
        /// </summary>
        internal List<ThreadSample> Parse()
        {
            uint batchThreadIndex = 0;
            var samples = new List<ThreadSample>();
            ulong batchTimestampNanos = 0;

            // FIXME actually have this go somewhere in the future
            while (pos < len)
            {
                byte op = buf[pos];
                pos++;
                if (op == OpCodes.StartBatch)
                {
                    int version = ReadInt();
                    if (version != 1)
                    {
                        return null; // not able to parse
                    }

                    long sampleStartMillis = ReadInt64();
                    batchTimestampNanos = (ulong)sampleStartMillis * 1_000_000u;

                    if (Log.IsEnabled(LogEventLevel.Debug))
                    {
                        var sampleStart = new DateTime(
                            (sampleStartMillis * TimeSpan.TicksPerMillisecond) + TimeConstants.UnixEpochInTicks).ToLocalTime();
                        Log.Debug(
                            "Parsing thread samples captured at {date} {time}",
                            sampleStart.ToLongDateString(),
                            sampleStart.ToLongTimeString());
                    }
                }
                else if (op == OpCodes.StartSample)
                {
                    int managedId = ReadInt();
                    int nativeId = ReadInt();
                    string threadName = ReadString();
                    long traceIdHigh = ReadInt64();
                    long traceIdLow = ReadInt64();
                    long spanId = ReadInt64();

                    var threadIndex = batchThreadIndex++;

                    var code = ReadShort();
                    if (code == 0)
                    {
                        // Empty stack, skip this sample.
                        continue;
                    }

                    var threadSample = new ThreadSample
                    {
                        Timestamp = batchTimestampNanos,
                        TraceIdHigh = traceIdHigh,
                        TraceIdLow = traceIdLow,
                        SpanId = spanId,
                    };

                    // The stack follows the experimental GDI conventions described at
                    // https://github.com/signalfx/gdi-specification/blob/29cbcbc969531d50ccfd0b6a4198bb8a89cedebb/specification/semantic_conventions.md#logrecord-message-fields
                    var stackTraceBuilder = new StringBuilder();
                    stackTraceBuilder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "\"{0}\" #{1} prio=0 os_prio=0 cpu=0 elapsed=0 tid=0x{2:x} nid=0x{3:x}\n",
                        threadName,
                        threadIndex,
                        managedId,
                        nativeId);

                    // FIXME: here should go Thread state, equivalent of"    java.lang.Thread.State: TIMED_WAITING (sleeping)"
                    stackTraceBuilder.Append("\n");

                    while (code != 0)
                    {
                        string value = null;
                        if (code < 0)
                        {
                            value = ReadString();
                            codes[-code] = value;
                        }
                        else if (code > 0)
                        {
                            value = codes[code];
                        }

                        if (value != null)
                        {
                            stackTraceBuilder.Append("\tat ");
                            stackTraceBuilder.Append(value.Replace("::", "."));
                            stackTraceBuilder.Append("(unknown)\n"); // TODO placeholder for file name and lines numbers
                        }

                        code = ReadShort();
                    }

                    if (threadName == ThreadSampler.BackgroundThreadName)
                    {
                        // TODO: add configuration option to include the sampler thread. By default remove it.
                        continue;
                    }

                    threadSample.StackTrace = stackTraceBuilder.ToString();
                    samples.Add(threadSample);
                }
                else if (op == OpCodes.EndBatch)
                {
                    // end batch, nothing here
                }
                else if (op == OpCodes.BatchStats)
                {
                    int microsSuspended = ReadInt();
                    int numThreads = ReadInt();
                    int totalFrames = ReadInt();
                    int numCacheMisses = ReadInt();

                    if (Log.IsEnabled(LogEventLevel.Debug))
                    {
                        Log.Debug(
                        "CLR was suspended for {microsSuspended} microseconds to collect a thread sample batch: threads={numThreads} frames={totalFrames} misses={numCacheMisses}",
                        new object[] { microsSuspended, numThreads, totalFrames, numCacheMisses });
                    }
                }
                else
                {
                    pos = len + 1; // FIXME improve error handling here
                }
            }

            return samples;
        }

        private string ReadString()
        {
            short len = ReadShort();
            string s = new UnicodeEncoding().GetString(buf, pos, len * 2);
            pos += 2 * len;
            return s;
        }

        private short ReadShort()
        {
            short s1 = (short)(buf[pos] & 0xFF);
            s1 <<= 8;
            short s2 = (short)(buf[pos + 1] & 0xFF);
            pos += 2;
            return (short)(s1 + s2);
        }

        private int ReadInt()
        {
            int i1 = buf[pos] & 0xFF;
            i1 <<= 24;
            int i2 = buf[pos + 1] & 0xFF;
            i2 <<= 16;
            int i3 = buf[pos + 2] & 0xFF;
            i3 <<= 8;
            int i4 = buf[pos + 3] & 0xFF;
            pos += 4;
            return i1 + i2 + i3 + i4;
        }

        private long ReadInt64()
        {
            long l1 = buf[pos] & 0xFF;
            l1 <<= 56;
            long l2 = buf[pos + 1] & 0xFF;
            l2 <<= 48;
            long l3 = buf[pos + 2] & 0xFF;
            l3 <<= 40;
            long l4 = buf[pos + 3] & 0xFF;
            l4 <<= 32;
            long l5 = buf[pos + 4] & 0xFF;
            l5 <<= 24;
            long l6 = buf[pos + 5] & 0xFF;
            l6 <<= 16;
            long l7 = buf[pos + 6] & 0xFF;
            l7 <<= 8;
            long l8 = buf[pos + 7] & 0xFF;
            pos += 8;
            return l1 + l2 + l3 + l4 + l5 + l6 + l7 + l8;
        }

        private static class OpCodes
        {
            /// <summary>
            /// Marks the start of a batch of thread samples, see THREAD_SAMPLES_START_BATCH on native code.
            /// </summary>
            public const byte StartBatch = 0x01;

            /// <summary>
            /// Marks the start of a thread sample, see THREAD_SAMPLES_START_SAMPLE on native code.
            /// </summary>
            public const byte StartSample = 0x02;

            /// <summary>
            /// Marks the end of a batch of thread samples, see THREAD_SAMPLES_END_BATCH on native code.
            /// </summary>
            public const byte EndBatch = 0x06;

            /// <summary>
            /// Marks the begining of a section with statistics, see THREAD_SAMPLES_FINAL_STATS on native code.
            /// </summary>
            public const byte BatchStats = 0x07;
        }
    }
}
