// Modified by SignalFx
#if NET461

using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public abstract class AspNetWebApi2Tests : TestHelper
    {
        private readonly IisFixture _iisFixture;
        private readonly bool _addClientIp;

        public AspNetWebApi2Tests(IisFixture iisFixture, ITestOutputHelper output, bool addClientIp)
            : base("AspNetMvc5", "samples-aspnet", output)
        {
            _iisFixture = iisFixture;
            _iisFixture.TryStartIis(this, addClientIp);
            _addClientIp = addClientIp;
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [Trait("Integration", nameof(Integrations.AspNetWebApi2Integration))]
        [InlineData("/api/environment", "GET /api/environment", HttpStatusCode.OK)]
        [InlineData("/api/delay/0", "GET /api/delay/{seconds}", HttpStatusCode.OK)]
        [InlineData("/api/delay-async/0", "GET /api/delay-async/{seconds}", HttpStatusCode.OK)]
        [InlineData("/api/transient-failure/true", "GET /api/transient-failure/{value}", HttpStatusCode.OK)]
        [InlineData("/api/transient-failure/false", "GET /api/transient-failure/{value}", HttpStatusCode.InternalServerError)]
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
                expectedResourceName,
                _addClientIp);
        }
    }
}

#endif
