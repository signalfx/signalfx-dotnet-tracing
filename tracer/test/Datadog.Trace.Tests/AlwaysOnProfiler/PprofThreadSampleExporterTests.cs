using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.Configuration;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.Pprof.Proto.Profile;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

public class PprofThreadSampleExporterTests
{
    [Fact]
    public void If_stack_sample_has_span_context_associated_then_it_is_exported_inside_labels()
    {
        var sender = new TestSender();
        var exporter = new PprofThreadSampleExporter(
            DefaultSettings(),
            sender);

        exporter.ExportThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                SpanId = 1234567890,
                TraceIdLow = 9876543210,
                TraceIdHigh = 1234567890,
                Timestamp = new ThreadSample.Time(10000),
            },
            new ThreadSample()
            {
                SpanId = 9876543210,
                TraceIdLow = 1234567890,
                TraceIdHigh = 9876543210,
                Timestamp = new ThreadSample.Time(10000),
            },
        });

        sender.SentLogs.Count.Should().Be(1, "all of the samples should be sent inside single LogRecord.");

        var log = sender.SentLogs[0];

        log.TraceId.Should().BeNull("traceId should not be set on LogRecord level for pprof exporter.");
        log.SpanId.Should().BeNull("spanId should not be set on LogRecord level for pprof exporter.");

        var profile = Deserialize(log.Body.StringValue);

        profile.Samples.Count.Should().Be(2);

        using (new AssertionScope())
        {
            var firstSpanId = GetLabelString("span_id", profile.StringTables, profile.Samples[0]);

            // hex representation of 1234567890
            firstSpanId.Should().Be("00000000499602d2");

            var firstTraceId = GetLabelString("trace_id", profile.StringTables, profile.Samples[0]);

            // concatenated hex representations of 1234567890 and 9876543210
            firstTraceId.Should().Be("00000000499602d2000000024cb016ea");

            var secondSpanId = GetLabelString("span_id", profile.StringTables, profile.Samples[1]);

            // hex representation of 9876543210
            secondSpanId.Should().Be("000000024cb016ea");

            var secondTraceId = GetLabelString("trace_id", profile.StringTables, profile.Samples[1]);

            // concatenated hex representations of 9876543210 and 1234567890
            secondTraceId.Should().Be("000000024cb016ea00000000499602d2");
        }
    }

    [Fact]
    public void Event_period_is_exported_inside_labels()
    {
        const long expectedPeriod = 1000;

        var sender = new TestSender();
        var exporter = new PprofThreadSampleExporter(
            DefaultSettings(samplingPeriodMs: expectedPeriod),
            sender);

        exporter.ExportThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                Timestamp = new ThreadSample.Time(10000)
            }
        });

        var sentLog = sender.SentLogs[0];

        var profile = Deserialize(sentLog.Body.StringValue);

        var eventPeriod = GetLabelNum("source.event.period", profile);

        eventPeriod.Should().Be(expectedPeriod);
    }

    [Fact]
    public void Format_is_added_to_log_record_attributes()
    {
        var sender = new TestSender();
        var exporter = new PprofThreadSampleExporter(
            DefaultSettings(),
            sender);

        exporter.ExportThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                Timestamp = new ThreadSample.Time(10000)
            }
        });

        var sentLog = sender.SentLogs[0];

        var format = sentLog.Attributes.Single(kv => kv.Key == "profiling.data.format");
        format.Value.StringValue.Should().Be("pprof-gzip-base64");
    }

    [Fact]
    public void Thread_info_is_exported_inside_labels()
    {
        var sender = new TestSender();
        var exporter = new PprofThreadSampleExporter(
            DefaultSettings(),
            sender);

        exporter.ExportThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                Timestamp = new ThreadSample.Time(10000),
                ThreadIndex = 0,
                NativeId = 1,
                ManagedId = 2,
                ThreadName = "test_thread"
            }
        });

        var sentLog = sender.SentLogs[0];

        var profile = Deserialize(sentLog.Body.StringValue);

        using (new AssertionScope())
        {
            var threadName = GetLabelString("thread.name", profile.StringTables, profile.Samples[0]);
            threadName.Should().Be("test_thread");

            var nativeId = GetLabelNum("thread.os.id", profile);
            nativeId.Should().Be(1);

            var managedThreadId = GetLabelNum("thread.id", profile);
            managedThreadId.Should().Be(2);
        }
    }

    private static long GetLabelNum(string labelName, Profile profile)
    {
        var label = GetLabel(labelName, profile.StringTables, profile.Samples[0]);
        return label.Num;
    }

    private static string GetLabelString(string labelName, IList<string> profileStringTables, Sample profileSample)
    {
        var label = GetLabel(labelName, profileStringTables, profileSample);
        return profileStringTables[(int)label.Str];
    }

    private static Label GetLabel(string labelName, IList<string> profileStringTables, Sample profileSample)
    {
        var labelKeyIndex = profileStringTables.IndexOf(labelName);
        var label = profileSample.Labels.Single(label => label.Key == labelKeyIndex);
        return label;
    }

    private static ImmutableTracerSettings DefaultSettings(long samplingPeriodMs = 10000)
    {
        return new ImmutableTracerSettings(new TracerSettings
        {
            ExporterSettings = new ExporterSettings
            {
                ProfilerExportFormat = ProfilerExportFormat.Pprof
            },
            ThreadSamplingPeriod = TimeSpan.FromMilliseconds(samplingPeriodMs)
        });
    }

    private static Profile Deserialize(string body)
    {
        using var memoryStream = new MemoryStream(Convert.FromBase64String(body));
        using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
        var profile = Serializer.Deserialize<Profile>(gzipStream);
        return profile;
    }
}
