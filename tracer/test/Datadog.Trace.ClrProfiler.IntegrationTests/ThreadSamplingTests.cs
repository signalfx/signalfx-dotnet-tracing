// Modified by Splunk Inc.

// Thread Sampling is not supported by .NET Framework and lower versions of .NET Core
#if NETCOREAPP3_1_OR_GREATER

using Datadog.Trace.TestHelpers;
using FluentAssertions;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [UsesVerify]
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
        public async void SubmitTheadSamples()
        {
            SetEnvironmentVariable("SIGNALFX_THREAD_SAMPLING_ENABLED", "true");
            SetEnvironmentVariable("SIGNALFX_THREAD_SAMPLING_PERIOD", "1000");

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (var logsCollector = EnvironmentHelper.GetMockOtelLogsCollector())
            using (var processResult = RunSampleAndWaitForExit(agent.Port, logCollectorPort: logsCollector.Port))
            {
                var logsData = logsCollector.LogsData.ToArray();
                // The application works for 5 seconds with debug logging enabled we expect at least 2 attempts of thread sampling in CI.
                // On a dev box it is typical to get at least 3 but the CI machines seem slower, using 2
                logsData.Length.Should().BeGreaterOrEqualTo(expected: 2);

                var settings = VerifyHelper.GetThreadSamplingVerifierSettings();
                foreach (var data in logsData)
                {
                    // all samples should be the same, as the testing application is just Thread.Sleep()ing
                    await Verifier.Verify(data, settings);
                }
            }
        }
    }
}

#endif
