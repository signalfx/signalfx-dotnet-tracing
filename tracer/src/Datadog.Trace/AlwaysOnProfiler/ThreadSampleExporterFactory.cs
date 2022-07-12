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
            var logSender = new OtlpHttpLogSender(_tracerSettings.ExporterSettings.LogsEndpointUrl);
            return _tracerSettings.ExporterSettings.ProfilerExportFormat switch
            {
                ProfilerExportFormat.Pprof => new PprofThreadSampleExporter(_tracerSettings, logSender),
                ProfilerExportFormat.Text => new PlainTextThreadSampleExporter(_tracerSettings, logSender),
                _ => throw new ArgumentOutOfRangeException(nameof(_tracerSettings.ExporterSettings.ProfilerExportFormat), _tracerSettings.ExporterSettings.ProfilerExportFormat, "Export format not supported.")
            };
        }
    }
}
