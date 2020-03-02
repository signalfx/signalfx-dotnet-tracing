// Modified by SignalFx
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class HttpClientTests : TestHelper
    {
        public HttpClientTests(ITestOutputHelper output)
            : base("HttpMessageHandler", output)
        {
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void HttpClient()
        {
            int agentPort = TcpPortProvider.GetOpenPort();
            int httpPort = TcpPortProvider.GetOpenPort();

            Output.WriteLine($"Assigning port {agentPort} for the agentPort.");
            Output.WriteLine($"Assigning port {httpPort} for the httpPort.");

            using (var agent = new MockZipkinCollector(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"HttpClient Port={httpPort}", envVars: ZipkinEnvVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(1);
                Assert.True(spans.Count > 0, "expected at least one span");

                var traceId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3TraceId);
                var parentSpanId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3SpanId);

                var firstSpan = spans.First();
                Assert.Equal("http.request", firstSpan.Name);
                Assert.Equal("Samples.HttpMessageHandler", firstSpan.Service);
                Assert.Null(firstSpan.Type);
                Assert.Equal(nameof(HttpMessageHandler), firstSpan.Tags[Tags.InstrumentationName]);

                var lastSpan = spans.Last();
                Assert.Equal(lastSpan.TraceId.ToString("x16", CultureInfo.InvariantCulture), traceId);
                Assert.Equal(lastSpan.SpanId.ToString("x16", CultureInfo.InvariantCulture), parentSpanId);
            }
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void HttpClient_TracingDisabled()
        {
            int agentPort = TcpPortProvider.GetOpenPort();
            int httpPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockZipkinCollector(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"HttpClient TracingDisabled Port={httpPort}", envVars: ZipkinEnvVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(1, 500);
                Assert.Equal(0, spans.Count);

                var traceId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3TraceId);
                var parentSpanId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3SpanId);
                var tracingEnabled = GetHeader(processResult.StandardOutput, HttpHeaderNames.TracingEnabled);

                Assert.Null(traceId);
                Assert.Null(parentSpanId);
                Assert.Equal("false", tracingEnabled);
            }
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void WebClient()
        {
            int agentPort = TcpPortProvider.GetOpenPort();
            int httpPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockZipkinCollector(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"WebClient Port={httpPort}", envVars: ZipkinEnvVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(1);
                Assert.True(spans.Count > 0, "expected at least one span");

                var traceId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3TraceId);
                var parentSpanId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3SpanId);

                // inspect the top-level span, underlying spans can be HttpMessageHandler in .NET Core
                var firstSpan = spans.First();
                Assert.Equal("http.request", firstSpan.Name);
                Assert.Equal("Samples.HttpMessageHandler", firstSpan.Service);
                Assert.Null(firstSpan.Type);
                Assert.Equal(nameof(WebRequest), firstSpan.Tags[Tags.InstrumentationName]);

                var lastSpan = spans.Last();
                Assert.Equal(lastSpan.TraceId.ToString("x16", CultureInfo.InvariantCulture), traceId);
                Assert.Equal(lastSpan.SpanId.ToString("x16", CultureInfo.InvariantCulture), parentSpanId);
            }
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void WebClient_TracingDisabled()
        {
            int agentPort = TcpPortProvider.GetOpenPort();
            int httpPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockZipkinCollector(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"WebClient TracingDisabled Port={httpPort}", envVars: ZipkinEnvVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(1, 500);
                Assert.Equal(0, spans.Count);

                var traceId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3TraceId);
                var parentSpanId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3SpanId);
                var tracingEnabled = GetHeader(processResult.StandardOutput, HttpHeaderNames.TracingEnabled);

                Assert.Null(traceId);
                Assert.Null(parentSpanId);
                Assert.Equal("false", tracingEnabled);
            }
        }

        private string GetHeader(string stdout, string name)
        {
            var pattern = $@"^\[HttpListener\] request header: {name}=(\w+)\r?$";
            var match = Regex.Match(stdout, pattern, RegexOptions.Multiline);

            return match.Success
                       ? match.Groups[1].Value
                       : null;
        }
    }
}
