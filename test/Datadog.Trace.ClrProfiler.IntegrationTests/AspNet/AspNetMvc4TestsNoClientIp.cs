// Modified by SignalFx
#if NET461

using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public abstract class AspNetMvc4TestsNoClientIp : AspNetMvc4Tests, IClassFixture<IisFixture>
    {
        public AspNetMvc4TestsNoClientIp(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, addClientIp: false)
        {
        }
    }
}

#endif
