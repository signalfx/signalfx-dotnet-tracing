using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using SignalFx.Tracing.ExtensionMethods;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class Elasticsearch7Tests : TestHelper
    {
        public Elasticsearch7Tests(ITestOutputHelper output)
            : base("Elasticsearch.V7", output)
        {
        }

        public static IEnumerable<object[]> TestParameters()
        {
            foreach (var versions in PackageVersions.ElasticSearch7)
            {
                foreach (var version in versions)
                {
                    yield return new[] { version, "true" };
                    yield return new[] { version, "false" };
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestParameters))]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces(string packageVersion, string tagQueries)
        {
            var agentPort = TcpPortProvider.GetOpenPort();
            var envVars = ZipkinEnvVars;
            envVars["SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES"] = tagQueries;

            using var agent = new MockZipkinCollector(agentPort);
            using var processResult = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion, envVars: envVars);
            Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode}");

            var expected = new List<string>();
            // commands with sync and async
            for (var i = 0; i < 2; i++)
            {
                expected.AddRange(
                    new List<string>
                    {
                        "DELETE http://elasticsearch7:9200/_security/role_mapping/test_role_1",
                        "GET http://elasticsearch7:9200/_cat/aliases",
                        "PUT http://elasticsearch7:9200/elastic-net-example/_create/3",
                        "GET http://elasticsearch7:9200/_cluster/health",
                        "DELETE http://elasticsearch7:9200/_template/test_template_1",
                        "GET http://elasticsearch7:9200/_cat/pending_tasks",
                        "PUT http://elasticsearch7:9200/test_index_1",
                        "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/_flush",
                        "POST http://elasticsearch7:9200/elastic-net-example/_search?typed_keys=true",
                        "GET http://elasticsearch7:9200/_ml/anomaly_detectors/_stats",
                        "GET http://elasticsearch7:9200/_cat/segments",
                        "PUT http://elasticsearch7:9200/_security/user/test_user_1/_password",
                        "GET http://elasticsearch7:9200/test_index_1/_stats",
                        "GET http://elasticsearch7:9200/_cat/indices",
                        "GET http://elasticsearch7:9200/_alias",
                        "PUT http://elasticsearch7:9200/test_index_1/_split/test_index_4",
                        "GET http://elasticsearch7:9200/_ml/anomaly_detectors/test_job",
                        "POST http://elasticsearch7:9200/test_index/_bulk",
                        "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/results/buckets",
                        "GET http://elasticsearch7:9200/test_index_1/_alias",
                        "GET http://elasticsearch7:9200/_cluster/stats",
                        "POST http://elasticsearch7:9200/_ml/anomaly_detectors/_validate",
                        "PUT http://elasticsearch7:9200/_cluster/settings",
                        "PUT http://elasticsearch7:9200/_security/role/test_role_1",
                        "POST http://elasticsearch7:9200/test_index_1/_close",
                        "GET http://elasticsearch7:9200/_cat/snapshots",
                        "PUT http://elasticsearch7:9200/_template/test_template_1",
                        "DELETE http://elasticsearch7:9200/test_index_1/_alias/test_index_2",
                        "GET http://elasticsearch7:9200/_security/user/test_user_1",
                        "GET http://elasticsearch7:9200/_cat/nodeattrs",
                        "GET http://elasticsearch7:9200/_cat/recovery",
                        "POST http://elasticsearch7:9200/test_index/_delete_by_query?size=0",
                        "PUT http://elasticsearch7:9200/_ml/anomaly_detectors/test_job",
                        "GET http://elasticsearch7:9200/_cat/health",
                        "PUT http://elasticsearch7:9200/test_index_1/_settings",
                        "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/_forecast",
                        "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/_close",
                        "GET http://elasticsearch7:9200/_cat/tasks",
                        "GET http://elasticsearch7:9200/_cluster/settings",
                        "HEAD http://elasticsearch7:9200/_alias/test_index_1",
                        "GET http://elasticsearch7:9200/_cluster/state",
                        "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/_open",
                        "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/results/influencers",
                        "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/results/records",
                        "GET http://elasticsearch7:9200/_security/role/test_role_1",
                        "GET http://elasticsearch7:9200/_shard_stores",
                        "DELETE http://elasticsearch7:9200/_security/role/test_role_1",
                        "POST http://elasticsearch7:9200/test_index_1/_open",
                        "PUT http://elasticsearch7:9200/_security/user/test_user_1/_disable",
                        "HEAD http://elasticsearch7:9200/_template/test_template_1",
                        "POST http://elasticsearch7:9200/_aliases",
                        "PUT http://elasticsearch7:9200/_security/user/test_user_1",
                        "DELETE http://elasticsearch7:9200/_security/user/test_user_1",
                        "DELETE http://elasticsearch7:9200/test_index_1",
                        "GET http://elasticsearch7:9200/_cat/master",
                        "GET http://elasticsearch7:9200/_cat/nodes",
                        "PUT http://elasticsearch7:9200/test_index_4",
                        "GET http://elasticsearch7:9200/_cat/plugins",
                        "POST http://elasticsearch7:9200/_reindex",
                        "GET http://elasticsearch7:9200/_cat/repositories",
                        "GET http://elasticsearch7:9200/_cat/shards",
                        "PUT http://elasticsearch7:9200/test_index/_create/2",
                        "GET http://elasticsearch7:9200/_cat/templates",
                        "HEAD http://elasticsearch7:9200/test_index_1",
                        "GET http://elasticsearch7:9200/_cat/allocation",
                        "GET http://elasticsearch7:9200/elastic-net-example/_count",
                        "GET http://elasticsearch7:9200/_cat/count",
                        "GET http://elasticsearch7:9200/_cat/fielddata",
                        "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/model_snapshots",
                        "DELETE http://elasticsearch7:9200/test_index_4",
                        "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/results/overall_buckets",
                        "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/results/categories/",
                        "PUT http://elasticsearch7:9200/test_index_1/_alias/test_index_3",
                        "POST http://elasticsearch7:9200/_cluster/allocation/explain",
                        "GET http://elasticsearch7:9200/_cat/thread_pool",
                        "DELETE http://elasticsearch7:9200/test_index_1/_alias/test_index_3",
                        "GET http://elasticsearch7:9200/_cat",
                        "DELETE http://elasticsearch7:9200/_ml/anomaly_detectors/test_job",
                        "GET http://elasticsearch7:9200/_cluster/pending_tasks",
                        "POST http://elasticsearch7:9200/_cluster/reroute",
                        "PUT http://elasticsearch7:9200/_security/role_mapping/test_role_1",
                        "GET http://elasticsearch7:9200/_security/role_mapping/test_role_1"
                    });
            }

            var mockSpans = agent.WaitForSpans(expected.Count)
                                 .Where(s => DictionaryExtensions.GetValueOrDefault(s.Tags, Tags.InstrumentationName) == "elasticsearch-net")
                                 .OrderBy(s => s.Start)
                                 .ToList();
            var spans = mockSpans;

            var statementNames = new List<string>
            {
                "PUT http://elasticsearch7:9200/test_index_1/_alias/test_index_3",
                "POST http://elasticsearch7:9200/_aliases",
                "PUT http://elasticsearch7:9200/test_index_1/_settings",
                "PUT http://elasticsearch7:9200/test_index_1",
                "POST http://elasticsearch7:9200/_cluster/allocation/explain",
                "PUT http://elasticsearch7:9200/test_index_1/_split/test_index_4",
                "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/model_snapshots",
                "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/results/categories/",
                "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/_flush",
                "PUT http://elasticsearch7:9200/_security/user/test_user_1/_password",
                "POST http://elasticsearch7:9200/_cluster/reroute",
                "PUT http://elasticsearch7:9200/_security/role_mapping/test_role_1",
                "PUT http://elasticsearch7:9200/_template/test_template_1",
                "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/results/influencers",
                "POST http://elasticsearch7:9200/_reindex",
                "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/results/buckets",
                "POST http://elasticsearch7:9200/test_index/_bulk",
                "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/results/records",
                "PUT http://elasticsearch7:9200/test_index/_create/2",
                "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/results/overall_buckets",
                "PUT http://elasticsearch7:9200/_security/user/test_user_1",
                "PUT http://elasticsearch7:9200/_cluster/settings",
                "POST http://elasticsearch7:9200/test_index/_delete_by_query",
                "PUT http://elasticsearch7:9200/_security/role/test_role_1",
                "PUT http://elasticsearch7:9200/_ml/anomaly_detectors/test_job",
                "GET http://localhost:5000/A/s",
                "POST http://elasticsearch7:9200/_ml/anomaly_detectors/_validate",
                "PUT http://elasticsearch7:9200/test_index_4",
                "PUT http://elasticsearch7:9200/elastic-net-example/_create/3",
                "POST http://elasticsearch7:9200/elastic-net-example/_search?typed_keys=true",
                "POST http://elasticsearch7:9200/test_index/_delete_by_query?size=0",
                "POST http://elasticsearch7:9200/_ml/anomaly_detectors/test_job/_close"
            };

            foreach (var span in spans)
            {
                Assert.Equal("Samples.Elasticsearch.V7", span.Service);
                Assert.Equal("elasticsearch", span.Tags["db.type"]);

                span.Tags.TryGetValue(Tags.DbStatement, out var statement);
                if (tagQueries.Equals("true") && statementNames.Contains($"{span.Tags["elasticsearch.method"]} {span.Tags["elasticsearch.url"]}"))
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

            ValidateSpans(spans, span => $"{span.Tags["elasticsearch.method"]} {span.Tags["elasticsearch.url"]}", expected);
        }
    }
}
