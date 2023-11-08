// Modified by Splunk Inc.

// Thread Sampling is not supported by .NET Framework and lower versions of .NET Core

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using OpenTelemetry.TestHelpers.Proto.Collector.Profiles.V1;
using OpenTelemetry.TestHelpers.Proto.Common.V1;
using OpenTelemetry.TestHelpers.Proto.Profiles.V1;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [UsesVerify]
    public class AlwaysOnProfilerPprofTests : TestHelper
    {
        public AlwaysOnProfilerPprofTests(ITestOutputHelper output)
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
            SetEnvironmentVariable("SIGNALFX_PROFILER_EXPORT_INTERVAL", "1000");

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (var profilesCollector = EnvironmentHelper.GetMockOtelProfilesCollector())
            using (var processResult = RunSampleAndWaitForExit(agent, profilesCollector.Port))
            {
                var profilesData = profilesCollector.ProfilesData.ToArray();
                // The application works for 6 seconds with debug logging enabled we expect at least 2 attempts of thread sampling in CI.
                // On a dev box it is typical to get at least 4 but the CI machines seem slower, using 2
                profilesData.Length.Should().BeGreaterOrEqualTo(expected: 2);

                await DumpProfilesData(profilesData);

                var containStackTraceForClassHierarchy = false;
                var expectedStackTrace = string.Join("\n", CreateExpectedStackTrace());

                foreach (var data in profilesData)
                {
                    var dataResourceProfile = data.ResourceProfiles[0];
                    var scopeProfiles = dataResourceProfile.ScopeProfiles[0];
                    var profiles = scopeProfiles.Profiles;

                    containStackTraceForClassHierarchy |= profiles.Any(profile => ContainStackTraceForClassHierarchy(profile, expectedStackTrace));

                    using (new AssertionScope())
                    {
                        ResourceContainsExpectedAttributes(dataResourceProfile.Resource);
                        HasNameAndVersionSet(scopeProfiles.Scope);
                    }

                    profiles.Clear();
                }

                Assert.True(containStackTraceForClassHierarchy, "At least one stack trace containing class hierarchy should be reported.");
            }
        }

        private static IEnumerable<string> CreateExpectedStackTrace()
        {
            var stackTrace = new List<string>
            {
#if NET7_0_OR_GREATER
                "System.Threading.Thread.Sleep(System.Int32)",
#else
                "System.Threading.Thread.Sleep(System.TimeSpan)",
#endif
                "Samples.AlwaysOnProfiler.Fs.ClassFs.methodFs(System.String)",
                "Samples.AlwaysOnProfiler.Vb.ClassVb.MethodVb(System.String)",
                "SignalFx.Tracing.TestDynamicClass.TryInvoke(System.Dynamic.InvokeBinder, System.Object[], System.Object\u0026)",
                "System.Dynamic.UpdateDelegates.UpdateAndExecuteVoid3[T0, T1, T2](System.Runtime.CompilerServices.CallSite, T0, T1, T2)"
            };

#if NETCOREAPP3_1
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                stackTrace.Add("Unknown_Native_Function(unknown)");
            }
#endif

            stackTrace.Add("My.Custom.Test.Namespace.ClassENonStandardCharacters\u0104\u0118\u00D3\u0141\u017B\u0179\u0106\u0105\u0119\u00F3\u0142\u017C\u017A\u015B\u0107\u011C\u0416\u13F3\u2CC4\u02A4\u01CB\u2093\u06BF\u0B1F\u0D10\u1250\u3023\u203F\u0A6E\u1FAD_\u00601.GenericMethodDFromGenericClass[TMethod, TMethod2](TClass, TMethod, TMethod2)");
            stackTrace.Add("My.Custom.Test.Namespace.ClassD`21.MethodD(T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, Unknown)");
            stackTrace.Add("My.Custom.Test.Namespace.GenericClassC`1.GenericMethodCFromGenericClass[T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20](T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, Unknown)");
            stackTrace.Add("My.Custom.Test.Namespace.GenericClassC`1.GenericMethodCFromGenericClass(T)");

#if NET5_0_OR_GREATER
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                stackTrace.Add("Unknown_Native_Function(unknown)");
            }
#else
            stackTrace.Add("Unknown_Native_Function(unknown)");
#endif

            stackTrace.Add("My.Custom.Test.Namespace.ClassA.InternalClassB`2.DoubleInternalClassB.TripleInternalClassB`1.MethodB[TB](System.Int32, TC[], TB, TD, System.Collections.Generic.IList`1[TA], System.Collections.Generic.IList`1[System.String])");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.<MethodAOthers>g__Action|8_0[T](System.Int32)");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAOthers[T](System.String, System.Object, My.Custom.Test.Namespace.CustomClass, My.Custom.Test.Namespace.CustomStruct, My.Custom.Test.Namespace.CustomClass[], My.Custom.Test.Namespace.CustomStruct[], System.Collections.Generic.List`1[T])");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAPointer(System.Int32*)");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAFloats(System.Single, System.Double)");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodAInts(System.UInt16, System.Int16, System.UInt32, System.Int32, System.UInt64, System.Int64, System.IntPtr, System.UIntPtr)");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodABytes(System.Boolean, System.Char, System.SByte, System.Byte)");
            stackTrace.Add("My.Custom.Test.Namespace.ClassA.MethodA()");

            return stackTrace;
        }

        private static void ResourceContainsExpectedAttributes(OpenTelemetry.TestHelpers.Proto.Resource.V1.Resource resource)
        {
            var constantAttributes = new List<KeyValue>
            {
                new() { Key = "deployment.environment", Value = new AnyValue { StringValue = "integration_tests" } },
                new() { Key = "service.name", Value = new AnyValue { StringValue = "Samples.AlwaysOnProfiler" } },
                new() { Key = "telemetry.sdk.name", Value = new AnyValue { StringValue = "signalfx-dotnet-tracing" } },
                new() { Key = "telemetry.sdk.language", Value = new AnyValue { StringValue = "dotnet" } },
                new() { Key = "telemetry.sdk.version", Value = new AnyValue { StringValue = TracerConstants.AssemblyVersion } },
                new() { Key = "splunk.distro.version", Value = new AnyValue { StringValue = TracerConstants.AssemblyVersion } },
            };

            foreach (var constantAttribute in constantAttributes)
            {
                resource.Attributes.Should().ContainEquivalentOf(constantAttribute);
            }

            resource.Attributes.Should().Contain(value => value.Key == "host.name");
            resource.Attributes.Should().Contain(value => value.Key == "process.pid");
        }

        private static void HasNameAndVersionSet(InstrumentationScope instrumentationScope)
        {
            instrumentationScope.Name.Should().Be("otlp.profiles@154f871");
            instrumentationScope.Version.Should().Be("0.0.1");
        }

        private static bool ContainStackTraceForClassHierarchy(Profile profile, string expectedStackTrace)
        {
            var frames = profile.Locations
                                .SelectMany(location => location.Lines)
                                .Select(line => line.FunctionIndex)
                                .Select(functionIndex => profile.Functions[(int)functionIndex])
                                .Select(function => profile.StringTables[(int)function.NameIndex]);

            var stackTrace = string.Join("\n", frames);
            return stackTrace.Contains(expectedStackTrace);
        }

        private async Task DumpProfilesData(ExportProfilesServiceRequest[] profilesData)
        {
            foreach (var data in profilesData)
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
