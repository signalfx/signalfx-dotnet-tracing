// <copyright file="MongoDbTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
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

            using var telemetry = this.ConfigureTelemetry();
            using (var agent = EnvironmentHelper.GetMockAgent())
            using (RunSampleAndWaitForExit(agent, packageVersion: packageVersion))
            {
                var manualNames = new HashSet<string>() { "sync-calls", "sync-calls-execute", "async-calls", "async-calls-execute" };
                var mongoNames = new HashSet<string>() { "aggregate", "delete", "find", "insert", "mongodb.query" };
                var expectedNames = new HashSet<string>();
                expectedNames.UnionWith(manualNames);
                expectedNames.UnionWith(mongoNames);

                var spans = agent.WaitForSpans(expectedNames.Count, 500);
                spans.Count.Should().BeGreaterOrEqualTo(expectedNames.Count);

                var rootSpan = spans.Single(s => s.ParentId == null);

                // Check for manual trace
                rootSpan.Name.Should().Be("Main()");
                rootSpan.Service.Should().Be("Samples.MongoDB");
                rootSpan.Type.Should().BeNull();

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
                        span.Type.Should().Be(SpanTypes.MongoDb);
                        span.Tags.GetValueOrDefault(Tags.DbType).Should().Be(SpanTypes.MongoDb);
                        span.Tags.GetValueOrDefault("component").Should().Be("MongoDb");

                        span.Tags.TryGetValue(Tags.DbStatement, out string statement);

                        if (tagCommands && !name.Equals("mongodb.query"))
                        {
                            statement.Should().NotBeNull();
                        }
                        else
                        {
                            statement.Should().BeNull();
                        }

                        if (!name.Equals("mongodb.query"))
                        {
                            span.Tags.GetValueOrDefault(Tags.DbName).Should().NotBeNull();
                        }
                    }
                    else
                    {
                        // These are manual traces
                        span.Service.Should().Be("Samples.MongoDB");
                        span.Tags?.GetValueOrDefault(Tags.Version).Should().Be("1.0.0");
                    }
                }

                foundNames.Should().BeEquivalentTo(expectedNames);
                telemetry.AssertIntegrationEnabled(IntegrationId.MongoDb);
            }
        }
    }
}
