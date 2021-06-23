// Modified by SignalFx
using Datadog.Trace.ClrProfiler.IntegrationTests.Helpers;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AdoNet
{
    public class MySqlCommandTests : TestHelper
    {
        public MySqlCommandTests(ITestOutputHelper output)
            : base("MySql", output)
        {
        }

        // Skipping on net5.0 due to SSL_ERROR_SSL on Alpine, see https://github.com/dotnet/SqlClient/issues/776
        [TargetFrameworkVersionsFact("net452;net461netcoreapp2.1;netcoreapp3.0;netcoreapp3.1")]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces()
        {
            // In .NET Framework, the MySQL client injects
            // a few extra queries the first time it connects to a database
            int expectedSpanCount = EnvironmentHelper.IsCoreClr() ? 21 : 24;
            const string dbType = "mysql";
            const string expectedOperationName = dbType + ".query";
            const string expectedServiceName = "Samples.MySql";

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
