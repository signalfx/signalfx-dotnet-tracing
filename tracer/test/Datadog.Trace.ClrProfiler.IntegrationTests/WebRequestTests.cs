// <copyright file="WebRequestTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.ClrProfiler.IntegrationTests.Helpers;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [CollectionDefinition(nameof(WebRequestTests), DisableParallelization = true)]
    [Collection(nameof(WebRequestTests))]
    public class WebRequestTests : TestHelper
    {
        public WebRequestTests(ITestOutputHelper output)
            : base("WebRequest", output)
        {
            SetServiceVersion("1.0.0");
            SetEnvironmentVariable("SIGNALFX_PROPAGATORS", "datadog,b3");
        }

        [SkippableTheory]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [InlineData(false)]
        [InlineData(true)]
        public void SubmitsTraces(bool enableCallTarget)
        {
            SetCallTargetSettings(enableCallTarget);

            var (ignoreAsync, expectedSpanCount) = (EnvironmentHelper.IsCoreClr(), enableCallTarget) switch
            {
                (false, false) => (true, 30), // .NET Framework CallSite instrumentation doesn't cover Async / TaskAsync operations
                _ => (false, 76)
            };

            const string expectedOperationName = "http.request";
            const string expectedServiceName = "Samples.WebRequest";

            int agentPort = TcpPortProvider.GetOpenPort();
            int httpPort = TcpPortProvider.GetOpenPort();
            var extraArgs = ignoreAsync ? "IgnoreAsync " : string.Empty;

            Output.WriteLine($"Assigning port {agentPort} for the agentPort.");
            Output.WriteLine($"Assigning port {httpPort} for the httpPort.");

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"{extraArgs}Port={httpPort}"))
            {
                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName).OrderBy(s => s.Start);
                spans.Should().HaveCount(expectedSpanCount);

                foreach (var span in spans)
                {
                    // Unlike upstream the span name is expected to match the "http.method" tag.
                    Assert.Equal(span.Tags["http.method"], span.Name);
                    Assert.Equal(expectedOperationName, span.LogicScope);
                    Assert.Equal(expectedServiceName, span.Service);
                    Assert.Equal(SpanTypes.Http, span.Type);
                    Assert.True(string.Equals(span.Tags[Tags.InstrumentationName], "WebRequest") || string.Equals(span.Tags[Tags.InstrumentationName], "HttpMessageHandler"));
                    Assert.Contains(Tags.Version, (IDictionary<string, string>)span.Tags);
                }

                PropagationTestHelpers.AssertPropagationEnabled(spans.First(), processResult);
            }
        }

        [SkippableTheory]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [InlineData(false)]
        [InlineData(true)]
        public void TracingDisabled_DoesNotSubmitsTraces(bool enableCallTarget)
        {
            SetCallTargetSettings(enableCallTarget);

            const string expectedOperationName = "http.request";

            int agentPort = TcpPortProvider.GetOpenPort();
            int httpPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockTracerAgent(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"TracingDisabled Port={httpPort}"))
            {
                var spans = agent.WaitForSpans(1, 3000, operationName: expectedOperationName);
                Assert.Equal(0, spans.Count);

                PropagationTestHelpers.AssertPropagationDisabled(processResult);
            }
        }
    }
}
