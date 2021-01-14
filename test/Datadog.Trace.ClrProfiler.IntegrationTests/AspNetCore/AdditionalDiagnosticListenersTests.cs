// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Datadog.Trace.ClrProfiler.IntegrationTests.Helpers;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AspNetCore
{
    public class AdditionalDiagnosticListenersTests : TestHelper
    {
        public AdditionalDiagnosticListenersTests(ITestOutputHelper output)
            : base("AdditionalDiagnosticListeners", output)
        {
            const string operationName = "api/Api";
            const string url = "/api/api";
            const string httpMethod = "GET";
            const string httpStatus = "200";
            const string resourceUrl = "/api/api";

            Expectations.Add(CreateTopLevelExpectation(operationName, url, httpMethod, httpStatus, resourceUrl, addClientIpExpectation: false));
            ExpectationsWithClientIp.Add(CreateTopLevelExpectation(operationName, url, httpMethod, httpStatus, resourceUrl, addClientIpExpectation: true));
        }

        protected HttpClient HttpClient { get; } = new HttpClient();

        protected List<AspNetCoreMvcSpanExpectation> Expectations { get; set; } = new List<AspNetCoreMvcSpanExpectation>();

        protected List<AspNetCoreMvcSpanExpectation> ExpectationsWithClientIp { get; set; } = new List<AspNetCoreMvcSpanExpectation>();

        [TargetFrameworkVersionsTheory("netcoreapp3.0;netcoreapp3.1")]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [InlineData(false)]
        [InlineData(true)]
        public void AdditionalDiagnosticListenerSpan(bool addClientIp)
        {
            var agentPort = TcpPortProvider.GetOpenPort();
            var aspNetCorePort = TcpPortProvider.GetOpenPort();
            var envVars = ZipkinEnvVars;
            envVars["SIGNALFX_INSTRUMENTATION_ASPNETCORE_DIAGNOSTIC_LISTENERS"] = "Unused,HotChocolate.Execution,Another.Unused";

            if (addClientIp)
            {
                envVars["SIGNALFX_ADD_CLIENT_IP_TO_SERVER_SPANS"] = "true";
            }

            using (var agent = new MockZipkinCollector(agentPort))
            using (var process = StartSample(agent.Port, arguments: null, packageVersion: string.Empty, aspNetCorePort: aspNetCorePort, envVars: envVars))
            {
                agent.SpanFilters.Add(IsNotServerLifeCheck);

                var wh = new EventWaitHandle(false, EventResetMode.AutoReset);

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        if (args.Data.Contains("Now listening on:") || args.Data.Contains("Unable to start Kestrel"))
                        {
                            wh.Set();
                        }

                        Output.WriteLine($"[webserver][stdout] {args.Data}");
                    }
                };
                process.BeginOutputReadLine();

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        Output.WriteLine($"[webserver][stderr] {args.Data}");
                    }
                };

                process.BeginErrorReadLine();

                wh.WaitOne(5000);

                var maxMillisecondsToWait = 15_000;
                var intervalMilliseconds = 500;
                var intervals = maxMillisecondsToWait / intervalMilliseconds;
                var serverReady = false;

                // wait for server to be ready to receive requests
                while (intervals-- > 0)
                {
                    try
                    {
                        serverReady = SubmitRequest(aspNetCorePort, "/alive-check") == HttpStatusCode.OK;
                    }
                    catch
                    {
                        // ignore
                    }

                    if (serverReady)
                    {
                        break;
                    }

                    Thread.Sleep(intervalMilliseconds);
                }

                if (!serverReady)
                {
                    throw new Exception("Couldn't verify the application is ready to receive requests.");
                }

                var testStart = DateTime.Now;

                var paths = Expectations.Select(e => e.OriginalUri).ToArray();
                SubmitRequests(aspNetCorePort, paths);

                var spans =
                    agent.WaitForSpans(
                              Expectations.Count,
                              minDateTime: testStart)
                         .OrderBy(s => s.Start)
                         .ToList();

                if (!process.HasExited)
                {
                    process.Kill();
                }

                Console.WriteLine($"Spans: {spans}");

                var expectations = addClientIp ? ExpectationsWithClientIp : Expectations;
                SpanTestHelpers.AssertExpectationsMet(expectations, spans);
            }
        }

        protected AspNetCoreMvcSpanExpectation CreateTopLevelExpectation(
            string operationName,
            string url,
            string httpMethod,
            string httpStatus,
            string resourceUrl,
            bool addClientIpExpectation,
            Func<IMockSpan, List<string>> additionalCheck = null)
        {
            var expectation = new AspNetCoreMvcSpanExpectation(EnvironmentHelper.FullSampleName, operationName, resourceUrl, httpStatus, httpMethod, addClientIpExpectation)
            {
                OriginalUri = url,
            };

            expectation.RegisterDelegateExpectation(additionalCheck);
            return expectation;
        }

        protected void SubmitRequests(int aspNetCorePort, string[] paths)
        {
            foreach (var path in paths)
            {
                SubmitRequest(aspNetCorePort, path);
            }
        }

        protected HttpStatusCode SubmitRequest(int aspNetCorePort, string path)
        {
            HttpResponseMessage response = HttpClient.GetAsync($"http://localhost:{aspNetCorePort}{path}").Result;
            string responseText = response.Content.ReadAsStringAsync().Result;
            Output.WriteLine($"[http] {response.StatusCode} {responseText}");
            return response.StatusCode;
        }

        private bool IsNotServerLifeCheck(IMockSpan span)
        {
            var url = SpanExpectation.GetTag(span, Tags.HttpUrl);
            if (url == null)
            {
                return true;
            }

            return !url.Contains("alive-check");
        }
    }
}
