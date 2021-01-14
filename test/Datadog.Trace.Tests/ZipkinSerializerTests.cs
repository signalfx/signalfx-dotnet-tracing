// Modified by SignalFx
using System.Linq;
using SignalFx.Tracing;
using SignalFx.Tracing.Agent;
using SignalFx.Tracing.Configuration;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class ZipkinSerializerTests
    {
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
            var zipkinSpan = new ZipkinSerializer.ZipkinSpan(span, settings);
            var tags = zipkinSpan.Tags;
            Assert.Single(tags);
            Assert.Equal(expected, tags.First().Value);
        }

        [Theory]
        [InlineData("SELECT * FROM TABLE WHERE FIELD=1234", "SELECT * FROM TABLE WHERE FIELD=?", true)]
        [InlineData("SELECT * FROM TABLE WHERE FIELD=1234", "SELECT * FROM TABLE", true)]
        [InlineData("SELECT * FROM TABLE WHERE FIELD=1234", "", true)]
        [InlineData("SELECT * FROM TABLE WHERE FIELD=1234", "SELECT * FROM TABLE WHERE FIELD=1234", false)]
        [InlineData("SELECT * FROM TABLE WHERE FIELD=1234", "SELECT * FROM TABLE", false)]
        [InlineData("SELECT * FROM TABLE WHERE FIELD=1234", "", false)]
        public void DbStatementSanitationAndTruncation(string original, string expected, bool sanitizeSqlStatements)
        {
            var traceContext = new TraceContext(Tracer.Instance);
            var spanContext = new SpanContext(null, traceContext, $"{nameof(TagTruncation)}");
            var span = new Span(spanContext, start: null);
            span.SetTag("db.statement", original);

            var settings = new TracerSettings
            {
                SanitizeSqlStatements = sanitizeSqlStatements,
                RecordedValueMaxLength = expected.Length,
            };
            var zipkinSpan = new ZipkinSerializer.ZipkinSpan(span, settings);
            var tags = zipkinSpan.Tags;
            Assert.Single(tags);
            Assert.Equal(expected, tags.First().Value);
        }

        [Theory]
        [InlineData("log not to be truncated", "log not to be truncated")]
        [InlineData("log to be truncated", "log to be")]
        [InlineData("log set to empty", "")]
        public void AnnotationTruncation(string original, string expected)
        {
            var traceContext = new TraceContext(Tracer.Instance);
            var spanContext = new SpanContext(null, traceContext, $"{nameof(AnnotationTruncation)}");
            var span = new Span(spanContext, start: null);
            span.Log(original);

            var settings = new TracerSettings
            {
                RecordedValueMaxLength = expected.Length,
            };
            var zipkinSpan = new ZipkinSerializer.ZipkinSpan(span, settings);
            var annotations = zipkinSpan.Annotations;
            Assert.Single(annotations);
            Assert.Equal($"{{\"event\":\"{expected}\"}}", annotations.First()["value"]);
        }
    }
}
