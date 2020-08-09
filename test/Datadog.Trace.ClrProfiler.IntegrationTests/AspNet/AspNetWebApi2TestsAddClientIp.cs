// Modified by SignalFx
#if NET461

using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [Collection("IisTests")]
    public class AspNetWebApi2TestsAddClientIp : AspNetWebApi2Tests, IClassFixture<IisFixture>
    {
        public AspNetWebApi2TestsAddClientIp(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, addClientIp: true)
        {
        }
    }
}

#endif
