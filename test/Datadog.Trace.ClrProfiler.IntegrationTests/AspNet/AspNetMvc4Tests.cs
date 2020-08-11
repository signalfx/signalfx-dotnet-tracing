// Modified by SignalFx
#if NET461

using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public abstract class AspNetMvc4Tests : TestHelper
    {
        private readonly IisFixture _iisFixture;
        private readonly bool _addClientIp;

        public AspNetMvc4Tests(IisFixture iisFixture, ITestOutputHelper output, bool addClientIp)
            : base("AspNetMvc4", "samples-aspnet", output)
        {
            _iisFixture = iisFixture;
            _iisFixture.TryStartIis(this, addClientIp);
            _addClientIp = addClientIp;
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [Trait("Integration", nameof(Integrations.AspNetMvcIntegration))]
        [InlineData("/Home/Index", "GET /home/index")]
        public async Task SubmitsTraces(
            string path,
            string expectedResourceName)
        {
            await AssertHttpSpan(
                path,
                _iisFixture.Agent,
                _iisFixture.HttpPort,
                HttpStatusCode.OK,
                "web",
                expectedOperationName: expectedResourceName,
                expectedResourceName,
                _addClientIp);
        }
    }
}

#endif
