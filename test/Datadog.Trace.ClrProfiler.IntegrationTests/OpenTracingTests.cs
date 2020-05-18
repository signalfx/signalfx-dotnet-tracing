// Modified by SignalFx
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class OpenTracingTests : TestHelper
    {
        public OpenTracingTests(ITestOutputHelper output)
            : base("OpenTracing", output)
        {
        }

        [Theory]
        [MemberData(nameof(PackageVersions.OpenTracing), MemberType = typeof(PackageVersions))]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces(string packageVersion)
        {
            int agentPort = TcpPortProvider.GetOpenPort();
            using (var agent = new MockZipkinCollector(agentPort))
            using (var processResult = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion, envVars: ZipkinEnvVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");

                var spans = agent.WaitForSpans(1, 500);
                Assert.True(spans.Count >= 1, $"Expecting at least 1 spans, only received {spans.Count}");

                var span = (MockZipkinCollector.Span)spans[0];
                Assert.Equal("MySpan", span.Name);
                Assert.Equal("Samples.OpenTracing", span.Service);
                Assert.Null(span.Type);

                span.Tags.TryGetValue("MyTag", out string spanValue);
                Assert.Equal("MyValue", spanValue);

                var logs = span.Logs.Values;
                Assert.Single(logs);
                Assert.Equal("My Log Statement", logs.First()["event"]);
            }
        }
    }
}
