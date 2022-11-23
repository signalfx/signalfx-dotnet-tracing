namespace Datadog.Trace.Configuration;

/// <summary>
/// Contains metrics integration-specific settings.
/// </summary>
public class ImmutableMetricsIntegrationSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableMetricsIntegrationSettings"/> class from an instance of
    /// <see cref="MetricsIntegrationSettings"/>.
    /// </summary>
    /// <param name="settings">The values to use.</param>
    internal ImmutableMetricsIntegrationSettings(MetricsIntegrationSettings settings)
    {
        IntegrationName = settings.IntegrationName;
        Enabled = settings.Enabled;
    }

    internal ImmutableMetricsIntegrationSettings(string name)
    {
        IntegrationName = name;
    }

    /// <summary>
    /// Gets the name of the integration. Used to retrieve integration-specific settings.
    /// </summary>
    public string IntegrationName { get; }

    /// <summary>
    /// Gets a value indicating whether
    /// this integration is enabled.
    /// </summary>
    public bool? Enabled { get; }
}
