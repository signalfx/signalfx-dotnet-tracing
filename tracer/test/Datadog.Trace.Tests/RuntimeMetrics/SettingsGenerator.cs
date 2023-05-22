using Datadog.Trace.Configuration;
using Moq;

namespace Datadog.Trace.Tests.RuntimeMetrics;

internal class SettingsGenerator
{
    internal static ImmutableMetricsIntegrationSettingsCollection Generate(bool netRuntimeEnabled = true, bool processEnabled = true, bool aspNetEnabled = true)
    {
        var sourceMock = new Mock<IConfigurationSource>();
        sourceMock.Setup(configurationSource => configurationSource.GetBool("SIGNALFX_METRICS_NetRuntime_ENABLED")).Returns(netRuntimeEnabled);
        sourceMock.Setup(configurationSource => configurationSource.GetBool("SIGNALFX_METRICS_Process_ENABLED")).Returns(processEnabled);
        sourceMock.Setup(configurationSource => configurationSource.GetBool("SIGNALFX_METRICS_AspNetCore_ENABLED")).Returns(aspNetEnabled);
        var metricsSetting = new MetricsIntegrationSettingsCollection(sourceMock.Object);

        return new ImmutableMetricsIntegrationSettingsCollection(metricsSetting, false);
    }
}
