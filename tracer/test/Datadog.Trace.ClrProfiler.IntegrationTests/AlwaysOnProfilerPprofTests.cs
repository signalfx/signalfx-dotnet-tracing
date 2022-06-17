// Modified by Splunk Inc.

// Thread Sampling is not supported by .NET Framework and lower versions of .NET Core

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Datadog.Trace.TestHelpers;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.Pprof.Proto.Profile;
using FluentAssertions;
using FluentAssertions.Execution;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [UsesVerify]
    public class AlwaysOnProfilerPprofTests : AlwaysOnProfilerTests
    {
        public AlwaysOnProfilerPprofTests(ITestOutputHelper output)
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

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (var logsCollector = EnvironmentHelper.GetMockOtelLogsCollector())
            using (var processResult = RunSampleAndWaitForExit(agent, logsCollector.Port))
            {
                var logsData = logsCollector.LogsData.ToArray();
                // The application works for 6 seconds with debug logging enabled we expect at least 2 attempts of thread sampling in CI.
                // On a dev box it is typical to get at least 4 but the CI machines seem slower, using 2
                logsData.Length.Should().BeGreaterOrEqualTo(expected: 2);

                var settings = VerifyHelper.GetThreadSamplingVerifierSettings();
                settings.UseTextForParameters("OnlyCommonAttributes");

                await DumpLogRecords(logsData);

                var containStackTraceForClassHierarchy = false;
                var expectedStackTrace = string.Join("\n", CreateExpectedStackTrace());

                foreach (var data in logsData)
                {
                    IList<Profile> profiles = new List<Profile>();
                    var logRecords = data.ResourceLogs[0].InstrumentationLibraryLogs[0].Logs;

                    foreach (var gzip in logRecords.Select(record => record.Body.StringValue).Select(Convert.FromBase64String))
                    {
                        await using var memoryStream = new MemoryStream(gzip);
                        await using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
                        var profile = Serializer.Deserialize<Profile>(gzipStream);
                        profiles.Add(profile);
                    }

                    containStackTraceForClassHierarchy |= profiles.Any(profile => ContainStackTraceForClassHierarchy(profile, expectedStackTrace));

                    using (new AssertionScope())
                    {
                        AllShouldHaveCorrectAttributes(logRecords, "pprof-gzip-base64");
                    }

                    // all samples should contain the same common attributes, only stack traces are vary
                    logRecords.Clear();
                    await Verifier.Verify(data, settings);
                }

                Assert.True(containStackTraceForClassHierarchy, "At least one stack trace containing class hierarchy should be reported.");
            }
        }

        private static bool ContainStackTraceForClassHierarchy(Profile profile, string expectedStackTrace)
        {
            var frames = profile.Locations
                                .SelectMany(location => location.Lines)
                                .Select(line => line.FunctionId)
                                .Select(functionId => profile.Functions[(int)functionId - 1])
                                .Select(function => profile.StringTables[(int)function.Name]);

            var stackTrace = string.Join("\n", frames);
            return stackTrace.Contains(expectedStackTrace);
        }
    }
}

#endif
