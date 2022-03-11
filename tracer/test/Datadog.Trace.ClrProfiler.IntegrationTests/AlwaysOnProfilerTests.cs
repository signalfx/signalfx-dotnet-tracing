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
    public class AlwaysOnProfilerTests : TestHelper
    {
        public AlwaysOnProfilerTests(ITestOutputHelper output)
            : base("AlwaysOnProfiler", output)
        {
            SetServiceVersion("1.0.0");
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public async void SubmitThreadSamples()
        {
            SetEnvironmentVariable("SIGNALFX_PROFILER_ENABLED", "true");
            SetEnvironmentVariable("SIGNALFX_PROFILER_CALL_STACK_INTERVAL", "1000");

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
                foreach (var data in logsData)
                {
                    var logRecords = data.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;

                    containStackTraceForClassHierarchy |= logRecords.Any(ContainStackTraceForClassHierarchy);

                    using (new AssertionScope())
                    {
                        AllShouldHaveCorrectAttributes(logRecords);
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
                "\tat System.Threading.Thread.Sleep(System.TimeSpan)\n" +
                "\tat My.Custom.Test.Namespace.ClassD`1.GenericMethodDFromGenericClass(!0, !!0)\n" +
                "\tat SharedGenericFunction.GenericMethodCFromGenericClass(!0)\n" +
                "\tat TripleInternalClassB.MethodB(System.String)\n" +
                "\tat My.Custom.Test.Namespace.ClassA.<MethodAOthers>g__Action|4_0(System.String)\n" +
                "\tat My.Custom.Test.Namespace.ClassA.MethodAOthers(System.String, System.Object, My.Custom.Test.Namespace.CustomClass, My.Custom.Test.Namespace.CustomStruct, My.Custom.Test.Namespace.CustomClass[], My.Custom.Test.Namespace.CustomStruct[], System.Collections.Generic.List`1[!!0])\n" +
                "\tat My.Custom.Test.Namespace.ClassA.MethodAFloats(System.Single, System.Double)\n" +
                "\tat My.Custom.Test.Namespace.ClassA.MethodAInts(System.UInt16, System.Int16, System.UInt32, System.Int32, System.UInt64, System.Int64, System.IntPtr, System.UIntPtr)\n" +
                "\tat My.Custom.Test.Namespace.ClassA.MethodABytes(System.Boolean, System.Char, System.SByte, System.Byte)\n" +
                "\tat My.Custom.Test.Namespace.ClassA.MethodA()\n");
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
