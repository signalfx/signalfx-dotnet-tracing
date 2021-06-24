// Modified by SignalFx
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class KafkaTests : TestHelper
    {
        public KafkaTests(ITestOutputHelper output)
            : base("Kafka", output)
        {
        }

        [Theory]
        [MemberData(nameof(PackageVersions.Kafka), MemberType = typeof(PackageVersions))]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces(string packageVersion)
        {
            int agentPort = TcpPortProvider.GetOpenPort();
            var envVars = ZipkinEnvVars;
            using (var agent = new MockZipkinCollector(agentPort))
            using (var processResult = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion, envVars: envVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");

                var spans = agent.WaitForSpans(1, 500);
                Assert.True(spans.Count >= 1, $"Expecting at least 1 span, only received {spans.Count}");
            }
        }
    }
}
