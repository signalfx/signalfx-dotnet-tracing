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
        private static bool printStackTraces = true;

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
                    // start batch, nothing here
                }
                else if (op == 0x02)
                {
                    int managedId = ReadInt();
                    int nativeId = ReadInt();
                    string threadName = ReadString();
                    if (printStackTraces)
                    {
                        Console.WriteLine("thread " + managedId + " " + nativeId + " " + threadName);
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
                else
                {
                    pos = len + 1; // FIXME improve error handling here
                }
            }
        }

        private string ReadString()
        {
            int len = ReadInt();
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
    }
}
