// Modified by SignalFx
using System;
using System.Collections;
using System.Data;
using System.Linq;
using Datadog.Trace.ExtensionMethods;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.ExtensionMethods
{
    public class SpanExtensionsTests
    {
        private string statement = string.Concat(Enumerable.Repeat("SELECT * FROM TABLE123 WHERE Field='123' ", 1000));

        [Fact]
        public void AddTagsFromDbCommandWithoutStatement()
        {
            var traceContext = new TraceContext(Tracer.Instance);
            var spanContext = new SpanContext(null, traceContext, "UsesCommandText");
            var span = new Span(spanContext, null);
            var command = new Mock<IDbCommand>();
            command.Setup(cmd => cmd.Connection.ConnectionString).Returns("Provider=Some.Provider.1.0;Data Source=Source.mdb");
            command.Setup(cmd => cmd.CommandText).Returns(statement);
            span.AddTagsFromDbCommand(command.Object);

            // Known length of truncated, then sanitized statement
            Assert.Equal(924, span.Tags["db.statement"].Length);
            Assert.Contains("Field=?", span.Tags["db.statement"]);
            Assert.DoesNotContain("Field='123'", span.Tags["db.statement"]);
        }

        [Fact]
        public void AddTagsFromDbCommandWithStatement()
        {
            var traceContext = new TraceContext(Tracer.Instance);
            var spanContext = new SpanContext(null, traceContext, "UsesStatement");
            var span = new Span(spanContext, null);
            var command = new Mock<IDbCommand>();
            command.Setup(cmd => cmd.CommandText).Returns("Undesired Command Text");
            command.Setup(cmd => cmd.Connection.ConnectionString).Returns("Provider=Some.Provider.1.0;Data Source=Source.mdb");
            span.AddTagsFromDbCommand(command.Object, statement);

            Assert.Equal(statement.Length, span.Tags["db.statement"].Length);
            Assert.DoesNotContain("Field=?", span.Tags["db.statement"]);
            Assert.Contains("Field='123'", span.Tags["db.statement"]);
        }
    }
}
