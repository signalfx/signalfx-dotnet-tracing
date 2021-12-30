using System;
using System.Collections.Generic;
using System.Text;

namespace Datadog.Trace.ThreadSampling
{
    /// <summary>
    /// Parser the native code's pause-time-optimized format.
    /// </summary>
    internal class ThreadSampleNativeFormatParser
    {
        private static bool printStackTraces = false;

        private byte[] buf;
        private int len;
        private int pos;
        private Dictionary<int, string> codes;

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
        /// Parses the data.  Currently does nothing but print it out
        /// </summary>
        internal void Parse()
        {
            // FIXME actually have this go somewhere in the future
            while (pos < len)
            {
                byte op = buf[pos];
                pos++;
                if (op == 0x01)
                {
                    int version = ReadInt();
                    if (version != 1)
                    {
                        return; // not able to parse
                    }

                    long sampleStartMillis = ReadInt64();
                    // Would like to use DateTimeOffset.FromUnixTimeMilliseconds here but version compatibility won't allow it
                    // A tick is 100 nanos and the big constant there is the unix epoch compared to the windows one, in ticks
                    var sampleStart = new DateTime((sampleStartMillis * 10000) + 621_355_968_000_000_000).ToLocalTime();
                    Console.WriteLine("thread samples captured at " + sampleStart.ToLongDateString() + " " + sampleStart.ToLongTimeString());
                }
                else if (op == 0x02)
                {
                    int managedId = ReadInt();
                    int nativeId = ReadInt();
                    string threadName = ReadString();
                    long traceIdHigh = ReadInt64();
                    long traceIdLow = ReadInt64();
                    long spanId = ReadInt64();
                    if (printStackTraces)
                    {
                        Console.WriteLine("thread mid=" + managedId + " nid=" + nativeId + " name=[" + threadName + "] traceHigh=" + traceIdHigh + " traceLow=" + traceIdLow + " span=" + spanId);
                    }

                    short code = ReadShort();
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

                        if (printStackTraces)
                        {
                            Console.WriteLine("      " + value);
                        }

                        code = ReadShort();
                    }
                }
                else if (op == 0x06)
                {
                    // end batch, nothing here
                }
                else if (op == 0x07)
                {
                    int microsSuspended = ReadInt();
                    Console.WriteLine("  clr was suspended for " + microsSuspended + " micros");
                }
                else
                {
                    pos = len + 1; // FIXME improve error handling here
                }
            }
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
    }
}
