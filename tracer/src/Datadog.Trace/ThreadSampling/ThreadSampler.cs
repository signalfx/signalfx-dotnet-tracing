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
        private static int bufferSize = 100 * 1024; // If you change this, check with ThreadSampler.cpp first

        private static void SampleReadingThread()
        {
            while (true)
            {
                Thread.Sleep(1000);
                Console.WriteLine("reading samples....");
                // FIXME consider calling twice in quick succession and allowing read=0 to silently skip?
                byte[] buf = new byte[bufferSize];
                try
                {
                    var start = DateTime.Now;
                    int read = signalfx_read_thread_samples(buf.Length, buf);
                    Console.WriteLine("read_thread_samples returned " + read + " bytes");
                    var parser = new ThreadSampleNativeFormatParser(buf, read);
                    parser.Parse();
                    var end = DateTime.Now;
                    Console.WriteLine("Parsed stack samples in " + ((end.Ticks - start.Ticks) / TimeSpan.TicksPerMillisecond) + " millis");
                }
                catch (Exception e)
                {
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
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1400 // Access modifier should be declared
        static extern int signalfx_read_thread_samples(int len, byte[] buf);
#pragma warning restore SA1400 // Access modifier should be declared
#pragma warning restore SA1300 // Element should begin with upper-case letter

    }
}
