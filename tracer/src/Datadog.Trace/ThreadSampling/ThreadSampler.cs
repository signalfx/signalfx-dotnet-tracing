using System;
using System.Threading;
using Datadog.Trace.ClrProfiler;

namespace Datadog.Trace.ThreadSampling
{
    /// <summary>
    ///  Provides the managed-side thread sample reader
    /// </summary>
    public static class ThreadSampler
    {
        // If you change any of these constants, check with ThreadSampler.cpp first
        private const int BufferSize = 200 * 1024;

        private static TimeSpan _threadSamplingPeriod;

        private static void ReadOneSample(byte[] buf)
        {
            var start = DateTime.UtcNow;
            int read = NativeMethods.SignalFxReadThreadSamples(buf.Length, buf);
            if (read > 0)
            {
                var parser = new ThreadSampleNativeFormatParser(buf, read);
                parser.Parse();
                var end = DateTime.UtcNow;
                Console.WriteLine("Parsed stack samples in " + ((end.Ticks - start.Ticks) / TimeSpan.TicksPerMillisecond) + " millis from " + read + " bytes");
            }
        }

        private static void SampleReadingThread()
        {
            byte[] buf = new byte[BufferSize];
            while (true)
            {
                Thread.Sleep(_threadSamplingPeriod);
                try
                {
                    // Call twice in quick succession to catch up any blips; the second will likely return 0 (no buffer)
                    ReadOneSample(buf);
                    ReadOneSample(buf);
                }
                catch (Exception e)
                {
                    // FIXME logging
                    Console.WriteLine("could not read samples: " + e);
                }
            }
        }

        /// <summary>
        ///  Initializes the managed-side thread sample reader
        /// </summary>
        /// <param name="threadSamplingPeriod">Thread sampling period</param>
        public static void Initialize(TimeSpan threadSamplingPeriod)
        {
            _threadSamplingPeriod = threadSamplingPeriod;

            var thread = new Thread(SampleReadingThread)
            {
                IsBackground = true
            };
            thread.Start();
        }
    }
}
