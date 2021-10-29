// <copyright file="MongoDbTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

using static Datadog.Trace.ExtensionMethods.ArrayExtensions;

#if NETFRAMEWORK
using static Datadog.Trace.ExtensionMethods.DictionaryExtensions;
#endif

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class MongoDbTests : TestHelper
    {
        public MongoDbTests(ITestOutputHelper output)
            : base("MongoDB", output)
        {
            SetServiceVersion("1.0.0");
        }

        public static IEnumerable<object[]> GetMongoDb()
        {
            foreach (var item in PackageVersions.MongoDB)
            {
                yield return item.Concat(false);
                yield return item.Concat(true);
            }
        }

        [SkippableTheory]
        [MemberData(nameof(GetMongoDb))]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces(string packageVersion, bool tagCommands)
        {
            SetEnvironmentVariable("SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS", tagCommands.ToString().ToLowerInvariant());

            int agentPort = TcpPortProvider.GetOpenPort();
            using (var agent = new MockTracerAgent(agentPort))
            using (RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion))
            {
                var manualNames = new HashSet<string>() { "sync-calls", "sync-calls-execute", "async-calls", "async-calls-execute" };
                var mongoNames = new HashSet<string>() { "aggregate", "buildInfo", "delete", "find", "getLastError", "insert", "mongodb.query" };
                var expectedNames = new HashSet<string>();
                expectedNames.UnionWith(manualNames);
                expectedNames.UnionWith(mongoNames);

                var spans = agent.WaitForSpans(expectedNames.Count, 500);
                Assert.True(spans.Count >= expectedNames.Count, $"Expecting at least {expectedNames.Count} spans, only received {spans.Count}");

                var rootSpan = spans.Single(s => s.ParentId == null);

                // Check for manual trace
                Assert.Equal("Main()", rootSpan.Name);
                Assert.Equal("Samples.MongoDB", rootSpan.Service);
                Assert.Null(rootSpan.Type);

                var foundNames = new HashSet<string>();

                foreach (var span in spans)
                {
                    if (span == rootSpan)
                    {
                        continue;
                    }

                    var name = span.Name;
                    foundNames.Add(name);

                    if (span.Service == "Samples.MongoDB" &&
                        span.LogicScope == "mongodb.query")
                    {
                        Assert.Equal(SpanTypes.MongoDb, span.Type);
                        Assert.Equal(SpanTypes.MongoDb, span.Tags.GetValueOrDefault(Tags.DbType));
                        Assert.Equal("MongoDb", span.Tags.GetValueOrDefault("component"));

                        span.Tags.TryGetValue(Tags.DbStatement, out string statement);

                        if (tagCommands && !name.Equals("mongodb.query"))
                        {
                            Assert.NotNull(statement);
                        }
                        else
                        {
                            Assert.Null(statement);
                        }

                        if (!name.Equals("mongodb.query"))
                        {
                            Assert.NotNull(span.Tags.GetValueOrDefault(Tags.DbName));
                        }
                    }
                    else
                    {
                        // These are manual traces
                        Assert.Equal("Samples.MongoDB", span.Service);
                        Assert.True("1.0.0" == span.Tags?.GetValueOrDefault(Tags.Version), span.ToString());
                    }
                }

                Assert.True(expectedNames.SetEquals(foundNames));
            }
        }
    }
}
