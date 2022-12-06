// <copyright file="RuntimeMetricsTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;
using FluentAssertions;
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

            requests.Count.Should().Be(0, "When metrics are disabled, no metrics should be sent.");
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

            var constantDimensions = new List<Dimension>
            {
                new() { key = "deployment.environment", value = "integration_tests" },
                new() { key = "service.name", value = "Samples.RuntimeMetrics" },
                new() { key = "telemetry.sdk.name", value = "signalfx-dotnet-tracing" },
                new() { key = "telemetry.sdk.language", value = "dotnet" },
                new() { key = "telemetry.sdk.version", value = TracerConstants.AssemblyVersion },
                new() { key = "splunk.distro.version", value = TracerConstants.AssemblyVersion }
            };

            using var agent = EnvironmentHelper.GetMockAgent(useStatsD: true);

            using var processResult = RunSampleAndWaitForExit(agent);

            var dataPoints = agent.Metrics;

            // Check if we receive 2 kinds of metrics:
            // - exception count is gathered using common .NET APIs
            // - contention count is gathered using platform-specific APIs

            var exceptionRequestsCount = dataPoints.Count(r => r.metric == "process.runtime.dotnet.exceptions.count");

            exceptionRequestsCount.Should().BeGreaterThan(0);

            // Check if .NET Framework or .NET Core 3.1+
            if (!EnvironmentHelper.IsCoreClr()
             || (Environment.Version.Major == 3 && Environment.Version.Minor == 1)
             || Environment.Version.Major >= 5)
            {
                var contentionRequestsCount = dataPoints.Count(r => r.metric == "process.runtime.dotnet.monitor.lock_contention.count");

                contentionRequestsCount.Should().BeGreaterThan(0);
            }

            foreach (var dataPoint in dataPoints)
            {
                foreach (var dimension in constantDimensions)
                {
                    dataPoint.dimensions.Should().ContainEquivalentOf(dimension);
                }

                dataPoint.dimensions.Should().Contain(dimension => dimension.key == "host.name");
                dataPoint.dimensions.Should().Contain(dimension => dimension.key == "process.pid");
            }

            agent.Exceptions.Should().BeEmpty();
            VerifyInstrumentation(processResult.Process);
        }
    }
}
