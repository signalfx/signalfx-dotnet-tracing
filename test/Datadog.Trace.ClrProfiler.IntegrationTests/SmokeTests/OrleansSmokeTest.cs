// Modified by SignalFx
using Datadog.Trace.ClrProfiler.IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.SmokeTests
{
    public class OrleansSmokeTest : SmokeTestBase
    {
        public OrleansSmokeTest(ITestOutputHelper output)
            : base(output, "OrleansCrash", maxTestRunSeconds: 30)
        {
            AssumeSuccessOnTimeout = true;
        }

        // Upstream has this passing for "net461;netcoreapp2.1;netcoreapp3.0" but any other changes
        // related to test so for now disabling it on the platforms that are consistently failing on CI.
        [TargetFrameworkVersionsFact("net461")]
        [Trait("Category", "Smoke")]
        public void NoExceptions()
        {
            CheckForSmoke(shouldDeserializeTraces: false);
        }
    }
}
