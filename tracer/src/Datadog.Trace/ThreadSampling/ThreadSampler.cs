// Modified by Splunk Inc.

using System;
using System.Threading;
using Datadog.Trace.ClrProfiler;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ThreadSampling
{
    /// <summary>
    ///  Provides the managed-side thread sample reader
    /// </summary>
    internal static class ThreadSampler
    {
        /// <summary>
        /// Name of the managed thread that periodically captures and exports the thread samples.
        /// </summary>
        public const string BackgroundThreadName = "SignalFx Profiling Sampler Thread";

        // If you change any of these constants, check with thread_sampler.cpp first
        private const int BufferSize = 200 * 1024;

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(ThreadSampleExporter));

        private static ImmutableTracerSettings _tracerSettings;

        private static void ReadAndExportThreadSampleBatch(byte[] buf, ThreadSampleExporter exporter)
        {
            int read = NativeMethods.SignalFxReadThreadSamples(buf.Length, buf);
            if (read <= 0)
            {
                // No data just return.
                return;
            }

            var parser = new ThreadSampleNativeFormatParser(buf, read);
            var threadSamples = parser.Parse();

            exporter.ExportThreadSamples(threadSamples);
        }

        private static void SampleReadingThread()
        {
            var buf = new byte[BufferSize];
            var exporter = new ThreadSampleExporter(_tracerSettings);

            while (true)
            {
                Thread.Sleep(_tracerSettings.ThreadSamplingPeriod);
                try
                {
                    // Call twice in quick succession to catch up any blips; the second will likely return 0 (no buffer)
                    ReadAndExportThreadSampleBatch(buf, exporter);
                    ReadAndExportThreadSampleBatch(buf, exporter);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing thread samples batch.");
                }
            }
        }

        /// <summary>
        ///  Initializes the managed-side thread sample reader.
        /// </summary>
        /// <param name="tracerSettings">Configuration settings.</param>
        public static void Initialize(ImmutableTracerSettings tracerSettings)
        {
            _tracerSettings = tracerSettings;

            var thread = new Thread(SampleReadingThread)
            {
                Name = BackgroundThreadName,
                IsBackground = true
            };
            thread.Start();
        }
    }
}
