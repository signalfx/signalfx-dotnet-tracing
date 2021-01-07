// Modified by SignalFx
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using SignalFx.Tracing.ExtensionMethods;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class Elasticsearch6Tests : TestHelper
    {
        public Elasticsearch6Tests(ITestOutputHelper output)
            : base("Elasticsearch", output)
        {
        }

        public static IEnumerable<object[]> TestParameters()
        {
            foreach (var versions in PackageVersions.ElasticSearch6)
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
                        "AliasExists",
                        "DeleteAlias",
                        "DeleteAlias",
                        "CreateIndex",
                        "SplitIndex", // Only present on 6.1+
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

                        "PutJob",
                        "ValidateJob",
                        "GetInfluencers",
                        "GetJobs",
                        "GetJobStats",
                        "GetModelSnapshots",
                        "FlushJob",
                        "GetOverallBuckets", // Only present on 6.1+
                        "ForecastJob", // Only present on 6.1+
                        "GetAnomalyRecords",
                        "GetBuckets",
                        "GetCategories",
                        "CloseJob",
                        "OpenJob",
                        "DeleteJob",

                        "ClusterAllocationExplain",
                        "ClusterGetSettings",
                        "ClusterHealth",
                        "ClusterPendingTasks",
                        "ClusterPutSettings",
                        "ClusterReroute",
                        "ClusterState",
                        "ClusterStats",

                        "PutRole",
                        "PutRoleMapping",
                        "GetRole",
                        "GetRoleMapping",
                        "DeleteRoleMapping",
                        "DeleteRole",
                        "PutUser",
                        "ChangePassword",
                        "GetUser",
                        "DisableUser",
                        "DeleteUser",
                    });

                    if (string.IsNullOrEmpty(packageVersion) || packageVersion.CompareTo("6.1.0") < 0)
                    {
                        expected.Remove("SplitIndex");
                        expected.Remove("GetOverallBuckets");
                        expected.Remove("ForecastJob");
                    }
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
                    "FlushJob",
                    "GetAnomalyRecords",
                    "GetBuckets",
                    "GetCategories",
                    "GetInfluencers",
                    "GetModelSnapshots",
                    "GetOverallBuckets",
                    "PutAlias",
                    "PutIndexTemplate",
                    "PutJob",
                    "PutRole",
                    "PutUser",
                    "PutRoleMapping",
                    "ReindexOnServer",
                    "Search",
                    "SplitIndex",
                    "UpdateIndexSettings",
                    "ValidateJob",
                };

                foreach (var span in spans)
                {
                    Assert.Equal("Samples.Elasticsearch", span.Service);
                    Assert.Equal("elasticsearch", span.Tags["db.type"]);

                    span.Tags.TryGetValue(Tags.DbStatement, out string statement);
                    if (tagQueries.Equals("true") && statementNames.Contains(span.Name))
                    {
                        Assert.NotNull(statement);
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
