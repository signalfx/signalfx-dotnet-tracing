// <copyright file="HttpMessageHandlerTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Datadog.Trace.ClrProfiler.IntegrationTests.Helpers;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [CollectionDefinition(nameof(HttpMessageHandlerTests), DisableParallelization = true)]
    public class HttpMessageHandlerTests : TestHelper
    {
        public HttpMessageHandlerTests(ITestOutputHelper output)
            : base("HttpMessageHandler", output)
        {
            SetEnvironmentVariable("SIGNALFX_PROPAGATORS", "B3");
            SetEnvironmentVariable("SIGNALFX_CONVENTION", "Datadog");
            SetEnvironmentVariable("SIGNALFX_HTTP_CLIENT_ERROR_STATUSES", "400-499, 502,-343,11-53, 500-500-200");
            SetServiceVersion("1.0.0");
        }

        internal static IEnumerable<InstrumentationOptions> InstrumentationOptionsValues =>
            new List<InstrumentationOptions>
            {
                new InstrumentationOptions(instrumentSocketHandler: false, instrumentWinHttpOrCurlHandler: false),
                new InstrumentationOptions(instrumentSocketHandler: false, instrumentWinHttpOrCurlHandler: true),
                new InstrumentationOptions(instrumentSocketHandler: true, instrumentWinHttpOrCurlHandler: false),
                new InstrumentationOptions(instrumentSocketHandler: true, instrumentWinHttpOrCurlHandler: true),
            };

        public static IEnumerable<object[]> IntegrationConfig() =>
            from instrumentationOptions in InstrumentationOptionsValues
            from socketHandlerEnabled in new[] { true, false }
            select new object[] { instrumentationOptions, socketHandlerEnabled };

        [SkippableTheory]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [Trait("SupportsInstrumentationVerification", "True")]
        [MemberData(nameof(IntegrationConfig))]
        public void HttpClient_SubmitsTraces(InstrumentationOptions instrumentation, bool enableSocketsHandler)
        {
            SetInstrumentationVerification();
            ConfigureInstrumentation(instrumentation, enableSocketsHandler);

            var expectedAsyncCount = CalculateExpectedAsyncSpans(instrumentation);
            var expectedSyncCount = CalculateExpectedSyncSpans(instrumentation);

            var expectedSpanCount = expectedAsyncCount + expectedSyncCount;

            const string expectedOperationName = "http.request";
            const string expectedServiceName = "Samples.HttpMessageHandler";

            int httpPort = TcpPortProvider.GetOpenPort();
            Output.WriteLine($"Assigning port {httpPort} for the httpPort.");

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent, arguments: $"Port={httpPort}"))
            {
                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);
                Assert.Equal(expectedSpanCount, spans.Count);

                foreach (var span in spans)
                {
                    var result = span.IsHttpMessageHandler();
                    Assert.True(result.Success, result.ToString());

                    Assert.Equal(expectedServiceName, span.Service);

                    var httpStatus = span.Tags[Tags.HttpStatusCode];
                    var expectedError = httpStatus == "502" || httpStatus == "400" ? 1 : 0;
                    Assert.Equal(expectedError, span.Error);
                }

                PropagationTestHelpers.AssertPropagationEnabled(spans.First(), processResult);

                using var scope = new AssertionScope();
                VerifyInstrumentation(processResult.Process);
            }
        }

        [SkippableTheory]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [Trait("SupportsInstrumentationVerification", "True")]
        [MemberData(nameof(IntegrationConfig))]
        public void TracingDisabled_DoesNotSubmitsTraces(InstrumentationOptions instrumentation, bool enableSocketsHandler)
        {
            SetInstrumentationVerification();
            ConfigureInstrumentation(instrumentation, enableSocketsHandler);

            const string expectedOperationName = "http.request";

            int httpPort = TcpPortProvider.GetOpenPort();

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (ProcessResult processResult = RunSampleAndWaitForExit(agent, arguments: $"TracingDisabled Port={httpPort}"))
            {
                var spans = agent.WaitForSpans(1, 2000, operationName: expectedOperationName);
                Assert.Equal(0, spans.Count);

                PropagationTestHelpers.AssertPropagationDisabled(processResult);

                using var scope = new AssertionScope();
                // ignore auto enabled for simplicity
                VerifyInstrumentation(processResult.Process);
            }
        }

        private static int CalculateExpectedAsyncSpans(InstrumentationOptions instrumentation)
        {
            // net4x doesn't have patch
            var spansPerHttpClient = EnvironmentHelper.IsCoreClr() ? 35 : 31;

            spansPerHttpClient++; // SignalFx adds Send.Get.HttpClientErrorCode span

            var expectedSpanCount = spansPerHttpClient * 2; // default HttpClient and CustomHttpClientHandler

            if (IsUsingWinHttpHandler(instrumentation))
            {
                expectedSpanCount += spansPerHttpClient;
            }

            if (IsUsingSocketHandler(instrumentation))
            {
                expectedSpanCount += spansPerHttpClient;
            }

#if NETCOREAPP2_1 || NETCOREAPP3_0 || NETCOREAPP3_1
            if (instrumentation.InstrumentWinHttpOrCurlHandler == true)
            {
                // Add 1 span for internal WinHttpHandler and CurlHandler using the HttpMessageInvoker
                expectedSpanCount++;
            }
#endif

            return expectedSpanCount;
        }

        private static bool IsSocketsHandlerSupported() => EnvironmentHelper.IsCoreClr();

        private static bool IsWinHttpHandlerSupported()
            => RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

        private static bool IsCurlHandlerSupported()
        {
#if NETCOREAPP2_1 || NETCOREAPP3_0 || NETCOREAPP3_1
            return !RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#else
            return false;
#endif
        }

        private static bool IsUsingWinHttpHandler(InstrumentationOptions instrumentation)
        {
            // WinHttpHandler instrumentation is off by default, and only available on Windows
            return IsWinHttpHandlerSupported() && (instrumentation.InstrumentWinHttpOrCurlHandler == true);
        }

        private static bool IsUsingCurlHandler(InstrumentationOptions instrumentation)
        {
            return IsCurlHandlerSupported() && (instrumentation.InstrumentWinHttpOrCurlHandler == true);
        }

        private static bool IsUsingSocketHandler(InstrumentationOptions instrumentation)
        {
            // SocketsHttpHandler instrumentation is on by default
            return IsSocketsHandlerSupported() && (instrumentation.InstrumentSocketHandler ?? true);
        }

        private static int CalculateExpectedSyncSpans(InstrumentationOptions instrumentation)
        {
            // Sync requests are only enabled in .NET 5
            if (!EnvironmentHelper.IsNet5())
            {
                return 0;
            }

            var spansPerHttpClient = 21;

            spansPerHttpClient++; // SignalFx adds Send.Get.HttpClientErrorCode span

            var expectedSpanCount = spansPerHttpClient * 2; // default HttpClient and CustomHttpClientHandler

            // SocketsHttpHandler instrumentation is on by default
            if (instrumentation.InstrumentSocketHandler ?? true)
            {
                expectedSpanCount += spansPerHttpClient;
            }

            return expectedSpanCount;
        }

        private void ConfigureInstrumentation(InstrumentationOptions instrumentation, bool enableSocketsHandler)
        {
            // Should HttpClient try to use HttpSocketsHandler
            SetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER", enableSocketsHandler ? "1" : "0");

            // Enable specific integrations, or use defaults
            if (instrumentation.InstrumentSocketHandler.HasValue)
            {
                SetEnvironmentVariable("SIGNALFX_HttpSocketsHandler_ENABLED", instrumentation.InstrumentSocketHandler.Value ? "true" : "false");
            }

            if (instrumentation.InstrumentWinHttpOrCurlHandler.HasValue)
            {
                SetEnvironmentVariable("SIGNALFX_WinHttpHandler_ENABLED", instrumentation.InstrumentWinHttpOrCurlHandler.Value ? "true" : "false");
                SetEnvironmentVariable("SIGNALFX_CurlHandler_ENABLED", instrumentation.InstrumentWinHttpOrCurlHandler.Value ? "true" : "false");
            }
        }

        public class InstrumentationOptions : IXunitSerializable
        {
            // ReSharper disable once UnusedMember.Global
            public InstrumentationOptions()
            {
            }

            internal InstrumentationOptions(
                bool? instrumentSocketHandler,
                bool? instrumentWinHttpOrCurlHandler)
            {
                InstrumentSocketHandler = instrumentSocketHandler;
                InstrumentWinHttpOrCurlHandler = instrumentWinHttpOrCurlHandler;
            }

            internal bool? InstrumentSocketHandler { get; private set; }

            internal bool? InstrumentWinHttpOrCurlHandler { get; private set; }

            public void Deserialize(IXunitSerializationInfo info)
            {
                InstrumentSocketHandler = info.GetValue<bool?>(nameof(InstrumentSocketHandler));
                InstrumentWinHttpOrCurlHandler = info.GetValue<bool?>(nameof(InstrumentWinHttpOrCurlHandler));
            }

            public void Serialize(IXunitSerializationInfo info)
            {
                info.AddValue(nameof(InstrumentSocketHandler), InstrumentSocketHandler);
                info.AddValue(nameof(InstrumentWinHttpOrCurlHandler), InstrumentWinHttpOrCurlHandler);
            }

            public override string ToString() =>
                $"InstrumentSocketHandler={InstrumentSocketHandler},InstrumentWinHttpOrCurlHandler={InstrumentWinHttpOrCurlHandler}";
        }
    }
}
