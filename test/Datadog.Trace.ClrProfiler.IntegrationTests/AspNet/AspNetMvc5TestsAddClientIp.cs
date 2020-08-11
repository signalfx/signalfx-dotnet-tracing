// Modified by SignalFx
#if NET461

using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [Collection("IisTests")]
    public class AspNetMvc5TestsAddClientIp : AspNetMvc5Tests, IClassFixture<IisFixture>
    {
        public AspNetMvc5TestsAddClientIp(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, addClientIp: true)
        {
        }
    }
}

#endif
