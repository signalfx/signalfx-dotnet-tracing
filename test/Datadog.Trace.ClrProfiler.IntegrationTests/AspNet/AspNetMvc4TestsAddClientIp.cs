// Modified by SignalFx
#if NET461

using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public abstract class AspNetMvc4TestsAddClientIp : AspNetMvc4Tests, IClassFixture<IisFixture>
    {
        public AspNetMvc4TestsAddClientIp(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, addClientIp: true)
        {
        }
    }
}

#endif
