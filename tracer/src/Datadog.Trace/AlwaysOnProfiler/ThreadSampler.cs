// Modified by Splunk Inc.

using System;
using System.Threading;
using Datadog.Trace.AlwaysOnProfiler.NativeBufferExporters;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;

namespace Datadog.Trace.AlwaysOnProfiler
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

        // If you change any of these constants, check with always_on_profiler.cpp first
        private const int BufferSize = 200 * 1024;

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(ThreadSampler));

        private static void SampleReadingThread(INativeBufferExporter nativeBufferExporter, TimeSpan exportInterval)
        {
            var buffer = new byte[BufferSize];

            while (true)
            {
                Thread.Sleep(exportInterval);
                nativeBufferExporter.Export(buffer);
            }
        }

        /// <summary>
        ///  Initializes the managed-side thread sample reader.
        /// </summary>
        /// <param name="tracerSettings">Configuration settings.</param>
        public static void Initialize(ImmutableTracerSettings tracerSettings)
        {
            if (tracerSettings.CpuProfilingEnabled && !FrameworkDescription.Instance.SupportsCpuProfiling())
            {
                Log.Warning("Cpu profiling enabled but not supported.");
            }

            if (tracerSettings.MemoryProfilingEnabled && !FrameworkDescription.Instance.SupportsMemoryProfiling())
            {
                Log.Warning("Memory profiling enabled but not supported.");
            }

            var cpuProfilingAvailable = tracerSettings.CpuProfilingEnabled && FrameworkDescription.Instance.SupportsCpuProfiling();
            var memoryProfilingAvailable = tracerSettings.MemoryProfilingEnabled && FrameworkDescription.Instance.SupportsMemoryProfiling();

            if (!cpuProfilingAvailable && !memoryProfilingAvailable)
            {
                Log.Warning("Environment does not meet AlwaysOnProfiler requirements.");
                return;
            }

            Log.Debug("Initializing AlwaysOnProfiler export thread.");

            var bufferExporter = GetConfiguredExporter(tracerSettings, cpuProfilingAvailable, memoryProfilingAvailable);

            var thread = new Thread(() =>
            {
                SampleReadingThread(bufferExporter, tracerSettings.ProfilerExportInterval);
            })
            {
                Name = BackgroundThreadName,
                IsBackground = true
            };
            thread.Start();

            Log.Information("AlwaysOnProfiler export thread initialized.");
        }

        private static INativeBufferExporter GetConfiguredExporter(ImmutableTracerSettings tracerSettings, bool cpuProfilingAvailable, bool memoryProfilingAvailable)
        {
            var exporterFactory = new ThreadSampleExporterFactory(tracerSettings);
            var sampleExporter = exporterFactory.CreateThreadSampleExporter();

            var cpuBufferExporter = new CpuNativeBufferExporter(sampleExporter);
            var allocationBufferExporter = new AllocationNativeBufferExporter(sampleExporter);

            INativeBufferExporter configuredExporter = cpuProfilingAvailable switch
            {
                true when memoryProfilingAvailable => new SequentialNativeBufferExporter(cpuBufferExporter, allocationBufferExporter),
                true => cpuBufferExporter,
                _ => allocationBufferExporter
            };

            return configuredExporter;
        }
    }
}
