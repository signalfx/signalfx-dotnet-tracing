// Modified by SignalFx
#if !NET452
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AdoNet
{
    public class DapperCommandTests : TestHelper
    {
        public DapperCommandTests(ITestOutputHelper output)
            : base("Dapper", output)
        {
        }

        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces(string sanitizeStatements)
        {
            var expectedSpanCount = EnvironmentHelper.IsCoreClr() ? 4 : 7;
            const string dbType = "postgres";
            const string expectedOperationName = dbType + ".query";
            const string expectedServiceName = "Samples.Dapper";

            int agentPort = TcpPortProvider.GetOpenPort();
            var envVars = ZipkinEnvVars;
            envVars["SIGNALFX_SANITIZE_SQL_STATEMENTS"] = sanitizeStatements;

            using (var agent = new MockZipkinCollector(agentPort))
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent.Port, envVars: envVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);
                Assert.Equal(expectedSpanCount, spans.Count);

                foreach (var span in spans)
                {
                    Assert.Equal(expectedOperationName, span.Name);
                    Assert.Equal(expectedServiceName, span.Service);
                    Assert.Equal(dbType, span.Tags[Tags.DbType]);
                    Assert.Null(span.Type);
                    var statement = span.Tags[Tags.DbStatement];
                    Assert.NotNull(statement);
                    if (sanitizeStatements.Equals("true"))
                    {
                        Assert.DoesNotContain(statement, "Id=1");
                        Assert.DoesNotContain(statement, "pg_proc.proname='array_recv'");
                        Assert.True(statement.Contains("Id=?") || statement.Contains("pg_proc.proname=?"));
                    }
                    else
                    {
                        Assert.DoesNotContain(statement, "Id=?");
                        Assert.DoesNotContain(statement, "pg_proc.proname=?");
                        Assert.True(statement.Contains("Id=1") || statement.Contains("pg_proc.proname='array_recv'"));
                    }
                }
            }
        }
    }
}
#endif
