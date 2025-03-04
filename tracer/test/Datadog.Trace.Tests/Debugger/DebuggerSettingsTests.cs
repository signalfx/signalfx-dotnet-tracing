// <copyright file="DebuggerSettingsTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.Configuration;
using Datadog.Trace.Debugger;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.Debugger
{
    public class DebuggerSettingsTests
    {
        [Fact]
        public void WhenFilePathProvided_UseFileMode()
        {
            var expected = "c:/temp";
            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(new()
            {
                { ConfigurationKeys.Debugger.ProbeFile, expected },
            }));

            tracerSettings.DebuggerSettings.ProbeMode.Should().Be(ProbeMode.File);
            tracerSettings.DebuggerSettings.ProbeConfigurationsPath.Should().Be(expected);
            tracerSettings.DebuggerSettings.SnapshotsPath.Should().Be("http://127.0.0.1:9411/api/v2/spans");
        }

        [Fact]
        public void DefaultMode_AgentMode()
        {
            var tracerSettings = new TracerSettings();
            tracerSettings.DebuggerSettings.ProbeMode.Should().Be(ProbeMode.Agent);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("http://127.0.0.1:9411/api/v2/spans")]
        public void WhenAgentModeEnabled_UseAgentMode(string agentUri)
        {
            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(new()
            {
                { ConfigurationKeys.EndpointUrl, agentUri },
            }));

            tracerSettings.DebuggerSettings.ProbeMode.Should().Be(ProbeMode.Agent);
            tracerSettings.DebuggerSettings.ProbeConfigurationsPath.Should().Be("http://127.0.0.1:9411/api/v2/spans");
            tracerSettings.DebuggerSettings.SnapshotsPath.Should().Be("http://127.0.0.1:9411/api/v2/spans");
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("0")]
        [InlineData("")]
        [InlineData(null)]
        public void InvalidPollInterval_DefaultUsed(string value)
        {
            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(new()
            {
                { ConfigurationKeys.Debugger.PollInterval, value },
            }));

            tracerSettings.DebuggerSettings.ProbeConfigurationsPollIntervalSeconds.Should().Be(1);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("0")]
        [InlineData("")]
        [InlineData(null)]
        public void InvalidMaxDepthToSerialize_DefaultUsed(string value)
        {
            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(new()
            {
                { ConfigurationKeys.Debugger.MaxDepthToSerialize, value },
            }));

            tracerSettings.DebuggerSettings.MaximumDepthOfMembersToCopy.Should().Be(1);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("0")]
        [InlineData("")]
        [InlineData(null)]
        public void InvalidSerializationTimeThreshold_DefaultUsed(string value)
        {
            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(new()
            {
                { ConfigurationKeys.Debugger.MaxTimeToSerialize, value },
            }));

            tracerSettings.DebuggerSettings.MaxSerializationTimeInMilliseconds.Should().Be(150);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("false")]
        public void DebuggerDisabled(string enabled)
        {
            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(new()
            {
                { ConfigurationKeys.Debugger.Enabled, enabled },
            }));

            tracerSettings.DebuggerSettings.Enabled.Should().BeFalse();
        }

        [Fact]
        public void DebuggerSettings_UseSettings()
        {
            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(new()
            {
                { ConfigurationKeys.Debugger.Enabled, "true" },
                { ConfigurationKeys.Debugger.PollInterval, "10" },
                { ConfigurationKeys.Debugger.MaxDepthToSerialize, "100" },
                { ConfigurationKeys.Debugger.MaxTimeToSerialize, "1000" },
            }));

            tracerSettings.DebuggerSettings.Enabled.Should().BeTrue();
            tracerSettings.DebuggerSettings.ProbeConfigurationsPollIntervalSeconds.Should().Be(10);
            tracerSettings.DebuggerSettings.MaximumDepthOfMembersToCopy.Should().Be(100);
            tracerSettings.DebuggerSettings.MaxSerializationTimeInMilliseconds.Should().Be(1000);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("0")]
        [InlineData("")]
        [InlineData(null)]
        public void InvalidUploadBatchSize_DefaultUsed(string value)
        {
            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(new()
            {
                { ConfigurationKeys.Debugger.UploadBatchSize, value },
            }));

            tracerSettings.DebuggerSettings.UploadBatchSize.Should().Be(100);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("0")]
        [InlineData("")]
        [InlineData(null)]
        public void InvalidDiagnosticsInterval_DefaultUsed(string value)
        {
            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(new()
            {
                { ConfigurationKeys.Debugger.DiagnosticsInterval, value },
            }));

            tracerSettings.DebuggerSettings.DiagnosticsIntervalSeconds.Should().Be(3600);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("0")]
        [InlineData("")]
        [InlineData(null)]
        public void InvalidUploadFlushInterval_DefaultUsed(string value)
        {
            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(new()
            {
                { ConfigurationKeys.Debugger.UploadFlushInterval, value },
            }));

            tracerSettings.DebuggerSettings.UploadFlushIntervalMilliseconds.Should().Be(0);
        }
    }
}
