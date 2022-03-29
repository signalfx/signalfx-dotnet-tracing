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
                "\tat Samples.AlwaysOnProfiler.Fs.ClassFs.methodFs(System.String)\n" +
                "\tat Samples.AlwaysOnProfiler.Vb.ClassVb.MethodVb(System.String)\n" +
                "\tat My.Custom.Test.Namespace.ClassENonStandardCharacters\u0104\u0118\u00D3\u0141\u017B\u0179\u0106\u0105\u0119\u00F3\u0142\u017C\u017A\u015B\u0107\u011C\u0416\u13F3\u2CC4\u02A4\u01CB\u2093\u06BF\u0B1F\u0D10\u1250\u3023\u203F\u0A6E\u1FAD_\u00601.GenericMethodDFromGenericClass[TMethod, TMethod2](TClass, TMethod, TMethod2)\n" +
                "\tat My.Custom.Test.Namespace.ClassD`21.MethodD(T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, Unknown)\n" +
                "\tat My.Custom.Test.Namespace.GenericClassC`1.GenericMethodCFromGenericClass[T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20](T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, Unknown)\n" +
                "\tat My.Custom.Test.Namespace.GenericClassC`1.GenericMethodCFromGenericClass(T)\n" +
                "\tat My.Custom.Test.Namespace.ClassA.InternalClassB`2.DoubleInternalClassB.TripleInternalClassB`1.MethodB[TB](System.String, TC[], TB, TD, System.Collections.Generic.IList`1[TA], System.Collections.Generic.IList`1[System.String])\n" +
                "\tat My.Custom.Test.Namespace.ClassA.<MethodAOthers>g__Action|4_0[T](System.String)\n" +
                "\tat My.Custom.Test.Namespace.ClassA.MethodAOthers[T](System.String, System.Object, My.Custom.Test.Namespace.CustomClass, My.Custom.Test.Namespace.CustomStruct, My.Custom.Test.Namespace.CustomClass[], My.Custom.Test.Namespace.CustomStruct[], System.Collections.Generic.List`1[T])\n" +
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
