// Modified by Splunk Inc.

// Thread Sampling is not supported by .NET Framework and lower versions of .NET Core
#if NETCOREAPP3_1_OR_GREATER

using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using OpenTelemetry.TestHelpers.Proto.Common.V1;
using OpenTelemetry.TestHelpers.Proto.Logs.V1;
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
        public async void SubmitThreadSamples()
        {
            SetEnvironmentVariable("SIGNALFX_THREAD_SAMPLING_ENABLED", "true");
            SetEnvironmentVariable("SIGNALFX_THREAD_SAMPLING_PERIOD", "1000");

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (var logsCollector = EnvironmentHelper.GetMockOtelLogsCollector())
            using (var processResult = RunSampleAndWaitForExit(agent, logCollectorPort: logsCollector.Port))
            {
                var logsData = logsCollector.LogsData.ToArray();
                // The application works for 5 seconds with debug logging enabled we expect at least 2 attempts of thread sampling in CI.
                // On a dev box it is typical to get at least 3 but the CI machines seem slower, using 2
                logsData.Length.Should().BeGreaterOrEqualTo(expected: 2);

                var settings = VerifyHelper.GetThreadSamplingVerifierSettings();
                settings.UseTextForParameters("OnlyCommonAttributes");

                foreach (var data in logsData)
                {
                    var logRecords = data.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;

                    using (new AssertionScope())
                    {
                        logRecords.Should().ContainSingle(x => ContainStackTraceForClassHierarchy(x));
                        AllShouldHaveCorrectAttributes(logRecords);
                        AllBodiesShouldHaveCorrectFormat(logRecords);
                    }

                    // all samples should contain the same common attributes, only stack traces are vary
                    logRecords.Clear();
                    await Verifier.Verify(data, settings);
                }
            }
        }

        private static void AllBodiesShouldHaveCorrectFormat(List<LogRecord> logRecords)
        {
            var stackTraces = logRecords.Select(x => x.Body.StringValue);

            foreach (var stackTrace in stackTraces)
            {
                stackTrace.Should().MatchRegex(@""".{0,}"" #\d+ prio=0 os_prio=0 cpu=0 elapsed=0 tid=0x\S+ nid=\S+\n\n(\tat .+\(unknown\)\n)+");
            }
        }

        private static void AllShouldHaveCorrectAttributes(List<LogRecord> logRecords)
        {
            var expectedAttributesForLogRecord = new LogRecord();
            expectedAttributesForLogRecord.Attributes.Add(
                new KeyValue
                {
                    Key = "com.splunk.sourcetype",
                    Value = new AnyValue { StringValue = "otel.profiling" }
                });
            expectedAttributesForLogRecord.Attributes.Add(
                new KeyValue
                {
                    Key = "source.event.period",
                    Value = new AnyValue { IntValue = 1000L }
                });
            logRecords.Should().AllBeEquivalentTo(expectedAttributesForLogRecord, option => option.Including(x => x.Attributes));
        }

        private static bool ContainStackTraceForClassHierarchy(LogRecord logRecord)
        {
            return logRecord.Body.StringValue.Contains(
                "\tat System.Threading.Thread.Sleep(unknown)\n" +
                "\tat ClassD.MethodD(unknown)\n" +
                "\tat ClassC.MethodC(unknown)\n" +
                "\tat ClassB.MethodB(unknown)\n" +
                "\tat ClassA.MethodA(unknown)\n");
        }
    }
}

#endif
