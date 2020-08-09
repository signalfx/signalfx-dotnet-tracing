// Modified by SignalFx
#if NET461

using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public abstract class AspNetMvc5Tests : TestHelper
    {
        private readonly IisFixture _iisFixture;
        private readonly bool _addClientIp;

        public AspNetMvc5Tests(IisFixture iisFixture, ITestOutputHelper output, bool addClientIp)
            : base("AspNetMvc5", "samples-aspnet", output)
        {
            _iisFixture = iisFixture;
            _iisFixture.TryStartIis(this, addClientIp);
            _addClientIp = addClientIp;
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [Trait("Integration", nameof(Integrations.AspNetMvcIntegration))]
        [InlineData("/Home/Index", "GET", "/home/index")]
        [InlineData("/delay/0", "GET", "/delay/{seconds}")]
        [InlineData("/delay-async/0", "GET", "/delay-async/{seconds}")]
        public async Task SubmitsTraces(
            string path,
            string expectedVerb,
            string expectedResourceSuffix)
        {
            var expectedResourceName = $"{expectedVerb} {expectedResourceSuffix}";
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
