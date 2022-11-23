using Datadog.Trace.Util;

namespace Datadog.Trace.Configuration;

/// <summary>
/// Contains metrics integration-specific settings.
/// </summary>
public class MetricsIntegrationSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsIntegrationSettings"/> class.
    /// </summary>
    /// <param name="integrationName">The integration name.</param>
    /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
    public MetricsIntegrationSettings(string integrationName, IConfigurationSource source)
    {
        if (integrationName is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(integrationName));
        }

        IntegrationName = integrationName;

        if (source == null)
        {
            return;
        }

        Enabled = source.GetBool(string.Format(ConfigurationKeys.Metrics.Enabled, integrationName)) ?? false;
    }

    /// <summary>
    /// Gets the name of the integration. Used to retrieve integration-specific settings.
    /// </summary>
    public string IntegrationName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether
    /// this integration is enabled.
    /// </summary>
    public bool Enabled { get; set; }
}
