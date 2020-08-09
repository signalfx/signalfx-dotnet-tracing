// Modified by SignalFx
using Datadog.Trace.ClrProfiler.IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AspNetCore
{
    public class AspNetCoreMvc21Tests : AspNetCoreMvcTestBase
    {
        public AspNetCoreMvc21Tests(ITestOutputHelper output)
            : base("AspNetCoreMvc21", output)
        {
        }

        [TargetFrameworkVersionsTheory("netcoreapp2.1")]
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
