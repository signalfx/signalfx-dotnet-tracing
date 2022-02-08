// Modified by Splunk Inc.

using System;
using System.Collections.Specialized;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using FluentAssertions;
using Xunit;
using MetricsTransportType = Datadog.Trace.Vendors.StatsdClient.Transport.TransportType;

namespace Datadog.Trace.Tests.Configuration;

public class ExporterSettingsTests
{
    [Fact]
    public void If_there_is_no_trace_endpoint_configuration_then_local_traces_collector_is_used()
    {
        var collection = new NameValueCollection();
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.AgentUri.Should().Be("http://127.0.0.1:9411/api/v2/spans");
    }

    [Fact]
    public void If_there_is_no_metrics_endpoint_configuration_then_local_metrics_collector_is_used()
    {
        var collection = new NameValueCollection();
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.MetricsEndpointUrl.Should().Be("http://localhost:9943/v2/datapoint");
    }

    [Fact]
    public void If_nondefault_signalfx_realm_is_configured_then_direct_ingest_endpoint_for_traces_is_used()
    {
        var collection = new NameValueCollection { { "SIGNALFX_REALM", "test-realm" } };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.AgentUri.Should().Be("https://ingest.test-realm.signalfx.com/v2/trace");
    }

    [Fact]
    public void If_nondefault_signalfx_realm_is_configured_then_direct_ingest_endpoint_for_metrics_is_used()
    {
        var collection = new NameValueCollection { { "SIGNALFX_REALM", "test-realm" } };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.MetricsEndpointUrl.Should().Be("https://ingest.test-realm.signalfx.com/v2/datapoint");
    }

    [Theory]
    [InlineData("none")]
    [InlineData("NONE")]
    public void If_default_signalfx_realm_is_configured_then_local_traces_collector_is_used(string configuredRealm)
    {
        var collection = new NameValueCollection { { "SIGNALFX_REALM", configuredRealm } };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.AgentUri.Should().Be("http://127.0.0.1:9411/api/v2/spans");
    }

    [Theory]
    [InlineData("none")]
    [InlineData("NONE")]
    public void If_default_signalfx_realm_is_configured_to_none_then_local_metrics_collector_is_used(string configuredRealm)
    {
        var collection = new NameValueCollection { { "SIGNALFX_REALM", configuredRealm } };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.MetricsEndpointUrl.Should().Be("http://localhost:9943/v2/datapoint");
    }

    [Theory]
    [InlineData("http://127.0.0.1:9411/api/v2/spans")]
    [InlineData("https://remotehost/v2/trace")]
    public void If_traces_endpoint_is_configured_then_its_used_as_provided(string configuredEndpoint)
    {
        var collection = new NameValueCollection { { "SIGNALFX_ENDPOINT_URL", configuredEndpoint } };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.AgentUri.Should().Be(configuredEndpoint);
    }

    [Theory]
    [InlineData("http://127.0.0.1:9943/v2/datapoint")]
    [InlineData("https://remotehost/v2/datapoint")]
    public void If_metrics_endpoint_is_configured_then_its_used_as_provided(string configuredEndpoint)
    {
        var collection = new NameValueCollection { { "SIGNALFX_METRICS_ENDPOINT_URL", configuredEndpoint } };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.MetricsEndpointUrl.Should().Be(configuredEndpoint);
    }

    [Fact]
    public void Endpoint_url_takes_precedence_over_signalfx_realm_for_traces()
    {
        var collection = new NameValueCollection
        {
            { "SIGNALFX_REALM", "test-realm" },
            { "SIGNALFX_ENDPOINT_URL", "http://127.0.0.1:9411/api/v2/spans" }
        };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.AgentUri.Should().Be("http://127.0.0.1:9411/api/v2/spans");
    }

    [Fact]
    public void Endpoint_url_takes_precedence_over_signalfx_realm_for_metrics()
    {
        var collection = new NameValueCollection
        {
            { "SIGNALFX_REALM", "test-realm" },
            { "SIGNALFX_METRICS_ENDPOINT_URL", "http://127.0.0.1:9943/v2/datapoint" }
        };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.MetricsEndpointUrl.Should().Be("http://127.0.0.1:9943/v2/datapoint");
    }

    [Fact]
    public void NoSocketFiles_NoExplicitConfiguration_DefaultsMatchExpectation()
    {
        var config = Setup();
        Assert.Equal(expected: TracesTransportType.Default, actual: config.TracesTransport);
        Assert.Equal(expected: MetricsTransportType.UDP, actual: config.MetricsTransport);
        Assert.Equal(expected: new Uri("http://127.0.0.1:9411/api/v2/spans"), actual: config.AgentUri);
        Assert.Equal(expected: 9943, actual: config.DogStatsdPort);
        Assert.False(config.PartialFlushEnabled);
        Assert.Equal(expected: 500, actual: config.PartialFlushMinSpans);
    }

    [Fact]
    public void PartialFlushVariables_Populated()
    {
        var config = Setup("SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED-true", "SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS-999");
        Assert.True(config.PartialFlushEnabled);
        Assert.Equal(expected: 999, actual: config.PartialFlushMinSpans);
    }

    [Fact]
    public void Traces_SocketFilesExist_ExplicitAgentHost_UsesDefaultTcp()
    {
        var expectedUri = new Uri("http://127.0.0.1:9411/api/v2/spans");
        var config = Setup("SIGNALFX_ENDPOINT_URL-http://127.0.0.1:9411/api/v2/spans");
        Assert.Equal(expected: TracesTransportType.Default, actual: config.TracesTransport);
        Assert.Equal(expected: expectedUri, actual: config.AgentUri);
    }

    [Fact]
    public void Traces_SocketFilesExist_ExplicitTraceAgentPort_UsesDefaultTcp()
    {
        var expectedUri = new Uri("http://127.0.0.1:8111/api/v2/spans");
        var config = Setup("SIGNALFX_TRACE_AGENT_PORT-8111");
        Assert.Equal(expected: TracesTransportType.Default, actual: config.TracesTransport);
        Assert.Equal(expected: expectedUri, actual: config.AgentUri);
    }

    [Fact]
    public void Traces_SocketFilesExist_ExplicitWindowsPipeConfig_UsesWindowsNamedPipe()
    {
        var config = Setup("SIGNALFX_TRACE_PIPE_NAME-somepipe");
        Assert.Equal(expected: TracesTransportType.WindowsNamedPipe, actual: config.TracesTransport);
        Assert.Equal(expected: "somepipe", actual: config.TracesPipeName);
    }

    /// <summary>
    /// This test is not actually important for functionality, it is just to document existing behavior.
    /// If for some reason the priority needs to change in the future, there is no compelling reason why this test can't change.
    /// </summary>
    [Fact]
    public void Traces_SocketFilesExist_ExplicitConfigForWindowsPipeAndUdp_PrioritizesWindowsPipe()
    {
        var config = Setup("SIGNALFX_TRACE_PIPE_NAME-somepipe", "SIGNALFX_APM_RECEIVER_SOCKET-somesocket");
        Assert.Equal(expected: TracesTransportType.WindowsNamedPipe, actual: config.TracesTransport);
    }

    [Fact]
    public void Traces_SocketFilesExist_ExplicitConfigForAll_UsesDefaultTcp()
    {
        var config = Setup("SIGNALFX_ENDPOINT_URL-http://127.0.0.1:9411/api/v2/spans", "SIGNALFX_TRACE_PIPE_NAME-somepipe", "SIGNALFX_APM_RECEIVER_SOCKET-somesocket");
        Assert.Equal(expected: TracesTransportType.Default, actual: config.TracesTransport);
    }

    [Fact]
    public void Metrics_SocketFilesExist_ExplicitMetricsPort_UsesUdp()
    {
        var expectedPort = 11125;
        var config = Setup("SIGNALFX_DOGSTATSD_PORT-11125");
        Assert.Equal(expected: MetricsTransportType.UDP, actual: config.MetricsTransport);
        Assert.Equal(expected: expectedPort, actual: config.DogStatsdPort);
    }

    [Fact]
    public void Metrics_SocketFilesExist_ExplicitWindowsPipeConfig_UsesWindowsNamedPipe()
    {
        var config = Setup("SIGNALFX_DOGSTATSD_PIPE_NAME-somepipe");
        Assert.Equal(expected: MetricsTransportType.NamedPipe, actual: config.MetricsTransport);
        Assert.Equal(expected: "somepipe", actual: config.MetricsPipeName);
    }

    [Fact]
    public void Metrics_SocketFilesExist_ExplicitConfigForAll_UsesDefaultTcp()
    {
        var config = Setup("SIGNALFX_ENDPOINT_URL-http://localhost:100", "SIGNALFX_DOGSTATSD_PIPE_NAME-somepipe", "SIGNALFX_DOGSTATSD_SOCKET-somesocket");
        Assert.Equal(expected: TracesTransportType.Default, actual: config.TracesTransport);
    }

    private ExporterSettings Setup(params string[] config)
    {
        var configNameValues = new NameValueCollection();

        foreach (var item in config)
        {
            var parts = item.Split('-');
            configNameValues.Add(parts[0], parts[1]);
        }

        var configSource = new NameValueConfigurationSource(configNameValues);

        var exporterSettings = new ExporterSettings(configSource);

        return exporterSettings;
    }
}
