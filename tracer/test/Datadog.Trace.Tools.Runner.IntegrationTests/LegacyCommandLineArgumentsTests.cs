// <copyright file="LegacyCommandLineArgumentsTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tools.Runner.IntegrationTests
{
    [CollectionDefinition(nameof(LegacyCommandLineArgumentsTests), DisableParallelization = true)]
    [Collection(nameof(LegacyCommandLineArgumentsTests))]
    public class LegacyCommandLineArgumentsTests
    {
        [Fact]
        public void InvalidArgument()
        {
            // This test makes sure that wrong arguments will return a non-zero exit code

            var exitCode = Program.Main(new[] { "--dummy-wrong-argument" });

            exitCode.Should().NotBe(0);
        }

        [Fact]
        public void Run()
        {
            string command = null;
            string arguments = null;
            Dictionary<string, string> environmentVariables = null;
            bool callbackInvoked = false;

            Program.CallbackForTests = (c, a, e) =>
            {
                command = c;
                arguments = a;
                environmentVariables = e;
                callbackInvoked = true;
            };

            // CI visibility mode checks if there's a running agent
            using var agent = new MockTracerAgent(TcpPortProvider.GetOpenPort());

            var agentUrl = $"http://localhost:{agent.Port}";

            var commandLine = $"test.exe --dd-env TestEnv --dd-service TestService --dd-version TestVersion --tracer-home TestTracerHome --agent-url {agentUrl} --ci-visibility --env-vars VAR1=A,VAR2=B";

            var exitCode = Program.Main(commandLine.Split(' '));

            exitCode.Should().Be(0);
            callbackInvoked.Should().BeTrue();

            command.Should().Be("test.exe");
            arguments.Should().BeNullOrEmpty();
            environmentVariables.Should().NotBeNull();

            environmentVariables["SIGNALFX_ENV"].Should().Be("TestEnv");
            environmentVariables["SIGNALFX_SERVICE_NAME"].Should().Be("TestService");
            environmentVariables["SIGNALFX_VERSION"].Should().Be("TestVersion");
            environmentVariables["SIGNALFX_DOTNET_TRACER_HOME"].Should().Be("TestTracerHome");
            environmentVariables["SIGNALFX_TRACE_AGENT_URL"].Should().Be(agentUrl);
            environmentVariables["SIGNALFX_CIVISIBILITY_ENABLED"].Should().Be("1");
            environmentVariables["VAR1"].Should().Be("A");
            environmentVariables["VAR2"].Should().Be("B");
        }

        [Fact]
        public void SetCi()
        {
            var tfBuild = Environment.GetEnvironmentVariable("TF_BUILD");

            var consoleWriter = Console.Out;

            try
            {
                Environment.SetEnvironmentVariable("TF_BUILD", "1");

                var output = new StringWriter();

                Console.SetOut(output);

                var commandLine = "--set-ci --dd-env TestEnv --dd-service TestService --dd-version TestVersion --tracer-home TestTracerHome --agent-url TestAgentUrl --env-vars VAR1=A,VAR2=B";

                var exitCode = Program.Main(commandLine.Split(' '));

                exitCode.Should().Be(0);

                var environmentVariables = new Dictionary<string, string>();

                foreach (var line in output.ToString().Split(Environment.NewLine))
                {
                    // ##vso[task.setvariable variable=SIGNALFX_DOTNET_TRACER_HOME]TestTracerHome
                    var match = Regex.Match(line, @"##vso\[task.setvariable variable=(?<name>[A-Z1-9_]+)\](?<value>.*)");

                    if (match.Success)
                    {
                        environmentVariables.Add(match.Groups["name"].Value, match.Groups["value"].Value);
                    }
                }

                environmentVariables["SIGNALFX_ENV"].Should().Be("TestEnv");
                environmentVariables["SIGNALFX_SERVICE_NAME"].Should().Be("TestService");
                environmentVariables["SIGNALFX_VERSION"].Should().Be("TestVersion");
                environmentVariables["SIGNALFX_DOTNET_TRACER_HOME"].Should().Be("TestTracerHome");
                environmentVariables["SIGNALFX_TRACE_AGENT_URL"].Should().Be("TestAgentUrl");
                environmentVariables["VAR1"].Should().Be("A");
                environmentVariables["VAR2"].Should().Be("B");
            }
            finally
            {
                Environment.SetEnvironmentVariable("TF_BUILD", tfBuild);
                Console.SetOut(consoleWriter);
            }
        }
    }
}