// Modified by SignalFx
#if NET461

using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [Collection("IisTests")]
    public class AspNetWebApi2TestsNoClientIp : AspNetWebApi2Tests, IClassFixture<IisFixture>
    {
        public AspNetWebApi2TestsNoClientIp(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, addClientIp: false)
        {
        }
    }
}

#endif
