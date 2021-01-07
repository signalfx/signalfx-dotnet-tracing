// Modified by SignalFx
using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Moq;
using SignalFx.Tracing;
using SignalFx.Tracing.ExtensionMethods;
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

            Assert.Same(statement, span.Tags["db.statement"]);
            Assert.Contains("Field='123'", span.Tags["db.statement"]);
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

        [Theory]
        [InlineData(null)]
        [InlineData("172.68.32.1")]
        [InlineData("2001:db8::2:1")]
        public void DecorateWebServerSpan(string remoteAddress)
        {
            var traceContext = new TraceContext(Tracer.Instance);
            var spanContext = new SpanContext(null, traceContext, "WebServer");
            var span = new Span(spanContext, null);

            IPAddress.TryParse(remoteAddress, out var remoteIp);
            span.DecorateWebServerSpan("resourceName", "METHOD", "host", "httpUrl", remoteIp);

            Assert.Equal("WebServer", span.ServiceName);
            Assert.Equal("resourceName", span.ResourceName);
            Assert.Equal("resourceName", span.OperationName);
            Assert.Equal("server", span.Tags["span.kind"]);
            Assert.Equal("METHOD", span.Tags["http.method"]);
            Assert.Equal("host", span.Tags["http.request.headers.host"]);
            Assert.Equal("httpUrl", span.Tags["http.url"]);

            if (remoteIp != null)
            {
                var peerTag = remoteIp.AddressFamily == AddressFamily.InterNetworkV6
                    ? "peer.ipv6"
                    : "peer.ipv4";
                Assert.Equal(remoteAddress, span.Tags[peerTag]);
            }
        }
    }
}
