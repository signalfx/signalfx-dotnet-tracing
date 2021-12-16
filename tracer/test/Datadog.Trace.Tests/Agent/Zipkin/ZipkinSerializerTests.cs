using System.Collections.Generic;
using System.IO;
using System.Linq;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers.Factories;
using Newtonsoft.Json;
using Xunit;
using static Datadog.Trace.Agent.ZipkinSerializer;

namespace Datadog.Trace.Tests.Agent.Zipkin
{
    public class ZipkinSerializerTests
    {
        [Fact]
        public void RoundTripRootSpanTest()
        {
            var settings = new TracerSettings().Build();
            var span = SpanFactory.CreateSpan();
            var zipkinSpan = new ZipkinSpan(span, settings);

            var serializer = new ZipkinSerializer(settings);
            using var ms = new MemoryStream();

            serializer.Serialize(ms, new[] { new[] { span } });

            ms.Position = 0;
            var jsonText = new StreamReader(ms).ReadToEnd();
            var actualTraces = JsonConvert.DeserializeObject<List<TestZipkinSpan>>(jsonText);

            Assert.Single(actualTraces);

            var actualSpan = actualTraces[0];
            actualSpan.AssertZipkinSerializerSpan(zipkinSpan);
        }

        [Theory]
        [InlineData("log not to be truncated", "log not to be truncated")]
        [InlineData("log to be truncated", "log to be")]
        [InlineData("log set to empty", "")]
        public void TagTruncation(string original, string expected)
        {
            var traceContext = new TraceContext(Tracer.Instance);
            var spanContext = new SpanContext(null, traceContext, $"{nameof(TagTruncation)}");
            var span = new Span(spanContext, start: null);
            span.SetTag("test", original);

            var settings = new TracerSettings
            {
                RecordedValueMaxLength = expected.Length,
            };
            var zipkinSpan = new ZipkinSpan(span, settings.Build());
            var tags = zipkinSpan.Tags;
            Assert.Single(tags);
            Assert.Equal(expected, tags.First().Value);
        }

        public class TestZipkinSpan
        {
            public string Id { get; set; }

            public string TraceId { get; set; }

            public string ParentId { get; set; }

            public string Name { get; set; }

            public long Timestamp { get; set; }

            public long Duration { get; set; }

            public string Kind { get; set; }

            public Dictionary<string, string> LocalEndpoint { get; set; }

            public IDictionary<string, string> Tags { get; set; }

            internal void AssertZipkinSerializerSpan(ZipkinSerializer.ZipkinSpan span)
            {
                Assert.Equal(Id, span.Id);
                Assert.Equal(TraceId, span.TraceId);
                Assert.Equal(ParentId, span.ParentId);
                Assert.Equal(Name, span.Name);
                Assert.Equal(Timestamp, span.Timestamp);
                Assert.Equal(Duration, span.Duration);
                Assert.Equal(Kind, span.Kind);
                Assert.Equal(LocalEndpoint.Count, span.LocalEndpoint.Count);
                Assert.Contains(
                    LocalEndpoint,
                    kvp => span.LocalEndpoint.TryGetValue(kvp.Key, out var value) && kvp.Value == value);
                Assert.Equal(Tags.Count, span.Tags.Count);
                Assert.Contains(
                    Tags,
                    kvp => span.Tags.TryGetValue(kvp.Key, out var value) && kvp.Value == value);
            }
        }
    }
}
