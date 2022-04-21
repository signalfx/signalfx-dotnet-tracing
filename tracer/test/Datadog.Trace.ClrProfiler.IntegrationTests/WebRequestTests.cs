// <copyright file="WebRequestTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.ClrProfiler.IntegrationTests.Helpers;
using Datadog.Trace.Configuration;
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
            SetEnvironmentVariable("SIGNALFX_CONVENTION", "Datadog");
            SetEnvironmentVariable("SIGNALFX_PROPAGATORS", "B3");
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void SubmitsTraces()
        {
            var expectedSpanCount = 76;

            const string expectedOperationName = "http.request";
            const string expectedServiceName = "Samples.WebRequest";

            int httpPort = TcpPortProvider.GetOpenPort();
            Output.WriteLine($"Assigning port {httpPort} for the httpPort.");

            using var telemetry = this.ConfigureTelemetry();
            using (var agent = EnvironmentHelper.GetMockAgent())
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent, arguments: $"Port={httpPort}"))
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

                telemetry.AssertIntegrationEnabled(IntegrationId.WebRequest);
            }
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void TracingDisabled_DoesNotSubmitsTraces()
        {
            const string expectedOperationName = "http.request";

            int httpPort = TcpPortProvider.GetOpenPort();

            using var telemetry = this.ConfigureTelemetry();
            using (var agent = EnvironmentHelper.GetMockAgent())
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent, arguments: $"TracingDisabled Port={httpPort}"))
            {
                var spans = agent.WaitForSpans(1, 3000, operationName: expectedOperationName);
                Assert.Equal(0, spans.Count);

                PropagationTestHelpers.AssertPropagationDisabled(processResult);

                telemetry.AssertIntegrationDisabled(IntegrationId.WebRequest);
            }
        }
    }
}
