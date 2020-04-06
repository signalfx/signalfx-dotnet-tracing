// Modified by SignalFx
using System;
using System.Linq;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class ServiceStackRedisTests : TestHelper
    {
        public ServiceStackRedisTests(ITestOutputHelper output)
            : base("ServiceStack.Redis", output)
        {
        }

        [Theory]
        [MemberData(nameof(PackageVersions.ServiceStackRedis), MemberType = typeof(PackageVersions))]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces(string packageVersion)
        {
            int agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockZipkinCollector(agentPort))
            using (var processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"{TestPrefix}", packageVersion: packageVersion, envVars: ZipkinEnvVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                // note: ignore the INFO command because it's timing is unpredictable (on Linux?)
                var spans = agent.WaitForSpans(11)
                                 .Where(s => s.Tags.GetValueOrDefault("db.type") == "redis" && s.Name != "INFO")
                                 .OrderBy(s => s.Start)
                                 .ToList();

                var host = Environment.GetEnvironmentVariable("SERVICESTACK_REDIS_HOST") ?? "localhost:6379";
                var port = host.Substring(host.IndexOf(':') + 1);
                host = host.Substring(0, host.IndexOf(':'));

                foreach (var span in spans)
                {
                    Assert.Equal("Samples.ServiceStack.Redis", span.Service);
                    Assert.Equal(SpanTypes.Redis, span.Tags.GetValueOrDefault<string>(Tags.DbType));
                    Assert.Equal("ServiceStack.Redis", span.Tags.GetValueOrDefault<string>("component"));
                    Assert.Equal(host, span.Tags.GetValueOrDefault("peer.hostname"));
                    Assert.Equal(port, span.Tags.GetValueOrDefault("peer.port"));
                }

                var expected = new TupleList<string, string>
                {
                    { "ROLE", "ROLE" },
                    { "SET", $"SET {TestPrefix}ServiceStack.Redis.INCR 0" },
                    { "PING", "PING" },
                    { "DDCUSTOM", "DDCUSTOM COMMAND" },
                    { "ECHO", "ECHO Hello World" },
                    { "SLOWLOG", "SLOWLOG GET 5" },
                    { "INCR", $"INCR {TestPrefix}ServiceStack.Redis.INCR" },
                    { "INCRBYFLOAT", $"INCRBYFLOAT {TestPrefix}ServiceStack.Redis.INCR 1.25" },
                    { "TIME", "TIME" },
                    { "SELECT", "SELECT 0" },
                };

                for (int i = 0; i < expected.Count; i++)
                {
                    var e1 = expected[i].Item1;
                    var e2 = expected[i].Item2;

                    var a1 = i < spans.Count
                                 ? spans[i].Name
                                 : string.Empty;
                    var a2 = i < spans.Count
                                 ? spans[i].Tags.GetValueOrDefault("db.statement")
                                 : string.Empty;

                    Assert.True(e1 == a1, $@"invalid resource name for span #{i}, expected ""{e1}"", actual ""{a1}""");
                    Assert.True(e2 == a2, $@"invalid raw command for span #{i}, expected ""{e2}"" != ""{a2}""");
                }
            }
        }
    }
}
