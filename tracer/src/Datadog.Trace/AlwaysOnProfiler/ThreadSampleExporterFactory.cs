// Modified by Splunk Inc.

using Datadog.Trace.Configuration;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class ThreadSampleExporterFactory
    {
        private ImmutableTracerSettings _tracerSettings;

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

            return new SimpleThreadSampleExporter(_tracerSettings);
        }
    }
}
