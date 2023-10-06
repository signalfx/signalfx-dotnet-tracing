using System;
using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.Configuration;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;
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
        var exporter = new SampleExporter(DefaultSettings(), testSender, new IProfileAppender[] { testAppender });

        exporter.Export();
        /* OTLP_PROFILES: TODO:
        using (new AssertionScope())
        {
            testSender.SentLogData.Should().HaveCount(1);
            testSender.LogRecordSnapshots[0].Name.Should().Be("1");
        }

        exporter.Export();

        using (new AssertionScope())
        {
            testSender.SentLogData.Should().HaveCount(2);
            testSender.SentLogData[0].Should().BeSameAs(testSender.SentLogData[1]);

            testSender.LogRecordSnapshots.Should().HaveCount(2);
            testSender.LogRecordSnapshots[1].Name.Should().Be("2");
        }
        */
    }

    private static ImmutableTracerSettings DefaultSettings()
    {
        return new ImmutableTracerSettings(new TracerSettings
        {
            Environment = "test",
            GlobalTags = new Dictionary<string, string>()
        });
    }

    private class TestAppender : IProfileAppender
    {
        private int _count;

        public void AppendTo(List<Profile> results)
        {
            _count += 1;
            results.Add(new Profile
            {
                ProfileId = Guid.Empty.ToByteArray()
            });
        }
    }
}
