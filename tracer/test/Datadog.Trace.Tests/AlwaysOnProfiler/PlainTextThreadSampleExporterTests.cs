using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.Configuration;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

public class PlainTextThreadSampleExporterTests
{
    [Fact]
    public void Span_context_exported_with_samples_when_converted_to_bytes_has_big_endian_order()
    {
        var testSender = new TestSender();
        var exporter = new PlainTextThreadSampleExporter(DefaultSettings(), testSender);

        exporter.ExportThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                SpanId = 1234567890,
                TraceIdLow = 1234567890,
                TraceIdHigh = 0987654321,
                Timestamp = new ThreadSample.Time(10000)
            }
        });

        // 1234567890 in big-endian order
        var expectedSpanBytes = new byte[8] { 0, 0, 0, 0, 73, 150, 2, 210 };

        // 987654321 in big-endian order concatenated with 1234567890 in big-endian order
        var expectedTraceBytes = new byte[16] { 0, 0, 0, 0, 58, 222, 104, 177, 0, 0, 0, 0, 73, 150, 2, 210 };

        var log = testSender.SentLogs[0];

        log.SpanId.Should().Equal(expectedSpanBytes);
        log.TraceId.Should().Equal(expectedTraceBytes);
    }

    [Fact]
    public void Timestamp_is_set_on_log_record()
    {
        var settings = DefaultSettings();
        var testSender = new TestSender();
        var exporter = new PlainTextThreadSampleExporter(settings, testSender);

        exporter.ExportThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                Timestamp = new ThreadSample.Time(milliseconds: 1000)
            }
        });

        var sentLog = testSender.SentLogs[0];

        sentLog.TimeUnixNano.Should().Be(1000 * 1_000_000u);
    }

    [Fact]
    public void Event_period_is_added_to_log_record_attributes()
    {
        const long expectedPeriod = 1000;
        var settings = DefaultSettings(samplingPeriodMs: expectedPeriod);
        var testSender = new TestSender();
        var exporter = new PlainTextThreadSampleExporter(settings, testSender);

        exporter.ExportThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                Timestamp = new ThreadSample.Time(10000)
            }
        });

        var sentLog = testSender.SentLogs[0];
        var eventPeriod = sentLog.Attributes.Single(kv => kv.Key == "source.event.period");
        eventPeriod.Value.IntValue.Should().Be(expectedPeriod);
    }

    [Fact]
    public void Format_is_added_to_log_record_attributes()
    {
        var settings = DefaultSettings();
        var testSender = new TestSender();
        var exporter = new PlainTextThreadSampleExporter(settings, testSender);

        exporter.ExportThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                Timestamp = new ThreadSample.Time(10000)
            }
        });

        var sentLog = testSender.SentLogs[0];
        var format = sentLog.Attributes.Single(kv => kv.Key == "profiling.data.format");
        format.Value.StringValue.Should().Be("text");
    }

    [Fact]
    public void Required_attributes_are_set_for_exported_allocation_sample()
    {
        var settings = DefaultSettings();
        var testSender = new TestSender();
        var exporter = new PlainTextThreadSampleExporter(settings, testSender);

        var allocationSample = new AllocationSample(
            1000,
            "System.String",
            new ThreadSample
            {
                Timestamp = new ThreadSample.Time(10000)
            });

        exporter.ExportAllocationSamples(new List<AllocationSample> { allocationSample });

        var sentLog = testSender.SentLogs[0];

        using (new AssertionScope())
        {
            // https://github.com/signalfx/gdi-specification/blob/bd5c6f56f26b535c6fa922d2d9b5d00a7b7b5afd/specification/semantic_conventions.md#logrecord-message-common-attributes
            // These attributes are common for cpu and allocation samples
            var sourceType = sentLog.Attributes.Single(kv => kv.Key == "com.splunk.sourcetype");
            sourceType.Value.StringValue.Should().Be("otel.profiling");

            var dataType = sentLog.Attributes.Single(kv => kv.Key == "profiling.data.type");
            dataType.Value.StringValue.Should().Be("allocation");

            var dataFormat = sentLog.Attributes.Single(kv => kv.Key == "profiling.data.format");
            dataFormat.Value.StringValue.Should().Be("text");

            // https://github.com/signalfx/gdi-specification/blob/bd5c6f56f26b535c6fa922d2d9b5d00a7b7b5afd/specification/semantic_conventions.md#logrecord-message-text-data-format-specific-attributes
            // This is specific to allocation sample
            var memoryAllocated = sentLog.Attributes.Single(kv => kv.Key == "memory.allocated");
            memoryAllocated.Value.IntValue.Should().Be(1000);
        }
    }

    [Fact]
    public void Context_is_set_for_exported_allocation_sample()
    {
        var settings = DefaultSettings();
        var testSender = new TestSender();
        var exporter = new PlainTextThreadSampleExporter(settings, testSender);

        var allocationSample = new AllocationSample(
            1000,
            "System.String",
            new ThreadSample
            {
                Timestamp = new ThreadSample.Time(1_000),
                SpanId = 1234567890,
                TraceIdLow = 1234567890,
                TraceIdHigh = 0987654321
            });

        exporter.ExportAllocationSamples(new List<AllocationSample> { allocationSample });

        var sentLog = testSender.SentLogs[0];

        var expectedSpanBytes = new byte[8] { 0, 0, 0, 0, 73, 150, 2, 210 };

        // 987654321 in big-endian order concatenated with 1234567890 in big-endian order
        var expectedTraceBytes = new byte[16] { 0, 0, 0, 0, 58, 222, 104, 177, 0, 0, 0, 0, 73, 150, 2, 210 };

        using (new AssertionScope())
        {
            sentLog.SpanId.Should().Equal(expectedSpanBytes);
            sentLog.TraceId.Should().Equal(expectedTraceBytes);

            sentLog.TimeUnixNano.Should().Be(1_000_000_000);
        }
    }

    private static ImmutableTracerSettings DefaultSettings(long samplingPeriodMs = 10000)
    {
        return new ImmutableTracerSettings(new TracerSettings
        {
            ExporterSettings = new ExporterSettings
            {
                ProfilerExportFormat = ProfilerExportFormat.Text
            },
            ThreadSamplingPeriod = TimeSpan.FromMilliseconds(samplingPeriodMs),
            GlobalTags = new Dictionary<string, string>()
        });
    }
}
