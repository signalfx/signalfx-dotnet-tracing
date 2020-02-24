// Modified by SignalFx
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class MongoDbTests : TestHelper
    {
        public MongoDbTests(ITestOutputHelper output)
            : base("MongoDB", output)
        {
        }

        public static IEnumerable<object[]> TestParameters()
        {
            foreach (var versions in PackageVersions.MongoDB)
            {
                foreach (var version in versions)
                {
                    yield return new object[] { version, "true" };
                    yield return new object[] { version, "false" };
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestParameters))]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces(string packageVersion, string tagCommands)
        {
            int agentPort = TcpPortProvider.GetOpenPort();
            var envVars = ZipkinEnvVars;
            envVars["SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS"] = tagCommands;

            using (var agent = new MockZipkinCollector(agentPort))
            using (var processResult = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion, envVars: envVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");

                var spans = agent.WaitForSpans(4, 500);
                Assert.True(spans.Count >= 4, $"Expecting at least 4 spans, only received {spans.Count}");

                var firstSpan = spans[0];
                // Check for manual trace
                Assert.Equal("Main()", firstSpan.Name);
                Assert.Equal("Samples.MongoDB", firstSpan.Service);
                Assert.Null(firstSpan.Type);

                var manualNames = new List<string>() { "sync-calls", "async-calls" };
                var mongoNames = new List<string>() { "aggregate", "buildInfo", "delete", "find", "getLastError", "insert", "isMaster" };
                var expectedNames = manualNames.Concat(mongoNames);

                for (int i = 1; i < spans.Count; i++)
                {
                    var span = spans[i];
                    var name = span.Name;
                    Assert.Contains(name, expectedNames);
                    if (mongoNames.Contains(name))
                    {
                        Assert.Equal("MongoDb", span.Tags["component"]);
                        span.Tags.TryGetValue(Tags.DbStatement, out string statement);
                        if (tagCommands.Equals("true"))
                        {
                            Assert.NotNull(statement);
                        }
                        else
                        {
                            Assert.Null(statement);
                        }

                        Assert.NotNull(span.Tags[Tags.DbName]);
                    }
                }
            }
        }
    }
}
