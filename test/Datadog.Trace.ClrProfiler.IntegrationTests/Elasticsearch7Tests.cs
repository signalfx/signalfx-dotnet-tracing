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

            const int expectedSpanCount = 164;
            var mockSpans = agent.WaitForSpans(expectedSpanCount)
                                 .Where(s => DictionaryExtensions.GetValueOrDefault(s.Tags, Tags.InstrumentationName) == "elasticsearch-net")
                                 .OrderBy(s => s.Start)
                                 .ToList();
            var spans = mockSpans;

            foreach (var span in spans)
            {
                Assert.Equal("Samples.Elasticsearch.V7", span.Service);
                Assert.Equal("elasticsearch", span.Tags["db.type"]);
            }

            Assert.Equal(expectedSpanCount, spans.Count);

            // ValidateSpans(spans, (span) => span.Name, expected);
        }
    }
}
