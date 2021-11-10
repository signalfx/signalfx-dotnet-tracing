// <copyright file="WcfTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#if NET461

using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class WcfTests : TestHelper
    {
        private const string ServiceVersion = "1.0.0";

        public WcfTests(ITestOutputHelper output)
            : base("Wcf", output)
        {
            SetServiceVersion(ServiceVersion);
        }

        [SkippableTheory]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [InlineData(false)]
        [InlineData(true)]
        public void SubmitsTraces(bool enableCallTarget)
        {
            SetCallTargetSettings(enableCallTarget);

            Output.WriteLine("Starting WcfTests.SubmitsTraces. Starting the Samples.Wcf requires ADMIN privileges");

            var expectedSpanCount = 4;

            const string expectedLogicScope = "wcf.request";
            const string expectedServiceName = "Samples.Wcf";

            HashSet<string> expectedResourceNames = new HashSet<string>()
            {
                "http://schemas.xmlsoap.org/ws/2005/02/trust/RSTR/Issue",
                "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue",
                "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/SCT",
                "WcfSample/ICalculator/Add"
            };

            int agentPort = TcpPortProvider.GetOpenPort();
            int wcfPort = 8585;

            using (var agent = new MockTracerAgent(agentPort))
            using (RunSampleAndWaitForExit(agent.Port, arguments: $"WSHttpBinding Port={wcfPort}"))
            {
                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedLogicScope);
                Assert.True(spans.Count >= expectedSpanCount, $"Expecting at least {expectedSpanCount} spans, only received {spans.Count}");

                HashSet<string> expectedOperationNames = new HashSet<string>(
                    expectedResourceNames
                        .Select(resourceName => $"{expectedLogicScope} {resourceName}"));

                HashSet<string> actualOperationNames = new HashSet<string>();

                foreach (var span in spans)
                {
                    actualOperationNames.Add(span.Name);

                    // Validate server fields
                    Assert.Equal(expectedServiceName, span.Service);
                    Assert.Equal(ServiceVersion, span.Tags[Tags.Version]);
                    Assert.Equal(SpanTypes.Web, span.Type);
                    Assert.Equal(SpanKinds.Server, span.Tags[Tags.SpanKind]);
                    Assert.Equal("Wcf", span.Tags[Tags.InstrumentationName]);

                    // Validate resource name
                    Assert.Contains(span.Resource, expectedResourceNames);

                    // Test HTTP tags
                    Assert.Equal("POST", span.Tags[Tags.HttpMethod]);
                    Assert.Equal("http://localhost:8585/WcfSample/CalculatorService", span.Tags[Tags.HttpUrl]);

                    // Upstream can use Tags.HttpRequestHeadersHost to retrieve the value, but, the mock agent
                    // transforms the tag key to its original value "http.request.headers.host" in order to
                    // avoid changes to the verification files.
                    Assert.Equal($"localhost:{wcfPort}", span.Tags["http.request.headers.host"]);
                }

                Assert.True(expectedOperationNames.SetEquals(actualOperationNames));
            }
        }
    }
}

#endif
