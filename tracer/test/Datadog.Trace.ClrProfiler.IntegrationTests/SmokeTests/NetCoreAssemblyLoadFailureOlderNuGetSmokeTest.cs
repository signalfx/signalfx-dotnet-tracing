// <copyright file="NetCoreAssemblyLoadFailureOlderNuGetSmokeTest.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#if !NETFRAMEWORK
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.SmokeTests
{
    public class NetCoreAssemblyLoadFailureOlderNuGetSmokeTest : SmokeTestBase
    {
        public NetCoreAssemblyLoadFailureOlderNuGetSmokeTest(ITestOutputHelper output)
            : base(output, "NetCoreAssemblyLoadFailureOlderNuGet")
        {
        }

        [Fact(Skip = "No previous version was created yet")]
        [Trait("Category", "Smoke")]
        public void NoExceptions()
        {
            CheckForSmoke(shouldDeserializeTraces: false);
        }
    }
}
#endif
