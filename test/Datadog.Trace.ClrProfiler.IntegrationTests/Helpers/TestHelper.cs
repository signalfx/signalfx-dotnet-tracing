// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Core.Tools;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public abstract class TestHelper
    {
        protected TestHelper(string sampleAppName, string samplePathOverrides, ITestOutputHelper output)
            : this(new EnvironmentHelper(sampleAppName, typeof(TestHelper), output, samplePathOverrides), output)
        {
        }

        protected TestHelper(string sampleAppName, ITestOutputHelper output)
            : this(new EnvironmentHelper(sampleAppName, typeof(TestHelper), output), output)
        {
        }

        protected TestHelper(EnvironmentHelper environmentHelper, ITestOutputHelper output)
        {
            EnvironmentHelper = environmentHelper;
            SampleAppName = EnvironmentHelper.SampleName;
            Output = output;

            PathToSample = EnvironmentHelper.GetSampleApplicationOutputDirectory();
            Output.WriteLine($"Platform: {EnvironmentTools.GetPlatform()}");
            Output.WriteLine($"Configuration: {EnvironmentTools.GetBuildConfiguration()}");
            Output.WriteLine($"TargetFramework: {EnvironmentHelper.GetTargetFramework()}");
            Output.WriteLine($".NET Core: {EnvironmentHelper.IsCoreClr()}");
            Output.WriteLine($"Application: {GetSampleApplicationPath()}");
            Output.WriteLine($"Profiler DLL: {EnvironmentHelper.GetProfilerPath()}");
        }

        public Dictionary<string, string> ZipkinEnvVars
        {
            get => new Dictionary<string, string>() { { "SIGNALFX_API_TYPE", "zipkin" } };
        }

        protected EnvironmentHelper EnvironmentHelper { get; set; }

        protected string TestPrefix => $"{EnvironmentTools.GetBuildConfiguration()}.{EnvironmentHelper.GetTargetFramework()}";

        protected string SampleAppName { get; }

        protected string PathToSample { get; }

        protected ITestOutputHelper Output { get; }

        public string GetSampleApplicationPath(string packageVersion = "")
        {
            return EnvironmentHelper.GetSampleApplicationPath(packageVersion);
        }

        public Process StartSample(int traceAgentPort, string arguments, string packageVersion, int aspNetCorePort, Dictionary<string, string> envVars = null)
        {
            // get path to sample app that the profiler will attach to
            string sampleAppPath = GetSampleApplicationPath(packageVersion);
            if (!File.Exists(sampleAppPath))
            {
                throw new Exception($"application not found: {sampleAppPath}");
            }

            // get full paths to integration definitions
            IEnumerable<string> integrationPaths = Directory.EnumerateFiles(".", "*integrations.json").Select(Path.GetFullPath);

            return ProfilerHelper.StartProcessWithProfiler(
                EnvironmentHelper,
                integrationPaths,
                arguments,
                traceAgentPort: traceAgentPort,
                aspNetCorePort: aspNetCorePort,
                envVars: envVars);
        }

        public ProcessResult RunSampleAndWaitForExit(int traceAgentPort, string arguments = null, string packageVersion = "", Dictionary<string, string> envVars = null, TimeSpan timeout = default)
        {
            if (timeout == default)
            {
                timeout = TimeSpan.FromMinutes(1);
            }

            Process process = StartSample(traceAgentPort, arguments, packageVersion, aspNetCorePort: 5000, envVars);
            var stdout = new StringBuilder();
            process.OutputDataReceived += (sender, args) => { lock (stdout) { stdout.AppendLine(args.Data); } };
            process.BeginOutputReadLine();
            var stderr = new StringBuilder();
            process.ErrorDataReceived += (sender, args) => { lock (stderr) { stderr.AppendLine(args.Data); } };
            process.BeginErrorReadLine();

            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                Output.WriteLine($"Timeout after {timeout}");
                process.Kill();
                process.WaitForExit();
            }

            string standardOutput = null;
            lock (stdout) { standardOutput = stdout.ToString(); }
            if (!string.IsNullOrWhiteSpace(standardOutput))
            {
                Output.WriteLine($"StandardOutput:{Environment.NewLine}{standardOutput}");
            }

            string standardError = null;
            lock (stderr) { standardError = stderr.ToString(); }
            if (!string.IsNullOrWhiteSpace(standardError))
            {
                Output.WriteLine($"StandardError:{Environment.NewLine}{standardError}");
            }

            return new ProcessResult(process, standardOutput, standardError, process.ExitCode);
        }

        public Process StartIISExpress(int traceAgentPort, int iisPort, bool addClientIp)
        {
            // get full paths to integration definitions
            IEnumerable<string> integrationPaths = Directory.EnumerateFiles(".", "*integrations.json").Select(Path.GetFullPath);

            var exe = EnvironmentHelper.GetSampleExecutionSource();
            var args = new string[]
                {
                    $"/clr:v4.0",
                    $"/path:{EnvironmentHelper.GetSampleProjectDirectory()}",
                    $"/systray:false",
                    $"/port:{iisPort}",
                    $"/trace:info",
                };

            Output.WriteLine($"[webserver] starting {exe} {string.Join(" ", args)}");

            var envVars = new Dictionary<string, string>();
            if (addClientIp)
            {
                envVars["SIGNALFX_ADD_CLIENT_IP_TO_SERVER_SPANS"] = "true";
            }

            var process = ProfilerHelper.StartProcessWithProfiler(
                EnvironmentHelper,
                integrationPaths,
                arguments: string.Join(" ", args),
                redirectStandardInput: true,
                traceAgentPort: traceAgentPort,
                envVars: envVars);

            var wh = new EventWaitHandle(false, EventResetMode.AutoReset);

            Task.Run(() =>
            {
                string line;
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    Output.WriteLine($"[webserver][stdout] {line}");

                    if (line.Contains("IIS Express is running"))
                    {
                        wh.Set();
                    }
                }
            });

            Task.Run(() =>
            {
                string line;
                while ((line = process.StandardError.ReadLine()) != null)
                {
                    Output.WriteLine($"[webserver][stderr] {line}");
                }
            });

            wh.WaitOne(5000);

            // Wait for iis express to finish starting up
            var retries = 5;
            while (true)
            {
                var usedPorts = IPGlobalProperties.GetIPGlobalProperties()
                                                  .GetActiveTcpListeners()
                                                  .Select(ipEndPoint => ipEndPoint.Port);

                if (usedPorts.Contains(iisPort))
                {
                    break;
                }

                retries--;

                if (retries == 0)
                {
                    throw new Exception("Gave up waiting for IIS Express.");
                }

                Thread.Sleep(1500);
            }

            return process;
        }

        protected static void ValidateSpans<T>(IEnumerable<IMockSpan> spans, Func<IMockSpan, T> mapper, IEnumerable<T> expected)
        {
            var spanLookup = new Dictionary<T, int>();
            foreach (var span in spans)
            {
                var key = mapper(span);
                if (spanLookup.ContainsKey(key))
                {
                    spanLookup[key]++;
                }
                else
                {
                    spanLookup[key] = 1;
                }
            }

            var missing = new List<T>();
            foreach (var e in expected)
            {
                var found = spanLookup.ContainsKey(e);
                if (found)
                {
                    if (--spanLookup[e] <= 0)
                    {
                        spanLookup.Remove(e);
                    }
                }
                else
                {
                    missing.Add(e);
                }
            }

            if (missing.Count == 0)
            {
                return;
            }

            var errorMessage = $"No spans found for:\n{string.Join("\n", missing)}\n" +
                               $"Remaining spans:\n{string.Join("\n", spanLookup.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}";
            Assert.True(condition: false, errorMessage);
        }

        protected void EnableDebugMode()
        {
            EnvironmentHelper.DebugModeEnabled = true;
        }

        protected void SetEnvironmentVariable(string key, string value)
        {
            EnvironmentHelper.CustomEnvironmentVariables.Add(key, value);
        }

        protected async Task AssertHttpSpan(
            string path,
            MockZipkinCollector agent,
            int httpPort,
            HttpStatusCode expectedHttpStatusCode,
            string expectedSpanType,
            string expectedOperationName,
            string expectedResourceName,
            bool expectClientIp)
        {
            IImmutableList<IMockSpan> spans;

            using (var httpClient = new HttpClient())
            {
                // disable tracing for this HttpClient request
                httpClient.DefaultRequestHeaders.Add(HttpHeaderNames.TracingEnabled, "false");
                var testStart = DateTime.UtcNow;
                var response = await httpClient.GetAsync($"http://localhost:{httpPort}" + path);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Assert.True(
                        response.Headers.Contains("Server-Timing"),
                        $"No Server-Timing header attached. Headers present: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={h.Value}"))}");
                    Assert.True(
                        response.Headers.Contains("Access-Control-Expose-Headers"),
                        $"No Access-Control-Expose-Headers header attached. Headers present: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={h.Value}"))}");
                }

                var content = await response.Content.ReadAsStringAsync();
                Output.WriteLine($"[http] {response.StatusCode} {content}");
                Assert.Equal(expectedHttpStatusCode, response.StatusCode);

                spans = agent.WaitForSpans(
                    count: 1,
                    minDateTime: testStart,
                    operationName: expectedOperationName);

                Assert.True(spans.Count == 1, "expected one span");
            }

            IMockSpan span = spans[0];
            Assert.Equal(expectedSpanType, span.Type);
            Assert.Equal(expectedOperationName, span.Name);
            Assert.Equal(expectedResourceName, span.Resource);
            Assert.Equal(SpanKinds.Server, span.Tags[Tags.SpanKind]);

            if (expectClientIp)
            {
                Assert.Contains(span.Tags, kvp => kvp.Key == "peer.ipv4" || kvp.Key == "peer.ipv6");
            }
            else
            {
                Assert.DoesNotContain(span.Tags, kvp => kvp.Key == "peer.ipv4" && kvp.Key == "peer.ipv6");
            }
        }

        internal class TupleList<T1, T2> : List<Tuple<T1, T2>>
        {
            public void Add(T1 item, T2 item2)
            {
                Add(new Tuple<T1, T2>(item, item2));
            }
        }
    }
}
