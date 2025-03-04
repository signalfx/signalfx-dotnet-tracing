// <copyright file="TracerSettingsTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.Sampling;
using FluentAssertions.Execution;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class TracerSettingsTests
    {
        private readonly Mock<IAgentWriter> _writerMock;
        private readonly Mock<ISampler> _samplerMock;

        public TracerSettingsTests()
        {
            _writerMock = new Mock<IAgentWriter>();
            _samplerMock = new Mock<ISampler>();
        }

        [Theory]
        [InlineData(ConfigurationKeys.Environment, Tags.Env, null)]
        [InlineData(ConfigurationKeys.Environment, Tags.Env, "custom-env")]
        [InlineData(ConfigurationKeys.ServiceVersion, Tags.Version, null)]
        [InlineData(ConfigurationKeys.ServiceVersion, Tags.Version, "custom-version")]
        public void ConfiguredTracerSettings_DefaultTagsSetFromEnvironmentVariable(string environmentVariableKey, string tagKey, string value)
        {
            var collection = new NameValueCollection { { environmentVariableKey, value } };

            IConfigurationSource source = new NameValueConfigurationSource(collection);
            var settings = new TracerSettings(source);

            var tracer = new Tracer(settings, _writerMock.Object, _samplerMock.Object, scopeManager: null, statsd: null);
            var span = tracer.StartSpan("Operation");

            Assert.Equal(span.GetTag(tagKey), value);
        }

        [Theory]
        [InlineData(ConfigurationKeys.Environment, Tags.Env)]
        [InlineData(ConfigurationKeys.ServiceVersion, Tags.Version)]
        public void DDVarTakesPrecedenceOverDDTags(string envKey, string tagKey)
        {
            string envValue = $"ddenv-custom-{tagKey}";
            string tagsLine = $"{tagKey}:ddtags-custom-{tagKey}";
            var collection = new NameValueCollection { { envKey, envValue }, { ConfigurationKeys.GlobalTags, tagsLine } };

            IConfigurationSource source = new NameValueConfigurationSource(collection);
            var settings = new TracerSettings(source);
            Assert.True(settings.GlobalTags.Any());

            var tracer = new Tracer(settings, _writerMock.Object, _samplerMock.Object, scopeManager: null, statsd: null);
            var span = tracer.StartSpan("Operation");

            Assert.Equal(span.GetTag(tagKey), envValue);
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("1", true)]
        [InlineData("0", false)]
        public void TraceEnabled(string value, bool areTracesEnabled)
        {
            var settings = new NameValueCollection
            {
                { ConfigurationKeys.TraceEnabled, value }
            };

            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(settings));

            Assert.Equal(areTracesEnabled, tracerSettings.TraceEnabled);

            _writerMock.Invocations.Clear();

            var tracer = new Tracer(tracerSettings, _writerMock.Object, _samplerMock.Object, scopeManager: null, statsd: null);
            var span = tracer.StartSpan("TestTracerDisabled");
            span.Dispose();

            var assertion = areTracesEnabled ? Times.Once() : Times.Never();

            _writerMock.Verify(w => w.WriteTrace(It.IsAny<ArraySegment<Span>>()), assertion);
        }

        [Theory]
        [InlineData("http://localhost:7777/agent?querystring", "http://127.0.0.1:7777/agent?querystring")]
        [InlineData("http://datadog:7777/agent?querystring", "http://datadog:7777/agent?querystring")]
        public void ReplaceLocalhost(string original, string expected)
        {
            var settings = new NameValueCollection
            {
                { ConfigurationKeys.EndpointUrl, original }
            };

            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(settings));

            Assert.Equal(expected, tracerSettings.ExporterSettings.AgentUri.ToString());
        }

        [Theory]
        [InlineData("1", "1", "1", "0")]
        [InlineData("1", "1", "0", "0")]
        [InlineData("1", "0", "0", "0")]
        [InlineData("1", "0", "1", "0")]
        [InlineData("0", "0", "1", "1")]
        [InlineData("0", "1", "0", "0")]
        [InlineData("0", "0", "1", "0")]
        [InlineData(null, null, null, "1")]
        [InlineData(null, null, null, null)]
        public void EnableRuntimeMetrics(string netRuntimeEnabled, string processEnabled, string aspNetEnabled, string memoryProfilingEnabled)
        {
            var expectedNetRuntime = netRuntimeEnabled == "1" || memoryProfilingEnabled == "1";
            var expectedProcess = processEnabled == "1";
            var expectedAspNet = aspNetEnabled == "1";

            var settings = new NameValueCollection
            {
                { string.Format(ConfigurationKeys.Metrics.Enabled, MetricsIntegrationId.NetRuntime), netRuntimeEnabled },
                { string.Format(ConfigurationKeys.Metrics.Enabled, MetricsIntegrationId.Process), processEnabled },
                { string.Format(ConfigurationKeys.Metrics.Enabled, MetricsIntegrationId.AspNetCore), aspNetEnabled },
                { ConfigurationKeys.AlwaysOnProfiler.MemoryEnabled, memoryProfilingEnabled }
            };

            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(settings)).Build();

            using var scope = new AssertionScope();
            Assert.Equal(expectedNetRuntime, tracerSettings.MetricsIntegrations[MetricsIntegrationId.NetRuntime].Enabled);
            Assert.Equal(expectedProcess, tracerSettings.MetricsIntegrations[MetricsIntegrationId.Process].Enabled);
            Assert.Equal(expectedAspNet, tracerSettings.MetricsIntegrations[MetricsIntegrationId.AspNetCore].Enabled);
        }

        [Theory]
        [InlineData("1", "1", "1")]
        [InlineData("1", "1", "0")]
        [InlineData("1", "0", "0")]
        [InlineData("1", "0", "1")]
        [InlineData("0", "0", "1")]
        [InlineData("0", "1", "0")]
        public void DisableIntegrations(string graphEnabled, string kafkaEnabled, string cosmosEnabled)
        {
            var expected = graphEnabled != "0";
            var expectedKafka = kafkaEnabled != "0";
            var expectedCosmos = cosmosEnabled != "0";

            var settings = new NameValueCollection
            {
                { string.Format(ConfigurationKeys.Integrations.Enabled, IntegrationId.GraphQL), graphEnabled },
                { string.Format(ConfigurationKeys.Integrations.Enabled, IntegrationId.Kafka), kafkaEnabled },
                { string.Format(ConfigurationKeys.Integrations.Enabled, IntegrationId.CosmosDb), cosmosEnabled }
            };

            var tracerSettings = new TracerSettings(new NameValueConfigurationSource(settings)).Build();

            using var scope = new AssertionScope();
            Assert.Equal(expected, tracerSettings.Integrations[IntegrationId.GraphQL].Enabled);
            Assert.Equal(expectedKafka, tracerSettings.Integrations[IntegrationId.Kafka].Enabled);
            Assert.Equal(expectedCosmos, tracerSettings.Integrations[IntegrationId.CosmosDb].Enabled);
        }

        [Theory]
        [InlineData("a,b,c,d,,f", new[] { "a", "b", "c", "d", "f" })]
        [InlineData(" a, b ,c, ,,f ", new[] { "a", "b", "c", "f" })]
        [InlineData("a,b, c ,d,      e      ,f  ", new[] { "a", "b", "c", "d", "e", "f" })]
        [InlineData("a,b,c,d,e,f", new[] { "a", "b", "c", "d", "e", "f" })]
        [InlineData("", new string[0])]
        public void ParseStringArraySplit(string input, string[] expected)
        {
            var result = TracerSettings.TrimSplitString(input, ',').ToArray();
            Assert.Equal(expected: expected, actual: result);
        }

        [Theory]
        [InlineData("404 -401, 419,344_ 23-302, 201,_5633-55, 409-411", "401,402,403,404,419,201,409,410,411")]
        [InlineData("-33, 500-503,113#53,500-502-200,456_2, 590-590", "500,501,502,503,590")]
        [InlineData("800", "")]
        [InlineData("599-605,700-800", "599")]
        [InlineData("400-403, 500-501-234, s342, 500-503", "400,401,402,403,500,501,502,503")]
        public void ParseHttpCodes(string original, string expected)
        {
            bool[] errorStatusCodesArray = TracerSettings.ParseHttpCodesToArray(original);
            string[] expectedKeysArray = expected.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (var value in expectedKeysArray)
            {
                Assert.True(errorStatusCodesArray[int.Parse(value)]);
            }
        }

        [Fact]
        public void SetClientHttpCodes()
        {
            SetAndValidateStatusCodes((s, c) => s.SetHttpClientErrorStatusCodes(c), s => s.HttpClientErrorStatusCodes);
        }

        [Fact]
        public void SetServerHttpCodes()
        {
            SetAndValidateStatusCodes((s, c) => s.SetHttpServerErrorStatusCodes(c), s => s.HttpServerErrorStatusCodes);
        }

        private void SetAndValidateStatusCodes(Action<TracerSettings, IEnumerable<int>> setStatusCodes, Func<TracerSettings, bool[]> getStatusCodes)
        {
            var settings = new TracerSettings();
            var statusCodes = new Queue<int>(new[] { 100, 201, 503 });

            setStatusCodes(settings, statusCodes);

            var result = getStatusCodes(settings);

            for (int i = 0; i < 600; i++)
            {
                if (result[i])
                {
                    var code = statusCodes.Dequeue();

                    Assert.Equal(code, i);
                }
            }

            Assert.Empty(statusCodes);
        }
    }
}
