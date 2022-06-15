// Modified by Splunk Inc.

using System;
using System.Collections.Specialized;
using System.Linq;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.SignalFx.Metrics;
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
        var collection = new NameValueCollection { { "SIGNALFX_REALM", "test-realm" }, { "SIGNALFX_ENDPOINT_URL", "http://127.0.0.1:9411/api/v2/spans" } };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.AgentUri.Should().Be("http://127.0.0.1:9411/api/v2/spans");
    }

    [Fact]
    public void Endpoint_url_takes_precedence_over_signalfx_realm_for_metrics()
    {
        var collection = new NameValueCollection { { "SIGNALFX_REALM", "test-realm" }, { "SIGNALFX_METRICS_ENDPOINT_URL", "http://127.0.0.1:9943/v2/datapoint" } };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.MetricsEndpointUrl.Should().Be("http://127.0.0.1:9943/v2/datapoint");
    }

    [Fact]
    public void NoSocketFiles_NoExplicitConfiguration_DefaultsMatchExpectation()
    {
        var settings = Setup("SIGNALFX_METRICS_EXPORTER-StatsD");
        Assert.Equal(expected: TracesTransportType.Default, actual: settings.TracesTransport);
        Assert.Equal(expected: MetricsTransportType.UDP, actual: settings.MetricsTransport);
        Assert.Equal(expected: new Uri("http://127.0.0.1:9411/api/v2/spans"), actual: settings.AgentUri);
        Assert.Equal(expected: 9943, actual: settings.DogStatsdPort);
        Assert.False(settings.PartialFlushEnabled);
        Assert.Equal(expected: 500, actual: settings.PartialFlushMinSpans);
    }

    [Fact]
    public void PartialFlushVariables_Populated()
    {
        var settings = Setup("SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED-true", "SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS-999");
        Assert.True(settings.PartialFlushEnabled);
        Assert.Equal(expected: 999, actual: settings.PartialFlushMinSpans);
    }

    [Fact]
    public void Traces_SocketFilesExist_ExplicitTraceAgentPort_UsesDefaultTcp()
    {
        var expectedUri = new Uri($"http://127.0.0.1:8111/api/v2/spans");
        var settings = Setup("SIGNALFX_TRACE_AGENT_PORT-8111");
        AssertHttpIsConfigured(settings, expectedUri);
    }

    [Fact]
    public void Traces_SocketFilesExist_ExplicitWindowsPipeConfig_UsesWindowsNamedPipe()
    {
        var settings = Setup("SIGNALFX_TRACE_PIPE_NAME-somepipe");
        AssertPipeIsConfigured(settings, "somepipe");
    }

    /// <summary>
    /// This test is not actually important for functionality, it is just to document existing behavior.
    /// If for some reason the priority needs to change in the future, there is no compelling reason why this test can't change.
    /// </summary>
    [Fact]
    public void Traces_SocketFilesExist_ExplicitConfigForWindowsPipeAndUdp_PrioritizesWindowsPipe()
    {
        var settings = Setup("SIGNALFX_TRACE_PIPE_NAME-somepipe", "SIGNALFX_APM_RECEIVER_SOCKET-somesocket");
        AssertPipeIsConfigured(settings, "somepipe");
    }

    [Fact]
    public void Traces_SocketFilesExist_ExplicitConfigForAll_UsesDefaultTcp()
    {
        var settings = Setup("SIGNALFX_ENDPOINT_URL-http://127.0.0.1:9411/api/v2/spans", "SIGNALFX_TRACE_PIPE_NAME-somepipe", "SIGNALFX_APM_RECEIVER_SOCKET-somesocket");
        Assert.Equal(expected: TracesTransportType.Default, actual: settings.TracesTransport);
    }

    [Fact]
    public void Metrics_SocketFilesExist_ExplicitMetricsPort_UsesUdp()
    {
        var expectedPort = 11125;
        var settings = Setup("SIGNALFX_DOGSTATSD_PORT-11125", "SIGNALFX_METRICS_EXPORTER-StatsD");
        Assert.Equal(expected: MetricsTransportType.UDP, actual: settings.MetricsTransport);
        Assert.Equal(expected: expectedPort, actual: settings.DogStatsdPort);
    }

    [Fact]
    public void Metrics_SocketFilesExist_ExplicitWindowsPipeConfig_UsesWindowsNamedPipe()
    {
        var settings = Setup("SIGNALFX_DOGSTATSD_PIPE_NAME-somepipe", "SIGNALFX_METRICS_EXPORTER-StatsD");
        AssertMetricsPipeIsConfigured(settings, "somepipe");
    }

    [Fact]
    public void Metrics_SocketFilesExist_ExplicitConfigForAll_UsesDefaultTcp()
    {
        var settings = Setup("SIGNALFX_DOGSTATSD_PIPE_NAME-somepipe", "SIGNALFX_DOGSTATSD_SOCKET-somesocket");
        Assert.Equal(expected: TracesTransportType.Default, actual: settings.TracesTransport);
    }

    // new tests
    [Fact]
    public void DefaultValues()
    {
        var settings = new ExporterSettings();
        CheckDefaultValues(settings);
    }

    [Fact]
    public void InvalidAgentUrlShouldNotThrow()
    {
        var param = "http://Invalid=%Url!!";
        var settingsFromSource = Setup($"SIGNALFX_ENDPOINT_URL-{param}");
        CheckDefaultValues(settingsFromSource);
        settingsFromSource.ValidationWarnings.Should().Contain($"The Uri: '{param}' is not valid. It won't be taken into account to send traces. Note that only absolute urls are accepted.");
    }

    [Fact]
    public void TracesPipeName()
    {
        var param = @"C:\temp\someval";
        var settings = new ExporterSettings() { TracesPipeName = param };
        var settingsFromSource = Setup($"SIGNALFX_TRACE_PIPE_NAME-{param}");

        AssertPipeIsConfigured(settingsFromSource, param);
        settings.TracesPipeName.Should().Be(param);
    }

    [Fact]
    public void MetricsPipeName()
    {
        var param = "/var/path";
        var settings = new ExporterSettings { MetricsPipeName = param };
        var settingsFromSource = Setup($"SIGNALFX_DOGSTATSD_PIPE_NAME-{param}", "SIGNALFX_METRICS_EXPORTER-StatsD");

        settings.MetricsPipeName.Should().Be(param);
        settingsFromSource.MetricsPipeName.Should().Be(param);

        AssertMetricsPipeIsConfigured(settingsFromSource, param);
        // AssertMetricsPipeIsConfigured(settings, param); // This is actually not working as we don't recompute the transport when setting the property
    }

    [Fact]
    public void DogStatsdPort()
    {
        var param = 9333;
        var settings = new ExporterSettings { DogStatsdPort = param };
        var settingsFromSource = Setup($"SIGNALFX_DOGSTATSD_PORT-{param}", "SIGNALFX_METRICS_EXPORTER-StatsD");

        settings.DogStatsdPort.Should().Be(param);
        settingsFromSource.DogStatsdPort.Should().Be(param);

        CheckDefaultValues(settings, "DogStatsdPort", "MetricsTransport", "MetricsExporter", "MetricsEndpointUrl");
        CheckDefaultValues(settingsFromSource, "DogStatsdPort", "MetricsTransport", "MetricsExporter", "MetricsEndpointUrl");
    }

    [Fact]
    public void PartialFlushEnabled()
    {
        var param = true;
        var settings = new ExporterSettings() { PartialFlushEnabled = param };
        var settingsFromSource = Setup($"SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED-{param}");

        settings.PartialFlushEnabled.Should().Be(param);
        settingsFromSource.PartialFlushEnabled.Should().Be(param);

        CheckDefaultValues(settings, "PartialFlushEnabled");
        CheckDefaultValues(settingsFromSource, "PartialFlushEnabled");
    }

    [Fact]
    public void PartialFlushMinSpans()
    {
        var param = 200;
        var settings = new ExporterSettings() { PartialFlushMinSpans = param };
        var settingsFromSource = Setup($"SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS-{param}");

        settings.PartialFlushMinSpans.Should().Be(param);
        settingsFromSource.PartialFlushMinSpans.Should().Be(param);

        CheckDefaultValues(settings, "PartialFlushMinSpans");
        CheckDefaultValues(settingsFromSource, "PartialFlushMinSpans");
    }

    [Fact]
    public void InvalidPartialFlushMinSpans()
    {
        var param = -200;
        var settingsFromSource = Setup($"DD_TRACE_PARTIAL_FLUSH_MIN_SPANS-{param}");
        settingsFromSource.PartialFlushMinSpans.Should().Be(500);
        Assert.Throws<ArgumentException>(() => new ExporterSettings() { PartialFlushMinSpans = param });
    }

    private void AssertHttpIsConfigured(ExporterSettings settings, Uri expectedUri)
    {
        Assert.Equal(expected: TracesTransportType.Default, actual: settings.TracesTransport);
        Assert.Equal(expected: expectedUri, actual: settings.AgentUri);
        Assert.False(string.Equals(settings.AgentUri.Host, "localhost", StringComparison.OrdinalIgnoreCase));
        CheckDefaultValues(settings, "AgentUri", "TracesTransport");
    }

    private void AssertPipeIsConfigured(ExporterSettings settings, string pipeName)
    {
        Assert.Equal(expected: TracesTransportType.WindowsNamedPipe, actual: settings.TracesTransport);
        Assert.Equal(expected: pipeName, actual: settings.TracesPipeName);
        Assert.NotNull(settings.AgentUri);
        Assert.False(string.Equals(settings.AgentUri.Host, "localhost", StringComparison.OrdinalIgnoreCase));
        CheckDefaultValues(settings, "TracesPipeName", "AgentUri", "TracesTransport", "TracesPipeTimeoutMs");
    }

    private void AssertMetricsPipeIsConfigured(ExporterSettings settings, string pipeName)
    {
        Assert.Equal(expected: MetricsTransportType.NamedPipe, actual: settings.MetricsTransport);
        Assert.Equal(expected: pipeName, actual: settings.MetricsPipeName);
        CheckDefaultValues(settings, "MetricsTransport", "MetricsPipeName", "DogStatsdPort", "MetricsExporter", "MetricsEndpointUrl");
    }

    private void CheckDefaultValues(ExporterSettings settings, params string[] paramToIgnore)
    {
        if (!paramToIgnore.Contains("AgentUri"))
        {
            settings.AgentUri.AbsoluteUri.Should().Be("http://127.0.0.1:9411/api/v2/spans");
        }

        if (!paramToIgnore.Contains("TracesTransport"))
        {
            settings.TracesTransport.Should().Be(TracesTransportType.Default);
        }

        if (!paramToIgnore.Contains("MetricsExporter"))
        {
            settings.MetricsExporter.Should().Be(MetricsExporterType.SignalFx);
        }

        if (!paramToIgnore.Contains("MetricsEndpointUrl"))
        {
            settings.MetricsEndpointUrl.Should().Be("http://localhost:9943/v2/datapoint");
        }

        if (!paramToIgnore.Contains("MetricsTransport"))
        {
            settings.MetricsTransport.Should().Be(MetricsTransportType.UDS);
        }

        if (!paramToIgnore.Contains("TracesPipeName"))
        {
            settings.TracesPipeName.Should().BeNull();
        }

        if (!paramToIgnore.Contains("TracesPipeTimeoutMs"))
        {
            settings.TracesPipeTimeoutMs.Should().Be(500);
        }

        if (!paramToIgnore.Contains("MetricsPipeName"))
        {
            settings.MetricsPipeName.Should().BeNull();
        }

        if (!paramToIgnore.Contains("DogStatsdPort"))
        {
            settings.DogStatsdPort.Should().Be(0);
        }

        if (!paramToIgnore.Contains("PartialFlushEnabled"))
        {
            settings.PartialFlushEnabled.Should().BeFalse();
        }

        if (!paramToIgnore.Contains("PartialFlushMinSpans"))
        {
            settings.PartialFlushMinSpans.Should().Be(500);
        }
    }

    // end new tests

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
