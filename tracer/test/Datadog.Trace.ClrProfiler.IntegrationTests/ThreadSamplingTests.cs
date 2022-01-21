// Modified by Splunk Inc.

// Thread Sampling is not supported by .NET Framework
#if !NETFRAMEWORK

using Datadog.Trace.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class ThreadSamplingTests : TestHelper
    {
        public ThreadSamplingTests(ITestOutputHelper output)
            : base("ThreadSampling", output)
        {
            SetServiceVersion("1.0.0");
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void SubmitTheadSamples()
        {
            SetEnvironmentVariable($"SIGNALFX_THREAD_SAMPLING_ENABLED", "true");
            SetEnvironmentVariable($"SIGNALFX_THREAD_SAMPLING_PERIOD", "1000");

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (var processResult = RunSampleAndWaitForExit(agent))
            {
                // at application works 5 seconds, we should expect at least 3 attempts of thread sampling
                processResult.StandardOutput.Should().Contain("thread samples captured at", AtLeast.Times(expected: 3));
            }
        }
    }
}

#endif
