// Modified by Splunk Inc.

using System;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class ThreadSampleExporterFactory
    {
        protected static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(ThreadSampleExporterFactory));
        private readonly ImmutableTracerSettings _tracerSettings;

        public ThreadSampleExporterFactory(ImmutableTracerSettings tracerSettings)
        {
            _tracerSettings = tracerSettings;
        }

        public IThreadSampleExporter CreateThreadSampleExporter()
        {
            var exporterType = _tracerSettings.ProfilerExportType;

            if (exporterType == "pprof")
            {
                return new PprofThreadSampleExporter(_tracerSettings);
            }

            if (exporterType == "text")
            {
                return new PlainTextThreadSampleExporter(_tracerSettings);
            }

            Log.Warning("Unknown exporter type passed: {0}. Profiling will not be enabled.", exporterType);
            throw new ArgumentException($"Exporter type {exporterType} us unknown");
        }
    }
}
