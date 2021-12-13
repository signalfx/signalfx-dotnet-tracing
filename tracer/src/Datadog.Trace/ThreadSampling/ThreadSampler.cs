using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Datadog.Trace.ThreadSampling
{
    /// <summary>
    ///  Provides the managed-side thread sample reader
    /// </summary>
    public class ThreadSampler
    {
        // If you change any of these constants, check with ThreadSampler.cpp first
        private static int bufferSize = 100 * 1024;
        private static int defaultSamplePeriod = 1000;
        private static int minimumSamplePeriod = 1000;

        private static int GetThreadSamplingPeriod()
        {
            string period = Environment.GetEnvironmentVariable("SIGNALFX_THREAD_SAMPLING_PERIOD");
            if (period == null)
            {
                return defaultSamplePeriod;
            }

            period = period.Trim();
            if (period.Length == 0)
            {
                return defaultSamplePeriod;
            }

            try
            {
                return Math.Max(int.Parse(period), minimumSamplePeriod);
            }
            catch
            {
                return defaultSamplePeriod;
            }
        }

        private static void ReadOneSample(byte[] buf)
        {
            var start = DateTime.Now;
            int read = SignalFx_read_thread_samples(buf.Length, buf);
            if (read > 0)
            {
                var parser = new ThreadSampleNativeFormatParser(buf, read);
                parser.Parse();
                var end = DateTime.Now;
                Console.WriteLine("Parsed stack samples in " + ((end.Ticks - start.Ticks) / TimeSpan.TicksPerMillisecond) + " millis from " + read + " bytes");
            }
        }

        private static void SampleReadingThread()
        {
            int period = GetThreadSamplingPeriod();
            while (true)
            {
                Thread.Sleep(period);
                byte[] buf = new byte[bufferSize];
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
        public static void Initialize()
        {
            var thread = new Thread(new ThreadStart(SampleReadingThread));
            thread.IsBackground = true;
            thread.Start();
        }

        [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
#pragma warning disable SA1400 // Access modifier should be declared
        static extern int SignalFx_read_thread_samples(int len, byte[] buf);
#pragma warning restore SA1400 // Access modifier should be declared

    }
}
