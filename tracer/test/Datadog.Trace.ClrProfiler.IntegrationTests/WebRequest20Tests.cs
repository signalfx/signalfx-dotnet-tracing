// <copyright file="WebRequest20Tests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#if NETFRAMEWORK
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.ClrProfiler.IntegrationTests.Helpers;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class WebRequest20Tests : TestHelper
    {
        public WebRequest20Tests(ITestOutputHelper output)
            : base("WebRequest.NetFramework20", output)
        {
            SetServiceVersion("1.0.0");
            SetEnvironmentVariable("SIGNALFX_CONVENTION", "Datadog");
            SetEnvironmentVariable("SIGNALFX_PROPAGATORS", "B3");
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [Trait("SupportsInstrumentationVerification", "True")]
        public void SubmitsTraces()
        {
            int expectedSpanCount = 45;
            const string expectedOperationName = "http.request";
            const string expectedServiceName = "Samples.WebRequest.NetFramework20";

            int httpPort = TcpPortProvider.GetOpenPort();
            Output.WriteLine($"Assigning port {httpPort} for the httpPort.");

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent, arguments: $"Port={httpPort}"))
            {
                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);
                Assert.Equal(expectedSpanCount, spans.Count);

                foreach (var span in spans)
                {
                    var result = span.IsWebRequest();
                    Assert.True(result.Success, result.ToString());

                    Assert.Equal(expectedServiceName, span.Service);
                    Assert.Equal("WebRequest", span.Tags[Tags.InstrumentationName]);
                    Assert.Contains(Tags.Version, (IDictionary<string, string>)span.Tags);
                }

                PropagationTestHelpers.AssertPropagationEnabled(spans.First(), processResult);
            }
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [Trait("SupportsInstrumentationVerification", "True")]
        public void TracingDisabled_DoesNotSubmitsTraces()
        {
            const string expectedOperationName = "http.request";
            SetInstrumentationVerification();
            int httpPort = TcpPortProvider.GetOpenPort();

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent, arguments: $"TracingDisabled Port={httpPort}"))
            {
                var spans = agent.WaitForSpans(1, 3000, operationName: expectedOperationName);
                Assert.Equal(0, spans.Count);

                PropagationTestHelpers.AssertPropagationDisabled(processResult);
                VerifyInstrumentation(processResult.Process);
            }
        }
    }
}
#endif
