using System.Collections.Specialized;
using Datadog.Trace.Configuration;
using FluentAssertions;
using Xunit;

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
        var collection = new NameValueCollection { { "SIGNALFX_REALM", "realm" } };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.AgentUri.Should().Be("https://ingest.realm.signalfx.com/v2/trace");
    }

    [Fact]
    public void If_nondefault_signalfx_realm_is_configured_then_direct_ingest_endpoint_for_metrics_is_used()
    {
        var collection = new NameValueCollection { { "SIGNALFX_REALM", "realm" } };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.MetricsEndpointUrl.Should().Be("https://ingest.realm.signalfx.com/v2/datapoint");
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
            { "SIGNALFX_REALM", "realm" },
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
            { "SIGNALFX_REALM", "realm" },
            { "SIGNALFX_METRICS_ENDPOINT_URL", "http://127.0.0.1:9943/v2/datapoint" }
        };
        var configuration = new NameValueConfigurationSource(collection);

        var settings = new ExporterSettings(configuration);

        settings.MetricsEndpointUrl.Should().Be("http://127.0.0.1:9943/v2/datapoint");
    }
}
