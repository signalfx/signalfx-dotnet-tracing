// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using SignalFx.Tracing.ExtensionMethods;
using Xunit;
using Xunit.Abstractions;

#if !NET452

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class Elasticsearch5Tests : TestHelper
    {
        public Elasticsearch5Tests(ITestOutputHelper output)
            : base("Elasticsearch.V5", output)
        {
        }

        public static IEnumerable<object[]> TestParameters()
        {
            foreach (var versions in PackageVersions.ElasticSearch5)
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
        public void SubmitsTraces(string packageVersion, string tagQueries)
        {
            int agentPort = TcpPortProvider.GetOpenPort();
            var envVars = ZipkinEnvVars;
            envVars["SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES"] = tagQueries;

            using (var agent = new MockZipkinCollector(agentPort))
            using (var processResult = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion, envVars: envVars))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

                var expected = new List<string>();

                // commands with sync and async
                for (var i = 0; i < 2; i++)
                {
                    expected.AddRange(new List<string>
                    {
                        "Bulk",
                        "Create",
                        "Search",
                        "DeleteByQuery",

                        "CreateIndex",
                        "IndexExists",
                        "UpdateIndexSettings",
                        "BulkAlias",
                        "GetAlias",
                        "PutAlias",
                        // "AliasExists",
                        "DeleteAlias",
                        "DeleteAlias",
                        "CreateIndex",
                        // "SplitIndex",
                        "DeleteIndex",
                        "CloseIndex",
                        "OpenIndex",
                        "PutIndexTemplate",
                        "IndexTemplateExists",
                        "DeleteIndexTemplate",
                        "IndicesShardStores",
                        "IndicesStats",
                        "DeleteIndex",
                        "GetAlias",
                        "ReindexOnServer",

                        "CatAliases",
                        "CatAllocation",
                        "CatCount",
                        "CatFielddata",
                        "CatHealth",
                        "CatHelp",
                        "CatIndices",
                        "CatMaster",
                        "CatNodeAttributes",
                        "CatNodes",
                        "CatPendingTasks",
                        "CatPlugins",
                        "CatRecovery",
                        "CatRepositories",
                        "CatSegments",
                        "CatShards",
                        // "CatSnapshots",
                        "CatTasks",
                        "CatTemplates",
                        "CatThreadPool",

                        // "PutJob",
                        // "ValidateJob",
                        // "GetInfluencers",
                        // "GetJobs",
                        // "GetJobStats",
                        // "GetModelSnapshots",
                        // "GetOverallBuckets",
                        // "FlushJob",
                        // "ForecastJob",
                        // "GetAnomalyRecords",
                        // "GetBuckets",
                        // "GetCategories",
                        // "CloseJob",
                        // "OpenJob",
                        // "DeleteJob",

                        "ClusterAllocationExplain",
                        "ClusterGetSettings",
                        "ClusterHealth",
                        "ClusterPendingTasks",
                        "ClusterPutSettings",
                        "ClusterReroute",
                        "ClusterState",
                        "ClusterStats",

                        "PutRole",
                        // "PutRoleMapping",
                        "GetRole",
                        // "GetRoleMapping",
                        // "DeleteRoleMapping",
                        "DeleteRole",
                        "PutUser",
                        "ChangePassword",
                        "GetUser",
                        // "DisableUser",
                        "DeleteUser",
                    });
                }

                var spans = agent.WaitForSpans(expected.Count)
                                 .Where(s => DictionaryExtensions.GetValueOrDefault(s.Tags, Tags.InstrumentationName) == "elasticsearch-net")
                                 .OrderBy(s => s.Start)
                                 .ToList();

                var statementNames = new List<string>
                {
                    "Bulk",
                    "BulkAlias",
                    "ChangePassword",
                    "ClusterAllocationExplain",
                    "ClusterPutSettings",
                    "ClusterReroute",
                    "Create",
                    "CreateIndex",
                    "DeleteByQuery",
                    "PutAlias",
                    "PutIndexTemplate",
                    "PutRole",
                    "PutRole",
                    "PutUser",
                    "ReindexOnServer",
                    "Search",
                    "UpdateIndexSettings"
                };

                foreach (var span in spans)
                {
                    Assert.Equal("Samples.Elasticsearch.V5", span.Service);
                    Assert.Equal("elasticsearch", span.Tags["db.type"]);

                    span.Tags.TryGetValue(Tags.DbStatement, out string statement);
                    if (tagQueries.Equals("true") && statementNames.Contains(span.Name))
                    {
                        Assert.NotNull(statement);
                        Assert.NotEqual(string.Empty, statement);
                        Assert.DoesNotContain(statement, "test_user");
                        Assert.DoesNotContain(statement, "supersecret");
                    }
                    else
                    {
                        Assert.Null(statement);
                    }
                }

                ValidateSpans(spans, (span) => span.Name, expected);
            }
        }
    }
}

#endif
