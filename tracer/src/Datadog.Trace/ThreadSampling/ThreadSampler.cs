using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Datadog.Trace.ThreadSampling
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented
    public class ThreadSampler
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        // FIXME JBLEY implement for real, clean up all these warnings
        private static void SampleReadingThread()
        {
            while (true)
            {
                Thread.Sleep(1000);
                Console.WriteLine("-- ThreadSampler.cs running --");
                byte[] buf = new byte[100 * 1024];
                int read = signalfx_read_thread_samples(buf.Length, buf);
                Console.WriteLine("read_thread_samples returned " + read);
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented
        public static void Initialize()
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
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
