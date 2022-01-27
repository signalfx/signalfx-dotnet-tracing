// Modified by Splunk Inc.

// Thread Sampling is not supported by .NET Framework and lower versions of .NET Core
#if NETCOREAPP3_1_OR_GREATER

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
            SetEnvironmentVariable("SIGNALFX_THREAD_SAMPLING_ENABLED", "true");
            SetEnvironmentVariable("SIGNALFX_THREAD_SAMPLING_PERIOD", "1000");

            // TODO: Start OTel collector, and capture stakcs sent to it, verify the actual stacks.
            // While that is not done use verbose log directed to the stdout.
            SetEnvironmentVariable("SIGNALFX_TRACE_DEBUG", "true");
            SetEnvironmentVariable("SIGNALFX_STDOUT_LOG_ENABLED", "true");

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (var processResult = RunSampleAndWaitForExit(agent))
            {
                // The application works for 5 seconds with debug logging enabled we expect at least 2 attempts of thread sampling in CI.
                // On a dev box it is typical to get at least 3 but the CI machines seem slower, using 2 until the test is improved.
                processResult.StandardOutput.Should().Contain("thread samples captured at", AtLeast.Times(expected: 2));
            }
        }
    }
}

#endif
