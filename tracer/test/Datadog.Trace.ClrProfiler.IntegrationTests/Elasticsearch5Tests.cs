// <copyright file="Elasticsearch5Tests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.TestHelpers;
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
            SetServiceVersion("1.0.0");
        }

        public static System.Collections.Generic.IEnumerable<object[]> GetElasticsearch()
        {
            foreach (var item in PackageVersions.ElasticSearch5)
            {
                yield return item.Concat(false, false);
                yield return item.Concat(true, false);
                yield return item.Concat(false, true);
                yield return item.Concat(true, true);
            }
        }

        [SkippableTheory]
        [MemberData(nameof(GetElasticsearch))]
        [Trait("Category", "EndToEnd")]
        [Trait("Category", "ArmUnsupported")]
        public void SubmitsTraces(string packageVersion, bool enableCallTarget, string tagQueries)
        {
            SetEnvironmentVariable("SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES", tagQueries.ToString().ToLowerInvariant());
            SetCallTargetSettings(enableCallTarget);

            int agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockTracerAgent(agentPort))
            using (RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion))
            {
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
                                 .Where(s => s.Type == "elasticsearch")
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
                    Assert.Equal("elasticsearch.query", span.LogicScope);
                    Assert.Equal("Samples.Elasticsearch.V5", span.Service);
                    Assert.Equal("elasticsearch", span.Type);
                    Assert.Equal("elasticsearch", DictionaryExtensions.GetValueOrDefault(span.Tags, "db.system"));
                    Assert.Contains(Tags.Version, (IDictionary<string, string>)span.Tags);

                    span.Tags.TryGetValue(Tags.DbStatement, out string statement);
                    if (enableCallTarget && tagQueries && statementNames.Contains(span.Name))
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

                ValidateSpans(spans, (span) => span.Resource, expected);
            }
        }
    }
}

#endif
