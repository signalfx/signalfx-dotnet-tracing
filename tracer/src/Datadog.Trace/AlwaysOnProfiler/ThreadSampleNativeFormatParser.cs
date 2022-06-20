// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Text;
using Datadog.Trace.Logging;
using Datadog.Trace.Vendors.Serilog.Events;

namespace Datadog.Trace.AlwaysOnProfiler
{
    /// <summary>
    /// Parser the native code's pause-time-optimized format.
    /// </summary>
    internal class ThreadSampleNativeFormatParser
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<ThreadSampleNativeFormatParser>();
        private static readonly bool IsLogLevelDebugEnabled = Log.IsEnabled(LogEventLevel.Debug);

        private readonly byte[] _buffer;
        private readonly int _length;
        private readonly Dictionary<int, string> _codes;
        private int _position;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSampleNativeFormatParser"/> class.
        /// </summary>
        /// <param name="buffer">byte array containing native thread samples format data</param>
        /// <param name="length">how much of the buffer is actually used</param>
        internal ThreadSampleNativeFormatParser(byte[] buffer, int length)
        {
            _buffer = buffer;
            _length = length;
            _position = 0;
            _codes = new Dictionary<int, string>();
        }

        /// <summary>
        /// Parses the thread sample batch.
        /// </summary>
        internal List<ThreadSample> Parse()
        {
            uint batchThreadIndex = 0;
            var samples = new List<ThreadSample>();
            long sampleStartMillis = 0;

            while (_position < _length)
            {
                var operationCode = _buffer[_position];
                _position++;
                if (operationCode == OpCodes.StartBatch)
                {
                    var version = ReadInt();
                    if (version != 1)
                    {
                        return null; // not able to parse
                    }

                    sampleStartMillis = ReadInt64();

                    if (IsLogLevelDebugEnabled)
                    {
                        var sampleStart = new DateTime(
                            (sampleStartMillis * TimeSpan.TicksPerMillisecond) + TimeConstants.UnixEpochInTicks).ToLocalTime();
                        Log.Debug(
                            "Parsing thread samples captured at {date} {time}",
                            sampleStart.ToLongDateString(),
                            sampleStart.ToLongTimeString());
                    }
                }
                else if (operationCode == OpCodes.StartSample)
                {
                    var managedId = ReadInt();
                    var nativeId = ReadInt();
                    var threadName = ReadString();
                    var traceIdHigh = ReadInt64();
                    var traceIdLow = ReadInt64();
                    var spanId = ReadInt64();

                    var threadIndex = batchThreadIndex++;

                    var code = ReadShort();
                    if (code == 0)
                    {
                        // Empty stack, skip this sample.
                        continue;
                    }

                    var threadSample = new ThreadSample
                    {
                        Timestamp = new ThreadSample.Time(sampleStartMillis),
                        TraceIdHigh = traceIdHigh,
                        TraceIdLow = traceIdLow,
                        SpanId = spanId,
                        ManagedId = managedId,
                        NativeId = nativeId,
                        ThreadName = threadName,
                        ThreadIndex = threadIndex
                    };

                    while (code != 0)
                    {
                        string value;
                        if (code < 0)
                        {
                            value = ReadString();
                            _codes[-code] = value;
                        }
                        else
                        {
                            value = _codes[code];
                        }

                        if (value != null)
                        {
                            // we are replacing Datadog.Trace namespace to avoid conflicts while upstream sync
                            var replacedValue = value.Replace("Datadog.Trace.", "SignalFx.Tracing.");
                            threadSample.Frames.Add(replacedValue);
                        }

                        code = ReadShort();
                    }

                    if (threadName == ThreadSampler.BackgroundThreadName)
                    {
                        // TODO Splunk: add configuration option to include the sampler thread. By default remove it.
                        continue;
                    }

                    samples.Add(threadSample);
                }
                else if (operationCode == OpCodes.EndBatch)
                {
                    // end batch, nothing here
                }
                else if (operationCode == OpCodes.BatchStats)
                {
                    var microsSuspended = ReadInt();
                    var numThreads = ReadInt();
                    var totalFrames = ReadInt();
                    var numCacheMisses = ReadInt();

                    if (IsLogLevelDebugEnabled)
                    {
                        Log.Debug(
                        "CLR was suspended for {microsSuspended} microseconds to collect a thread sample batch: threads={numThreads} frames={totalFrames} misses={numCacheMisses}",
                        new object[] { microsSuspended, numThreads, totalFrames, numCacheMisses });
                    }
                }
                else
                {
                    _position = _length + 1;

                    if (IsLogLevelDebugEnabled)
                    {
                        Log.Debug("Not expected operation code while parsing thread stack trace: '{0}'. Operation will be ignored.", operationCode);
                    }
                }
            }

            return samples;
        }

        private string ReadString()
        {
            var length = ReadShort();
            var s = new UnicodeEncoding().GetString(_buffer, _position, length * 2);
            _position += 2 * length;
            return s;
        }

        private short ReadShort()
        {
            var s1 = (short)(_buffer[_position] & 0xFF);
            s1 <<= 8;
            var s2 = (short)(_buffer[_position + 1] & 0xFF);
            _position += 2;
            return (short)(s1 + s2);
        }

        private int ReadInt()
        {
            var i1 = _buffer[_position] & 0xFF;
            i1 <<= 24;
            var i2 = _buffer[_position + 1] & 0xFF;
            i2 <<= 16;
            var i3 = _buffer[_position + 2] & 0xFF;
            i3 <<= 8;
            var i4 = _buffer[_position + 3] & 0xFF;
            _position += 4;
            return i1 + i2 + i3 + i4;
        }

        private long ReadInt64()
        {
            long l1 = _buffer[_position] & 0xFF;
            l1 <<= 56;
            long l2 = _buffer[_position + 1] & 0xFF;
            l2 <<= 48;
            long l3 = _buffer[_position + 2] & 0xFF;
            l3 <<= 40;
            long l4 = _buffer[_position + 3] & 0xFF;
            l4 <<= 32;
            long l5 = _buffer[_position + 4] & 0xFF;
            l5 <<= 24;
            long l6 = _buffer[_position + 5] & 0xFF;
            l6 <<= 16;
            long l7 = _buffer[_position + 6] & 0xFF;
            l7 <<= 8;
            long l8 = _buffer[_position + 7] & 0xFF;
            _position += 8;
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
            /// Marks the beginning of a section with statistics, see THREAD_SAMPLES_FINAL_STATS on native code.
            /// </summary>
            public const byte BatchStats = 0x07;
        }
    }
}
