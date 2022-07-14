using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.Configuration;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.Pprof.Proto.Profile;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

public class PprofThreadSampleExporterTests
{
    [Fact]
    public void If_stack_sample_has_span_context_associated_then_it_is_sent_inside_labels()
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
            }
        });

        var log = sender.SentLogs[0];

        // for Pprof exporter, span context SHOULD NOT be set on logRecord level
        Assert.Null(log.TraceId);
        Assert.Null(log.SpanId);

        var profile = Deserialize(log.Body.StringValue);

        var spanId = GetId("span_id", profile);

        // hex representation of 1234567890
        Assert.Equal("00000000499602d2", spanId);

        var traceId = GetId("trace_id", profile);

        // concatenated hex representations of 1234567890 and 9876543210
        Assert.Equal("00000000499602d2000000024cb016ea", traceId);
    }

    private static string GetId(string id, Profile profile)
    {
        var stringTables = profile.StringTables;
        var idKey = stringTables.IndexOf(id);
        var idLabel = profile.Samples[0].Labels.Single(label => label.Key == idKey);
        return stringTables[(int)idLabel.Str];
    }

    private static ImmutableTracerSettings DefaultSettings()
    {
        return new ImmutableTracerSettings(new TracerSettings
        {
            ExporterSettings = new ExporterSettings
            {
                ProfilerExportFormat = ProfilerExportFormat.Pprof
            },
            ThreadSamplingPeriod = TimeSpan.FromMilliseconds(1000)
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
