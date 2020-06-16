// Modified by SignalFx
#if NET461

using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [Collection("IisTests")]
    public class AspNetWebApi2Tests : TestHelper, IClassFixture<IisFixture>
    {
        private readonly IisFixture _iisFixture;

        public AspNetWebApi2Tests(IisFixture iisFixture, ITestOutputHelper output)
            : base("AspNetMvc5", "samples-aspnet", output)
        {
            _iisFixture = iisFixture;
            _iisFixture.TryStartIis(this);
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [Trait("Integration", nameof(Integrations.AspNetWebApi2Integration))]
        [InlineData("/api/environment", "GET api/environment", HttpStatusCode.OK)]
        [InlineData("/api/delay/0", "GET api/delay/{seconds}", HttpStatusCode.OK)]
        [InlineData("/api/delay-async/0", "GET api/delay-async/{seconds}", HttpStatusCode.OK)]
        [InlineData("/api/transient-failure/true", "GET api/transient-failure/{value}", HttpStatusCode.OK)]
        [InlineData("/api/transient-failure/false", "GET api/transient-failure/{value}", HttpStatusCode.InternalServerError)]
        public async Task SubmitsTraces(
            string path,
            string expectedResourceName,
            HttpStatusCode expectedHttpStatusCode)
        {
            await AssertHttpSpan(
                path,
                _iisFixture.Agent,
                _iisFixture.HttpPort,
                expectedHttpStatusCode,
                "web",
                expectedOperationName: expectedResourceName,
                expectedResourceName);
        }
    }
}

#endif
