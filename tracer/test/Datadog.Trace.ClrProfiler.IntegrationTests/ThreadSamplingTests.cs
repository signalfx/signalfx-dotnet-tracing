// Modified by Splunk Inc.

// Thread Sampling is not supported by .NET Framework and lower versions of .NET Core

#if NETCOREAPP3_1_OR_GREATER

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            using (var processResult = RunSampleAndWaitForExit(agent.Port, logCollectorPort: logsCollector.Port))
            {
                var logsData = logsCollector.LogsData.ToArray();
                // The application works for 6 seconds with debug logging enabled we expect at least 2 attempts of thread sampling in CI.
                // On a dev box it is typical to get at least 4 but the CI machines seem slower, using 3
                logsData.Length.Should().BeGreaterOrEqualTo(expected: 3);

                var settings = VerifyHelper.GetThreadSamplingVerifierSettings();
                settings.UseTextForParameters("OnlyCommonAttributes");

                await DumpLogRecords(logsData);

                for (var index = 0; index < logsData.Length; index++)
                {
                    var data = logsData[index];
                    var logRecords = data.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;

                    using (new AssertionScope())
                    {
                        if (index == 0 || index == logsData.Length - 1)
                        {
                            // skip verification for the first and the last logs. Depending on env., the expected method may not have started or is already in a finished state
                            logRecords.Should().ContainSingle(x => ContainStackTraceForClassHierarchy(x));
                        }

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

        private async Task DumpLogRecords(LogsData[] logsData)
        {
            foreach (var data in logsData)
            {
                await using var memoryStream = new MemoryStream();
                await System.Text.Json.JsonSerializer.SerializeAsync(memoryStream, data);
                memoryStream.Position = 0;
                using var sr = new StreamReader(memoryStream);
                var readToEnd = await sr.ReadToEndAsync();

                Output.WriteLine(readToEnd);
            }
        }
    }
}

#endif
