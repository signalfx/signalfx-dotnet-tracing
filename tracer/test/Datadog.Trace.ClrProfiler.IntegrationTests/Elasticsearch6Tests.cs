// <copyright file="Elasticsearch6Tests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [Trait("RequiresDockerDependency", "true")]
    public class Elasticsearch6Tests : TestHelper
    {
        public Elasticsearch6Tests(ITestOutputHelper output)
            : base("Elasticsearch", output)
        {
            SetServiceVersion("1.0.0");
        }

        public static IEnumerable<object[]> GetElasticsearch()
        {
            foreach (var item in PackageVersions.ElasticSearch6)
            {
                yield return item.Concat(true);
                yield return item.Concat(false);
            }
        }

        [SkippableTheory]
        [MemberData(nameof(GetElasticsearch))]
        [Trait("Category", "EndToEnd")]
        [Trait("Category", "ArmUnsupported")]
        public void SubmitsTraces(string packageVersion, bool tagQueries)
        {
            SetEnvironmentVariable("SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES", tagQueries.ToString().ToLowerInvariant());

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (RunSampleAndWaitForExit(agent, packageVersion: packageVersion))
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

                    if (string.IsNullOrEmpty(packageVersion) || string.Compare(packageVersion, "6.1.0", StringComparison.Ordinal) < 0)
                    {
                        expected.Remove("SplitIndex");
                        expected.Remove("GetOverallBuckets");
                        expected.Remove("ForecastJob");
                    }
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
                    "FlushJob",
                    "GetAnomalyRecords",
                    "GetBuckets",
                    "GetCategories",
                    "GetInfluencers",
                    "GetModelSnapshots",
                    "PutAlias",
                    "PutIndexTemplate",
                    "PutJob",
                    "PutRole",
                    "PutUser",
                    "PutRoleMapping",
                    "ReindexOnServer",
                    "Search",
                    "UpdateIndexSettings",
                    "ValidateJob",
                };

                foreach (var span in spans)
                {
                    var result = span.IsElasticsearchNet();
                    Assert.True(result.Success, result.ToString());

                    Assert.Equal("Samples.Elasticsearch", span.Service);
                }

                ValidateSpans(spans, (span) => span.Resource, expected);
            }
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("Category", "ArmUnsupported")]
        public void IntegrationDisabled()
        {
            string packageVersion = PackageVersions.ElasticSearch6.First()[0] as string;
            SetEnvironmentVariable($"SIGNALFX_TRACE_{nameof(IntegrationId.ElasticsearchNet)}_ENABLED", "false");
            using var agent = EnvironmentHelper.GetMockAgent();
            using var process = RunSampleAndWaitForExit(agent, packageVersion: packageVersion);
            var spans = agent.WaitForSpans(1).Where(s => s.Type == "elasticsearch").ToList();

            Assert.Empty(spans);
        }
    }
}
