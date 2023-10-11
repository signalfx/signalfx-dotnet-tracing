// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Threading;
using Datadog.Trace.AlwaysOnProfiler.ProfileAppenders;
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

        private static void SampleReadingThread(SampleExporter sampleExporter, TimeSpan exportInterval)
        {
            while (true)
            {
                Thread.Sleep(exportInterval);
                sampleExporter.Export();
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

            var sampleExporter = GetConfiguredExporter(tracerSettings, cpuProfilingAvailable, memoryProfilingAvailable);

            var thread = new Thread(() =>
            {
                SampleReadingThread(sampleExporter, tracerSettings.ProfilerExportInterval);
            })
            {
                Name = BackgroundThreadName,
                IsBackground = true
            };
            thread.Start();

            Log.Information("AlwaysOnProfiler export thread initialized.");
        }

        private static SampleExporter GetConfiguredExporter(ImmutableTracerSettings tracerSettings, bool cpuProfilingAvailable, bool memoryProfilingAvailable)
        {
            var buffer = new byte[BufferSize];
            var cpuProfileTypeBuilder = new CpuProfileTypeBuilder(tracerSettings.ThreadSamplingPeriod, buffer);
            var allocationProfileTypeBuilder = new AllocationProfileTypeBuilder(buffer);

            List<IProfileTypeBuilder> profileTypeBuilders = cpuProfilingAvailable switch
            {
                // The doubling of the cpuProfileTypeBuilder is intentional: to preserve behavior of SFx.NET original implementation.
                true when memoryProfilingAvailable => new() { cpuProfileTypeBuilder, cpuProfileTypeBuilder, allocationProfileTypeBuilder },
                true => new() { cpuProfileTypeBuilder, cpuProfileTypeBuilder },
                _ => new() { allocationProfileTypeBuilder }
            };

            var profileBuilder = new ProfileBuilder(profileTypeBuilders);
            var otlpHttpSender = new OtlpHttpSender(tracerSettings.ExporterSettings.LogsEndpointUrl);

            return new SampleExporter(tracerSettings, otlpHttpSender, profileBuilder);
        }
    }
}
