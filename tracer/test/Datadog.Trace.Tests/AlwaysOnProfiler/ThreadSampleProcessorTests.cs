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

public class ThreadSampleProcessorTests
{
    [Fact]
    public void If_stack_sample_has_span_context_associated_then_it_is_exported_inside_labels()
    {
        var exporter = new ThreadSampleProcessor(
            DefaultSettings());

        var profile = exporter.ProcessThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                SpanId = 1234567890,
                TraceIdLow = 9876543210,
                TraceIdHigh = 1234567890,
                Timestamp = new ThreadSample.Time(10_000),
            },
            new ThreadSample()
            {
                SpanId = 9876543210,
                TraceIdLow = 1234567890,
                TraceIdHigh = 9876543210,
                Timestamp = new ThreadSample.Time(11_000),
            },
        });

        profile.ProfileId.Should().BeEquivalentTo(Guid.Empty.ToByteArray());
        profile.StartTimeUnixNano.Should().Be(10_000_000_000);
        profile.EndTimeUnixNano.Should().Be(11_000_000_000);
        profile.Stacktraces.Should().ContainSingle();
        profile.Links.Should().HaveCount(3); // 2 different trace IDs, plus mandatory "empty" entry
        profile.ProfileTypes.Should().ContainSingle();

        // OTLP_PROFILES: TODO: profile.Links[1] and profile.Links[2]

        var cpuProfile = profile.ProfileTypes[0];
        cpuProfile.StacktraceIndices.Should().HaveCount(2);
        cpuProfile.StacktraceIndices.Should().AllBeEquivalentTo(0);
        cpuProfile.LinkIndices.Should().HaveCount(2);
        cpuProfile.LinkIndices.Should().NotContain(index => index == 0); // None of the thread samples have
        cpuProfile.Timestamps.Should().HaveCount(2);
        cpuProfile.Timestamps.Should().Satisfy(ts => ts == 10_000_000_000, ts => ts == 11_000_000_000);

        // Check default sampling rate.
        cpuProfile.SampleRate.Should().Be(10_000);
        profile.StringTables[(int)cpuProfile.TypeIndex].Should().Be("cpu");
        profile.StringTables[(int)cpuProfile.UnitIndex].Should().Be("ms");
    }

    [Fact]
    public void Sample_rate_is_exported_in_profile_type()
    {
        const long expectedPeriod = 1000;

        var exporter = new ThreadSampleProcessor(
            DefaultSettings(samplingPeriodMs: expectedPeriod));

        var profile = exporter.ProcessThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                Timestamp = new ThreadSample.Time(11000)
            }
        });

        var profileType = profile.ProfileTypes[0];
        profileType.SampleRate.Should().Be(expectedPeriod);
        profile.StringTables[(int)profileType.TypeIndex].Should().Be("cpu");
        profile.StringTables[(int)profileType.UnitIndex].Should().Be("ms");
    }

    [Fact]
    public void Thread_info_is_exported_inside_attribute_sets()
    {
        var exporter = new ThreadSampleProcessor(
            DefaultSettings());

        var profile = exporter.ProcessThreadSamples(new List<ThreadSample>
        {
            new ThreadSample()
            {
                Timestamp = new ThreadSample.Time(10000),
                ThreadIndex = 0,
                ManagedId = 2,
                ThreadName = "test_thread"
            }
        });

        using (new AssertionScope())
        {
            var profileType = profile.ProfileTypes[0];
            var attributeSetIndex = profileType.AttributeSetIndices[0];
            var attributeSet = profile.AttributeSets[(int)attributeSetIndex];
            attributeSet.Attributes.Should().Contain(kv => kv.Key == "thread.name" && profile.StringTables[(int)kv.Value.IntValue] == "test_thread");
            attributeSet.Attributes.Should().Contain(kv => kv.Key == "thread.id" && kv.Value.IntValue == 2);
        }
    }

    /*
    [Fact]
    public void Allocation_size_in_bytes_is_sent_as_a_value_for_exported_allocation_sample()
    {
        var exporter = new ThreadSampleProcessor(
            DefaultSettings());

        var allocationSample = new AllocationSample(
            1000,
            "System.String",
            new ThreadSample
            {
                Timestamp = new ThreadSample.Time(10000),
                ThreadIndex = 0,
                ManagedId = 2,
                ThreadName = "test_thread"
            });

        var logRecord = exporter.ProcessAllocationSamples(
            new List<AllocationSample> { allocationSample });

        var profile = Deserialize(logRecord.Body.StringValue);

        using (new AssertionScope())
        {
            var values = profile.Samples[0].Values;
            values.Length.Should().Be(1);

            var allocationSize = values[0];
            allocationSize.Should().Be(1000);
        }
    }

    [Fact]
    public void Log_record_type_is_set_to_allocation_for_exported_allocation_sample()
    {
        var exporter = new ThreadSampleProcessor(
            DefaultSettings());

        var allocationSample = new AllocationSample(
            1000,
            "System.String",
            new ThreadSample
            {
                Timestamp = new ThreadSample.Time(10000),
                ThreadIndex = 0,
                ManagedId = 2,
                ThreadName = "test_thread"
            });

        var logRecord = exporter.ProcessAllocationSamples(new List<AllocationSample> { allocationSample });

        var profilingDataType = logRecord.Attributes.Single(kv => kv.Key == "profiling.data.type");
        profilingDataType.Value.StringValue.Should().Be("allocation");
    }

    [Fact]
    public void Common_labels_are_set_for_exported_allocation_sample()
    {
        var exporter = new ThreadSampleProcessor(
            DefaultSettings());

        var allocationSample = new AllocationSample(
            1000,
            "System.String",
            new ThreadSample
            {
                Timestamp = new ThreadSample.Time(1000),
                ThreadIndex = 0,
                ManagedId = 2,
                ThreadName = "test_thread",
                SpanId = 1234567890,
                TraceIdLow = 9876543210,
                TraceIdHigh = 1234567890
            });

        var logRecord = exporter.ProcessAllocationSamples(new List<AllocationSample> { allocationSample });

        var profile = Deserialize(logRecord.Body.StringValue);

        using (new AssertionScope())
        {
            // https://github.com/signalfx/gdi-specification/blob/bd5c6f56f26b535c6fa922d2d9b5d00a7b7b5afd/specification/semantic_conventions.md#pprof-profileproto-data-format
            // These labels are common for cpu and allocation samples

            var timestamp = GetLabelNum("source.event.time", profile);
            timestamp.Should().Be(1000);

            var traceId = GetLabelString("trace_id", profile.StringTables, profile.Samples[0]);
            // concatenated hex representations of 1234567890 and 9876543210
            traceId.Should().Be("00000000499602d2000000024cb016ea");

            var spanId = GetLabelString("span_id", profile.StringTables, profile.Samples[0]);
            // hex representation of 1234567890
            spanId.Should().Be("00000000499602d2");

            var managedThreadId = GetLabelNum("thread.id", profile);
            managedThreadId.Should().Be(2);

            var threadName = GetLabelString("thread.name", profile.StringTables, profile.Samples[0]);
            threadName.Should().Be("test_thread");
        }
    }

    [Fact]
    public void Total_frame_count_is_added_to_logRecord_attributes_for_cpu_samples()
    {
        var exporter = new ThreadSampleProcessor(DefaultSettings());

        var firstSample = DefaultSample("frame1", "frame2");

        var secondSample = DefaultSample("frame1");

        var logRecord = exporter.ProcessThreadSamples(new List<ThreadSample> { firstSample, secondSample });

        var profilingDataType = logRecord.Attributes.Single(kv => kv.Key == "profiling.data.total.frame.count");
        profilingDataType.Value.IntValue.Should().Be(3);
    }

    [Fact]
    public void Total_frame_count_is_added_to_logRecord_attributes_for_allocation_samples()
    {
        var exporter = new ThreadSampleProcessor(
            DefaultSettings());

        var firstSample = new AllocationSample(
            1000,
            "System.String",
            DefaultSample("frame1", "frame2"));

        var secondSample = new AllocationSample(
            1000,
            "System.String",
            DefaultSample("frame1"));

        var logRecord = exporter.ProcessAllocationSamples(new List<AllocationSample> { firstSample, secondSample });

        var profilingDataType = logRecord.Attributes.Single(kv => kv.Key == "profiling.data.total.frame.count");
        profilingDataType.Value.IntValue.Should().Be(3);
    }

    private static ThreadSample DefaultSample(params string[] frames)
    {
        var sample = new ThreadSample
        {
            Timestamp = new ThreadSample.Time(10000),
            ThreadIndex = 0,
            ManagedId = 2,
            ThreadName = "test_thread"
        };
        foreach (var frame in frames)
        {
            sample.Frames.Add(frame);
        }

        return sample;
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

    private static Profile Deserialize(string body)
    {
        using var memoryStream = new MemoryStream(Convert.FromBase64String(body));
        using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
        var profile = Serializer.Deserialize<Profile>(gzipStream);
        return profile;
    }
    */

    private static ImmutableTracerSettings DefaultSettings(long samplingPeriodMs = 10000)
    {
        return new ImmutableTracerSettings(new TracerSettings
        {
            ExporterSettings = new ExporterSettings(),
            ThreadSamplingPeriod = TimeSpan.FromMilliseconds(samplingPeriodMs)
        });
    }
}
