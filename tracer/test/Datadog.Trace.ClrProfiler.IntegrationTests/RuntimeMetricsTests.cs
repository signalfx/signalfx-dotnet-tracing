// <copyright file="RuntimeMetricsTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Linq;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [CollectionDefinition(nameof(RuntimeMetricsTests), DisableParallelization = true)]
    public class RuntimeMetricsTests : TestHelper
    {
        public RuntimeMetricsTests(ITestOutputHelper output)
            : base("RuntimeMetrics", output)
        {
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [Trait("SupportsInstrumentationVerification", "True")]
        public void MetricsDisabled()
        {
            SetEnvironmentVariable("SIGNALFX_METRICS_NetRuntime_ENABLED", "0");
            using var agent = EnvironmentHelper.GetMockAgent(useStatsD: true);

            using var processResult = RunSampleAndWaitForExit(agent);
            var requests = agent.Metrics;

            Assert.True(requests.Count == 0, "Received metrics despite being disabled. Metrics received: " + string.Join("\n", requests));
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [Trait("SupportsInstrumentationVerification", "True")]
        public void UdpSubmitsMetrics()
        {
            EnvironmentHelper.EnableDefaultTransport();
            RunTest();
        }

        [SkippableFact(Skip = "Flaky test")]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void NamedPipesSubmitsMetrics()
        {
            if (!EnvironmentTools.IsWindows())
            {
                throw new SkipException("Can't use WindowsNamedPipes on non-Windows");
            }

            EnvironmentHelper.EnableWindowsNamedPipes();
            // The server implementation of named pipes is flaky so have 3 attempts
            var attemptsRemaining = 3;
            while (true)
            {
                try
                {
                    attemptsRemaining--;
                    RunTest();
                    return;
                }
                catch (Exception ex) when (attemptsRemaining > 0 && ex is not SkipException)
                {
                    Output.WriteLine($"Error executing test. {attemptsRemaining} attempts remaining. {ex}");
                }
            }
        }

        private void RunTest()
        {
            SetEnvironmentVariable("SIGNALFX_METRICS_NetRuntime_ENABLED", "1");
            SetInstrumentationVerification();

            using var agent = EnvironmentHelper.GetMockAgent(useStatsD: true);

            using var processResult = RunSampleAndWaitForExit(agent);

            var requests = agent.Metrics;

            // Check if we receive 2 kinds of metrics:
            // - exception count is gathered using common .NET APIs
            // - contention count is gathered using platform-specific APIs

            var exceptionRequestsCount = requests.Count(r => r.metric == "process.runtime.dotnet.exceptions.count");

            Assert.True(exceptionRequestsCount > 0, "No exception metrics received.");

            // Check if .NET Framework or .NET Core 3.1+
            if (!EnvironmentHelper.IsCoreClr()
             || (Environment.Version.Major == 3 && Environment.Version.Minor == 1)
             || Environment.Version.Major >= 5)
            {
                var contentionRequestsCount = requests.Count(r => r.metric == "process.runtime.dotnet.monitor.lock_contention.count");

                Assert.True(contentionRequestsCount > 0, "No contention metrics received.");
            }

            Assert.Empty(agent.Exceptions);
            VerifyInstrumentation(processResult.Process);
        }
    }
}
