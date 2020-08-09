// Modified by SignalFx
#if NET461 || NET452

using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class AspNetWebFormsTestsAddClientIp : AspNetWebFormsTests, IClassFixture<IisFixture>
    {
        public AspNetWebFormsTestsAddClientIp(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, addClientIp: true)
        {
        }
    }
}

#endif
