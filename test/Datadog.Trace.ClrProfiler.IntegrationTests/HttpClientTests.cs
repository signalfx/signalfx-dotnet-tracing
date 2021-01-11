// Modified by SignalFx
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class HttpClientTests : TestHelper
    {
        public HttpClientTests(ITestOutputHelper output)
            : base("HttpMessageHandler", output)
        {
            SetEnvironmentVariable("SIGNALFX_TRACE_DOMAIN_NEUTRAL_INSTRUMENTATION", "true");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void HttpClient(bool appendPathToName)
        {
            int expectedSpanCount = EnvironmentHelper.IsCoreClr() ? 2 : 1;
            const string expectedServiceName = "Samples.HttpMessageHandler";
            var expectedOperationName = appendPathToName
                ? "POST:/Samples.HttpMessageHandler/"
                : "POST";

            int agentPort = TcpPortProvider.GetOpenPort();
            int httpPort = TcpPortProvider.GetOpenPort();

            Output.WriteLine($"Assigning port {agentPort} for the agentPort.");
            Output.WriteLine($"Assigning port {httpPort} for the httpPort.");

            var envVars = ZipkinEnvVars;
            if (appendPathToName)
            {
                envVars["SIGNALFX_APPEND_URL_PATH_TO_NAME"] = "true";
            }

            using (var agent = new MockZipkinCollector(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"HttpClient Port={httpPort}", envVars: envVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);
                Assert.True(spans.Count >= expectedSpanCount, $"Expected at least {expectedSpanCount} span, only received {spans.Count}" + System.Environment.NewLine + "IMPORTANT: Make sure SignalFx.Tracing.ClrProfiler.Managed.dll and its dependencies are in the GAC.");

                foreach (var span in spans)
                {
                    Assert.Equal(expectedOperationName, span.Name);
                    Assert.Equal(expectedServiceName, span.Service);
                    Assert.Null(span.Type);
                    Assert.Equal(nameof(HttpMessageHandler), span.Tags[Tags.InstrumentationName]);
                }

                var firstSpan = spans.First();
                var traceId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3TraceId);
                var parentSpanId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3SpanId);

                Assert.Equal(firstSpan.TraceId.ToString("x16", CultureInfo.InvariantCulture), traceId);
                Assert.Equal(firstSpan.SpanId.ToString("x16", CultureInfo.InvariantCulture), parentSpanId);
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
                Assert.True(spans.Count > 0, "expected at least one span." + System.Environment.NewLine + "IMPORTANT: Make sure SignalFx.Tracing.ClrProfiler.Managed.dll and its dependencies are in the GAC.");

                var traceId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3TraceId);
                var parentSpanId = GetHeader(processResult.StandardOutput, HttpHeaderNames.B3SpanId);

                // inspect the top-level span, underlying spans can be HttpMessageHandler in .NET Core
                var firstSpan = spans.First();
                Assert.Equal("GET", firstSpan.Name);
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
