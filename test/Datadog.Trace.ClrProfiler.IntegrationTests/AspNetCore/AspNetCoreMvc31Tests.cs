// Modified by SignalFx
using Datadog.Trace.ClrProfiler.IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AspNetCore
{
    public class AspNetCoreMvc31Tests : AspNetCoreMvcTestBase
    {
        public AspNetCoreMvc31Tests(ITestOutputHelper output)
            : base("AspNetCoreMvc31", output)
        {
            // EnableDebugMode();
        }

        [TargetFrameworkVersionsTheory("netcoreapp3.1")]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [InlineData(true)]
        [InlineData(false)]
        public void MeetsAllAspNetCoreMvcExpectations(bool addClientIp)
        {
            // No package versions are relevant because this is built-in
            RunTraceTestOnSelfHosted(string.Empty, addClientIp);
        }
    }
}
