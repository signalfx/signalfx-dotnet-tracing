// Modified by Splunk Inc.

// Thread Sampling is not supported by .NET Framework and lower versions of .NET Core

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using OpenTelemetry.TestHelpers.Proto.Collector.Logs.V1;
using OpenTelemetry.TestHelpers.Proto.Common.V1;
using OpenTelemetry.TestHelpers.Proto.Logs.V1;
using OpenTelemetry.TestHelpers.Proto.Resource.V1;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class AlwaysOnProfilerTests : TestHelper
    {
        protected AlwaysOnProfilerTests(ITestOutputHelper output)
            : base("AlwaysOnProfiler", output)
        {
            SetServiceVersion("1.0.0");
        }

        protected static void AllShouldHaveBasicAttributes(List<LogRecord> logRecords, List<KeyValue> attributes)
        {
            foreach (var logRecord in logRecords)
            {
                foreach (var attribute in attributes)
                {
                    logRecord.Attributes.Should().ContainEquivalentOf(attribute);
                }
            }
        }

        protected static IEnumerable<string> CreateExpectedStackTrace()
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

        protected static void ResourceContainsExpectedAttributes(Resource resource)
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

        protected static void HasNameAndVersionSet(InstrumentationLibrary instrumentationLibrary)
        {
            instrumentationLibrary.Name.Should().Be("otel.profiling");
            instrumentationLibrary.Version.Should().Be("0.1.0");
        }

        protected async Task DumpLogRecords(ExportLogsServiceRequest[] logsData)
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
