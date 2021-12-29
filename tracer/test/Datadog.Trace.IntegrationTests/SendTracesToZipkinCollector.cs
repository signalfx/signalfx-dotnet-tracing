using System;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.Zipkin;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using Xunit;

namespace Datadog.Trace.IntegrationTests
{
    public class SendTracesToZipkinCollector : IDisposable
    {
        private readonly Tracer _tracer;
        private readonly MockZipkinCollector _zipkinCollector;

        public SendTracesToZipkinCollector()
        {
            int collectorPort = 9411;
            var settings = new TracerSettings();
            settings.ExporterSettings.AgentUri = new Uri($"http://localhost:{collectorPort}/api/v2/spans");
            var exporter = new ZipkinExporter(settings.Build());
            var exporterWriter = new ExporterWriter(exporter, new NullMetrics());
            _tracer = new Tracer(new TracerSettings(), exporterWriter, sampler: null, scopeManager: null, statsd: null);
            _zipkinCollector = new MockZipkinCollector(collectorPort);
        }

        public void Dispose()
        {
            _zipkinCollector.Dispose();
        }

        [Fact]
        public void MinimalSpan()
        {
            const string operationName = "MyOperation";
            using (var scope = _tracer.StartActive(operationName))
            {
                scope.Span.SetTag(Tags.SpanKind, SpanKinds.Client);
                scope.Span.SetTag("key", "value");
            }

            var zspan = RetrieveSpan();

            Assert.Equal(operationName, zspan.Name);
            Assert.True(zspan.Tags.TryGetValue("key", out var tagValue));
            Assert.Equal("value", tagValue);

            // The span.kind has an special treatment.
            Assert.False(zspan.Tags.ContainsKey(Tags.SpanKind));
            Assert.Equal("CLIENT", zspan.Kind);

            Assert.False(zspan.Tags.ContainsKey("error")); // MUST NOT be set if the status code is UNSET.
        }

        [Fact]
        public void OkSpan()
        {
            using (var scope = _tracer.StartActive("Operation"))
            {
                scope.Span.Status = SpanStatus.Ok;
            }

            var zspan = RetrieveSpan();

            Assert.False(zspan.Tags.ContainsKey("error")); // MUST NOT be set if the status code is OK.
        }

        [Fact]
        public void ErrorSpan()
        {
            using (var scope = _tracer.StartActive("Operation"))
            {
                scope.Span.Status = SpanStatus.Error;
            }

            var zspan = RetrieveSpan();

            Assert.True(zspan.Tags.TryGetValue("error", out var tagValue));
            Assert.Equal("true", tagValue);
        }

        private MockZipkinCollector.Span RetrieveSpan()
        {
            var spans = _zipkinCollector.WaitForSpans(1);
            Assert.Single(spans);
            return spans[0];
        }
    }
}
