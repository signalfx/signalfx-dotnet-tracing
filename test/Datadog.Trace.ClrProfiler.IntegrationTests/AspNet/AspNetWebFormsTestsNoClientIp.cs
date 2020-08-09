// Modified by SignalFx
#if NET461 || NET452

using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class AspNetWebFormsTestsNoClientIp : AspNetWebFormsTests, IClassFixture<IisFixture>
    {
        public AspNetWebFormsTestsNoClientIp(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, addClientIp: false)
        {
        }
    }
}

#endif
