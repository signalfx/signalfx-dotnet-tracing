// Modified by SignalFx
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using Datadog.Core.Tools;
using Datadog.Trace.ClrProfiler.IntegrationTests.Helpers;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class WebRequestTests : TestHelper
    {
        public WebRequestTests(ITestOutputHelper output)
            : base("WebRequest", output)
        {
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void SubmitsTraces()
        {
            int expectedSpanCount = EnvironmentHelper.IsCoreClr() ? 71 : 27; // .NET Framework automatic instrumentation doesn't cover Async / TaskAsync operations
            const string expectedServiceName = "Samples.WebRequest";
            var expectedSpanNamePrefixes = new string[] { "GET", "POST" };

            int agentPort = TcpPortProvider.GetOpenPort();
            int httpPort = TcpPortProvider.GetOpenPort();

            Output.WriteLine($"Assigning port {agentPort} for the agentPort.");
            Output.WriteLine($"Assigning port {httpPort} for the httpPort.");

            using (var agent = new MockZipkinCollector(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"Port={httpPort}"))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(expectedSpanCount, operationNameContainsAny: expectedSpanNamePrefixes);
                Assert.Equal(expectedSpanCount, spans.Count);

                foreach (var span in spans)
                {
#pragma warning disable xUnit2012 // Do not use Enumerable.Any() to check if a value exists in a collection
                    Assert.True(expectedSpanNamePrefixes.Any(prefix => span.Name.StartsWith(prefix)));
#pragma warning restore xUnit2012 // Do not use Enumerable.Any() to check if a value exists in a collection
                    Assert.Equal(expectedServiceName, span.Service);
                    Assert.Null(span.Type);
                    Assert.True(span.Tags[Tags.InstrumentationName].StartsWith("WebRequest") || string.Equals(span.Tags[Tags.InstrumentationName], "HttpMessageHandler"));
                    Assert.False(span.Tags?.ContainsKey(Tags.Version), "External service span should not have service version tag.");
                }

                var firstSpan = spans.First();
                var traceId = StringUtil.GetHeader(processResult.StandardOutput, HttpHeaderNames.B3TraceId);
                var parentSpanId = StringUtil.GetHeader(processResult.StandardOutput, HttpHeaderNames.B3SpanId);

                Assert.Equal(firstSpan.TraceId.ToString("x16"), traceId);
                Assert.Equal(firstSpan.SpanId.ToString("x16"), parentSpanId);
            }
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void TracingDisabled_DoesNotSubmitsTraces()
        {
            const string expectedOperationName = "http.request";

            int agentPort = TcpPortProvider.GetOpenPort();
            int httpPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockZipkinCollector(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"TracingDisabled Port={httpPort}"))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(1, 3000, operationName: expectedOperationName);
                Assert.Equal(0, spans.Count);

                var traceId = StringUtil.GetHeader(processResult.StandardOutput, HttpHeaderNames.B3TraceId);
                var parentSpanId = StringUtil.GetHeader(processResult.StandardOutput, HttpHeaderNames.B3ParentId);
                var tracingEnabled = StringUtil.GetHeader(processResult.StandardOutput, HttpHeaderNames.TracingEnabled);

                Assert.Null(traceId);
                Assert.Null(parentSpanId);
                Assert.Equal("false", tracingEnabled);
            }
        }
    }
}
