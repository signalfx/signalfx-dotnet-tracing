// <copyright file="MongoDbTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

using static Datadog.Trace.ExtensionMethods.ArrayExtensions;

#if NETFRAMEWORK
using static Datadog.Trace.ExtensionMethods.DictionaryExtensions;
#endif

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [Trait("RequiresDockerDependency", "true")]
    [UsesVerify]
    public class MongoDbTests : TestHelper
    {
        private static readonly Regex OsRegex = new(@"""os"" : \{.*?\} ");
        private static readonly Regex ObjectIdRegex = new(@"ObjectId\("".*?""\)");

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
        public async Task SubmitsTraces(string packageVersion, bool tagCommands)
        {
            SetEnvironmentVariable("SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS", tagCommands.ToString().ToLowerInvariant());

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

                var version = string.IsNullOrEmpty(packageVersion) ? null : new Version(packageVersion);
                var snapshotSuffix = version switch
                {
                    null => "2_7", // default is version 2.8.0
                    { Major: >= 3 } or { Major: 2, Minor: >= 15 } => "2_15", // A bunch of stuff was removed in 2.15.0
                    { Major: 2, Minor: >= 7 } => "2_7", // default is version 2.8.0
                    { Major: 2, Minor: >= 5 } => "2_5", // version 2.5 + 2.6 include additional info on queries compared to 2.2
                    { Major: 2, Minor: >= 2 } => "2_2",
                    _ => "PRE_2_2"
                };

                var settings = VerifyHelper.GetSpanVerifierSettings();
                // mongo stamps the current framework version, and OS so normalise those
                settings.AddRegexScrubber(OsRegex, @"""os"" : {} ");
                // v2.5.x records additional info in the insert query which is execution-specific
                settings.AddRegexScrubber(ObjectIdRegex, @"ObjectId(""ABC123"")");
                // normalise between running directly against localhost and against mongo container
                settings.AddSimpleScrubber("net.peer.name: localhost", "net.peer.name: mongo");
                settings.AddSimpleScrubber("net.peer.name: mongo_arm64", "net.peer.name: mongo");
                // In some package versions, aggregate queries have an ID, others don't
                settings.AddSimpleScrubber("\"$group\" : { \"_id\" : null, \"n\"", "\"$group\" : { \"_id\" : 1, \"n\"");

                // The mongodb driver sends periodic monitors
                var adminSpans = spans
                                .Where(x => x.Resource is "buildInfo admin" or "getLastError admin")
                                .ToList();
                var nonAdminSpans = spans
                                   .Where(x => !adminSpans.Contains(x))
                                   .ToList();
                var allMongoSpans = spans
                                    .Where(x => x.GetTag(Tags.InstrumentationName) == "MongoDb")
                                    .ToList();

                await VerifyHelper.VerifySpans(nonAdminSpans, settings)
                                  .UseTextForParameters($"packageVersion={snapshotSuffix}_tagCommands={tagCommands}")
                                  .DisableRequireUniquePrefix();

                foreach (var span in allMongoSpans)
                {
                    var result = span.IsMongoDB();
                    Assert.True(result.Success, result.ToString());
                }

                // do some basic verification on the "admin" spans
                using var scope = new AssertionScope();
                adminSpans.Should().AllBeEquivalentTo(new { Service = "Samples.MongoDB", Type = "mongodb", });
                foreach (var adminSpan in adminSpans)
                {
                    adminSpan.Tags.Should().IntersectWith(new Dictionary<string, string>
                    {
                        { "db.name", "admin" },
                        { "deployment.environment", "integration_tests" },
                        { "mongodb.collection", "1" },
                    });

                    if (adminSpan.Resource == "buildInfo admin")
                    {
                        adminSpan.Tags.Should().Contain("mongodb.query", "{ \"buildInfo\" : 1 }");
                    }
                    else
                    {
                        adminSpan.Tags.Should().Contain("mongodb.query", "{ \"getLastError\" : 1 }");
                    }
                }
            }
        }
    }
}
