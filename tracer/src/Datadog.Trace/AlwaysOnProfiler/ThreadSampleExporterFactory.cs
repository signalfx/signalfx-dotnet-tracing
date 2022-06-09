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

        public ThreadSampleExporter CreateThreadSampleExporter()
        {
            return _tracerSettings.ProfilerExportFormat switch
            {
                ProfilerExportFormat.Pprof => new PprofThreadSampleExporter(_tracerSettings),
                ProfilerExportFormat.Text => new PlainTextThreadSampleExporter(_tracerSettings),
                _ => throw new ArgumentOutOfRangeException(nameof(_tracerSettings.ProfilerExportFormat), _tracerSettings.ProfilerExportFormat, null)
            };
        }
    }
}
