// Modified by Splunk Inc.

// Thread Sampling is not supported by .NET Framework and lower versions of .NET Core

#if NETCOREAPP3_1_OR_GREATER

using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using OpenTelemetry.TestHelpers.Proto.Logs.V1;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [UsesVerify]
    public class AlwaysOnProfilerPlainTextTests : AlwaysOnProfilerTests
    {
        public AlwaysOnProfilerPlainTextTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public async void SubmitThreadSamples()
        {
            SetEnvironmentVariable("SIGNALFX_PROFILER_ENABLED", "true");
            SetEnvironmentVariable("SIGNALFX_PROFILER_CALL_STACK_INTERVAL", "1000");
            SetEnvironmentVariable("SIGNALFX_PROFILER_EXPORT_FORMAT", "Text");

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (var logsCollector = EnvironmentHelper.GetMockOtelLogsCollector())
            using (var processResult = RunSampleAndWaitForExit(agent, logCollectorPort: logsCollector.Port))
            {
                var logsData = logsCollector.LogsData.ToArray();
                // The application works for 6 seconds with debug logging enabled we expect at least 2 attempts of thread sampling in CI.
                // On a dev box it is typical to get at least 4 but the CI machines seem slower, using 2
                logsData.Length.Should().BeGreaterOrEqualTo(expected: 2);

                var settings = VerifyHelper.GetThreadSamplingVerifierSettings();
                settings.UseTextForParameters("OnlyCommonAttributes");

                await DumpLogRecords(logsData);

                var containStackTraceForClassHierarchy = false;
                var expectedStackTrace = string.Join(string.Empty, CreateExpectedStackTrace().Select(frame => $"\tat {frame}\n"));

                foreach (var data in logsData)
                {
                    var logRecords = data.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;

                    containStackTraceForClassHierarchy |= logRecords.Any(record => ContainStackTraceForClassHierarchy(record, expectedStackTrace));

                    using (new AssertionScope())
                    {
                        AllShouldHaveCorrectAttributes(logRecords, "text");
                        AllBodiesShouldHaveCorrectFormat(logRecords);
                    }

                    // all samples should contain the same common attributes, only stack traces are vary
                    logRecords.Clear();
                    await Verifier.Verify(data, settings);
                }

                Assert.True(containStackTraceForClassHierarchy, "At least one stack trace containing class hierarchy should be reported.");
            }
        }

        private static void AllBodiesShouldHaveCorrectFormat(List<LogRecord> logRecords)
        {
            var stackTraces = logRecords.Select(x => x.Body.StringValue);

            foreach (var stackTrace in stackTraces)
            {
                stackTrace.Should().MatchRegex(@""".{0,}"" #\d+ prio=0 os_prio=0 cpu=0 elapsed=0 tid=0x\S+ nid=\S+\n\n(\tat .+\(.*\)\n)+");
            }
        }

        private static bool ContainStackTraceForClassHierarchy(LogRecord logRecord, string expectedStackTrace)
        {
            return logRecord.Body.StringValue.Contains(expectedStackTrace);
        }
    }
}

#endif