using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.Configuration;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

public class SampleExporterTests
{
    [Fact]
    public void Logs_data_is_reused_between_export_attempts()
    {
        var testAppender = new TestAppender();
        var testSender = new TestSender();
        var exporter = new SampleExporter(DefaultSettings(), testSender, new ILogRecordAppender[] { testAppender });

        // Initially, LogsData has no log records.
        exporter.LogsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs.Should().BeEmpty();

        exporter.Export();

        using (new AssertionScope())
        {
            testSender.SentLogData.Should().HaveCount(1);

            // Exporter's LogsData was used to export logRecords.
            testSender.SentLogData[0].Should().BeSameAs(exporter.LogsData);

            // Exporter's LogsData was not empty when an attempt to export using testSender was made.
            testSender.LogRecordSnapshots[0].Name.Should().Be("1");

            // LogsData's log records are cleared after an export attempt.
            exporter.LogsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs.Should().BeEmpty();
        }

        exporter.Export();

        using (new AssertionScope())
        {
            testSender.SentLogData.Should().HaveCount(2);
            testSender.SentLogData[0].Should().BeSameAs(exporter.LogsData);
            testSender.SentLogData[1].Should().BeSameAs(exporter.LogsData);

            testSender.LogRecordSnapshots.Should().HaveCount(2);
            testSender.LogRecordSnapshots[1].Name.Should().Be("2");

            exporter.LogsData.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs.Should().BeEmpty();
        }
    }

    private static ImmutableTracerSettings DefaultSettings()
    {
        return new ImmutableTracerSettings(new TracerSettings
        {
            Environment = "test",
            GlobalTags = new Dictionary<string, string>()
        });
    }

    private class TestAppender : ILogRecordAppender
    {
        private int _count;

        public void AppendTo(List<LogRecord> results)
        {
            _count += 1;
            results.Add(new LogRecord
            {
                Name = _count.ToString()
            });
        }
    }
}
