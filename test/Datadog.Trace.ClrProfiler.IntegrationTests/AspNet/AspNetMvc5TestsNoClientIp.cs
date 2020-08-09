// Modified by SignalFx
#if NET461

using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [Collection("IisTests")]
    public class AspNetMvc5TestsNoClientIp : AspNetMvc5Tests, IClassFixture<IisFixture>
    {
        public AspNetMvc5TestsNoClientIp(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, addClientIp: false)
        {
        }
    }
}

#endif
