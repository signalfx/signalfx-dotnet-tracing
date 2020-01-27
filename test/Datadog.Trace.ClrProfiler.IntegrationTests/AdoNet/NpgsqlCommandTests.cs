// Modified by SignalFx
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AdoNet
{
    public class NpgsqlCommandTests : TestHelper
    {
        public NpgsqlCommandTests(ITestOutputHelper output)
            : base("Npgsql", output)
        {
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces()
        {
            // In .NET Framework, the Npgsql client injects
            // a few extra queries the first time it connects to a database
            int expectedSpanCount = EnvironmentHelper.IsCoreClr() ? 21 : 22;
            const string dbType = "postgres";
            const string expectedOperationName = dbType + ".query";
            const string expectedServiceName = "Samples.Npgsql";

            int agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockZipkinCollector(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, envVars: ZipkinEnvVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);
                Assert.Equal(expectedSpanCount, spans.Count);

                foreach (var span in spans)
                {
                    Assert.Equal(expectedOperationName, span.Name);
                    Assert.Equal(expectedServiceName, span.Service);
                    Assert.Null(span.Type);
                    Assert.Equal(dbType, span.Tags[Tags.DbType]);
                    Assert.NotNull(span.Tags[Tags.DbStatement]);
                }
            }
        }
    }
}
