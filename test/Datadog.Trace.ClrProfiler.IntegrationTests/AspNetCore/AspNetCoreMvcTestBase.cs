// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AspNetCore
{
    public abstract class AspNetCoreMvcTestBase : TestHelper
    {
        protected static readonly string TopLevelOperationName = "aspnet_core.request";

        protected AspNetCoreMvcTestBase(string sampleAppName, ITestOutputHelper output)
            : base(sampleAppName, output)
        {
        }

        protected HttpClient HttpClient { get; } = new HttpClient();

        public void RunTraceTestOnSelfHosted(string packageVersion, bool addClientIp)
        {
            var agentPort = TcpPortProvider.GetOpenPort();
            var aspNetCorePort = TcpPortProvider.GetOpenPort();

            var expectations = CreateExpectations(addClientIp);
            var envVars = ZipkinEnvVars;
            if (addClientIp)
            {
                envVars["SIGNALFX_ADD_CLIENT_IP_TO_SERVER_SPANS"] = "1";
            }

            using (var agent = new MockZipkinCollector(agentPort))
            using (var process = StartSample(agent.Port, arguments: null, packageVersion: packageVersion, aspNetCorePort: aspNetCorePort, envVars))
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
                        serverReady = SubmitRequest(aspNetCorePort, "/alive-check").StatusCode == HttpStatusCode.OK;
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

                var paths = expectations.Select(e => e.OriginalUri).ToArray();
                var responses = SubmitRequests(aspNetCorePort, paths).ToArray();

                var spans =
                    agent.WaitForSpans(
                              expectations.Count,
                              minDateTime: testStart)
                         .OrderBy(s => s.Start)
                         .ToList();

                if (!process.HasExited)
                {
                    process.Kill();
                }

                for (var i = 0; i < spans.Count; ++i)
                {
                    try
                    {
                        if (responses[i].StatusCode == HttpStatusCode.InternalServerError)
                        {
                            // ASP.NET Core by default removes CORS headers in case of exception.
                            // In this case the Server-Timing header will be missing.
                            continue;
                        }

                        AssertServerTimingHeaders(responses[i]);
                    }
                    catch (Exception)
                    {
                        Output.WriteLine($"Exception validating Server-Timing for {spans[i].Tags["http.url"]}");
                        throw;
                    }
                }

                SpanTestHelpers.AssertExpectationsMet(expectations, spans);
            }
        }

        protected AspNetCoreMvcSpanExpectation CreateSingleExpectation(
            string operationName,
            string url,
            string httpMethod,
            string httpStatus,
            string resourceName,
            bool addClientIpExpectation = false,
            Func<IMockSpan, List<string>> additionalCheck = null)
        {
            var expectation = new AspNetCoreMvcSpanExpectation(EnvironmentHelper.FullSampleName, operationName, resourceName, httpStatus, httpMethod, addClientIpExpectation)
            {
                OriginalUri = url,
            };

            expectation.RegisterDelegateExpectation(additionalCheck);
            return expectation;
        }

        protected List<AspNetCoreMvcSpanExpectation> CreateExpectations(bool addClientIpExpectation)
        {
            return new List<AspNetCoreMvcSpanExpectation>()
            {
                CreateSingleExpectation(operationName: "home.index", url: "/", httpMethod: "GET", httpStatus: "200", resourceName: "/", addClientIpExpectation),
                CreateSingleExpectation(operationName: "delay/{seconds}", url: "/delay/0", httpMethod: "GET", httpStatus: "200", resourceName: "/delay/?", addClientIpExpectation),
                CreateSingleExpectation(operationName: "api/delay/{seconds}", url: "/api/delay/0", httpMethod: "GET", httpStatus: "200", resourceName: "/api/delay/?", addClientIpExpectation),
                CreateSingleExpectation(operationName: "/not-found", url: "/not-found", httpMethod: "GET", httpStatus: "404", resourceName: "/not-found", addClientIpExpectation),
                CreateSingleExpectation(operationName: "status-code/{statusCode}", url: "/status-code/203", httpMethod: "GET", httpStatus: "203", resourceName: "/status-code/?", addClientIpExpectation),
                CreateSingleExpectation(
                    operationName: "bad-request",
                    url: "/bad-request",
                    httpMethod: "GET",
                    httpStatus: "500",
                    resourceName: "/bad-request",
                    addClientIpExpectation,
                    additionalCheck: span =>
                    {
                        var failures = new List<string>();

                        if (span.Error == 0)
                        {
                            failures.Add($"Expected Error flag set within {span.Resource}");
                        }

                        if (SpanExpectation.GetTag(span, Tags.ErrorKind) != "System.Exception")
                        {
                            failures.Add($"Expected specific exception within {span.Resource}");
                        }

                        var errorMessage = SpanExpectation.GetTag(span, Tags.ErrorMsg);

                        if (errorMessage != "This was a bad request.")
                        {
                            failures.Add($"Expected specific error message within {span.Resource}. Found \"{errorMessage}\"");
                        }

                        return failures;
                    }),
        };
    }

        protected IEnumerable<HttpResponseMessage> SubmitRequests(int aspNetCorePort, string[] paths)
        {
            foreach (var path in paths)
            {
                yield return SubmitRequest(aspNetCorePort, path);
            }
        }

        protected HttpResponseMessage SubmitRequest(int aspNetCorePort, string path)
        {
            HttpResponseMessage response = HttpClient.GetAsync($"http://localhost:{aspNetCorePort}{path}").Result;
            string responseText = response.Content.ReadAsStringAsync().Result;
            Output.WriteLine($"[http] {response.StatusCode} {responseText}");
            return response;
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
