// Modified by SignalFx
#if NET461 || NET452

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using SignalFx.Tracing;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public abstract class AspNetWebFormsTests : TestHelper
    {
        private readonly IisFixture _iisFixture;
        private readonly bool _addClientIp;

        // NOTE: Would pass this in addition to the name/output to the new constructor if we removed the Samples.WebForms copied project in favor of the demo repo source project...
        // $"../dd-trace-demo/dotnet-coffeehouse/Datadog.Coffeehouse.WebForms",
        public AspNetWebFormsTests(IisFixture iisFixture, ITestOutputHelper output, bool addClientIp)
            : base("WebForms", "samples-aspnet", output)
        {
            _iisFixture = iisFixture;
            _iisFixture.TryStartIis(this, addClientIp);
            _addClientIp = addClientIp;
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [Trait("Integration", nameof(AspNetWebFormsTests))]
        [InlineData("/Account/Login", "GET /account/login")]
        public async Task SubmitsTraces(
            string path,
            string expectedResourceName)
        {
            await AssertHttpSpan(
                path,
                _iisFixture.Agent,
                _iisFixture.HttpPort,
                HttpStatusCode.OK,
                SpanTypes.Web,
                expectedOperationName: expectedResourceName,
                expectedResourceName,
                _addClientIp);
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        [Trait("Integration", nameof(AspNetWebFormsTests))]
        public async Task NestedAsyncElasticCallSubmitsTrace()
        {
            var testStart = DateTime.UtcNow;
            using (var httpClient = new HttpClient())
            {
                // disable tracing for this HttpClient request
                httpClient.DefaultRequestHeaders.Add(HttpHeaderNames.TracingEnabled, "false");

                var response = await httpClient.GetAsync($"http://localhost:{_iisFixture.HttpPort}" + "/Database/Elasticsearch");
                var content = await response.Content.ReadAsStringAsync();
                Output.WriteLine($"[http] {response.StatusCode} {content}");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            var allSpans = _iisFixture.Agent.WaitForSpans(3, minDateTime: testStart)
                                   .OrderBy(s => s.Start)
                                   .ToList();

            Assert.True(allSpans.Count > 0, "Expected there to be spans.");

            var elasticSpans = allSpans
                             .Where(s => s.Tags.ContainsKey("db.type") && s.Tags["db.type"] == "elasticsearch")
                             .ToList();

            Assert.True(elasticSpans.Count > 0, "Expected elasticsearch spans.");

            var expectedOperations = new string[]
            {
                "ClusterHealth",
                "ClusterState",
            };
            var expectedOperationIdx = 0;

            foreach (var span in elasticSpans)
            {
                Assert.Equal(expectedOperations[expectedOperationIdx++], span.Name);
                Assert.Equal("Development Web Site", span.Service);
                Assert.Equal("elasticsearch", span.Tags["db.type"]);
            }
        }
    }
}

#endif
