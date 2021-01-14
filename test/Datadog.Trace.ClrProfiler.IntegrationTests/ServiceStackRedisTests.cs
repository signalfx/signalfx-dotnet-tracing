// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using SignalFx.Tracing.ExtensionMethods;
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

        public static IEnumerable<object[]> TestParameters()
        {
            foreach (var versions in PackageVersions.ServiceStackRedis)
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
            envVars["SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS"] = tagCommands;

            using (var agent = new MockZipkinCollector(agentPort))
            using (var processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"{TestPrefix}", packageVersion: packageVersion, envVars: envVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                // note: ignore the INFO command because it's timing is unpredictable (on Linux?)
                var spans = agent.WaitForSpans(11)
                                 .Where(s => s.Tags.GetValueOrDefault<string>("db.type") == "redis" && s.Name != "INFO")
                                 .OrderBy(s => s.Start)
                                 .ToList();

                var host = Environment.GetEnvironmentVariable("SERVICESTACK_REDIS_HOST") ?? "localhost:6379";
                var port = host.Substring(host.IndexOf(':') + 1);
                host = host.Substring(0, host.IndexOf(':'));

                foreach (var span in spans)
                {
                    Assert.Equal("Samples.ServiceStack.Redis", span.Service);
                    Assert.Equal(SpanTypes.Redis, span.Tags.GetValueOrDefault<string>("db.type"));
                    Assert.Equal("ServiceStack.Redis", span.Tags.GetValueOrDefault<string>("component"));
                    Assert.Equal(host, span.Tags.GetValueOrDefault<string>("peer.hostname"));
                    Assert.Equal(port, span.Tags.GetValueOrDefault<string>("peer.port"));
                    Assert.Equal(SpanKinds.Client, span.Tags.GetValueOrDefault<string>("span.kind"));
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

                    // By default all "db.statement" are sanitized - no worries about truncation because
                    // all tag values here are smaller than the maximum recorded length default.
                    var e2 = expected[i].Item2.SanitizeSqlStatement();

                    var a1 = i < spans.Count
                                 ? spans[i].Name
                                 : string.Empty;
                    var a2 = i < spans.Count
                                 ? spans[i].Tags.GetValueOrDefault<string>("db.statement")
                                 : string.Empty;

                    Assert.True(e1 == a1, $@"invalid resource name for span #{i}, expected ""{e1}"", actual ""{a1}""");
                    if (tagCommands.Equals("true"))
                    {
                        Assert.True(e2 == a2, $@"invalid raw command for span #{i}, expected ""{e2}"" != ""{a2}""");
                    }
                    else
                    {
                        Assert.Null(a2);
                    }
                }
            }
        }
    }
}
