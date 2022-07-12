using System;
using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.Configuration;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

public class ThreadSampleExporterTests
{
    [Fact]
    public void Span_context_exported_with_samples_when_converted_to_bytes_has_big_endian_order()
    {
        var settings = new ImmutableTracerSettings(new TracerSettings
        {
            ExporterSettings = new ExporterSettings()
            {
                ProfilerExportFormat = ProfilerExportFormat.Text
            },
            ThreadSamplingPeriod = TimeSpan.FromSeconds(1),
            GlobalTags = new Dictionary<string, string>()
        });
        var testSender = new TestSender();
        var exporter = new PlainTextThreadSampleExporter(settings, testSender);

        exporter.ExportThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                SpanId = 1234567890,
                TraceIdLow = 1234567890,
                TraceIdHigh = 0987654321,
                ThreadName = "test_thread_name",
                ThreadIndex = 1,
                ManagedId = 1,
                NativeId = 1,
                Timestamp = new ThreadSample.Time(10000)
            }
        });

        // 1234567890 in big-endian order
        var expectedSpanBytes = new byte[8] { 0, 0, 0, 0, 73, 150, 2, 210 };

        // 987654321 in big-endian order concatenated with 1234567890 in big-endian order
        var expectedTraceBytes = new byte[16] { 0, 0, 0, 0, 58, 222, 104, 177, 0, 0, 0, 0, 73, 150, 2, 210 };

        var log = testSender.SentLogs[0];

        Assert.Equal(expectedSpanBytes, log.SpanId);
        Assert.Equal(expectedTraceBytes, log.TraceId);
    }

    private class TestSender : ILogSender
    {
        public IList<LogRecord> SentLogs { get; } = new List<LogRecord>();

        public void Send(LogsData logsData)
        {
            var logRecord = logsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs[0];
            SentLogs.Add(logRecord);
        }
    }
}
