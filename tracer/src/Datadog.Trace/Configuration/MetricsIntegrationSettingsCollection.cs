// Modified by Splunk Inc.

using Datadog.Trace.Logging;

namespace Datadog.Trace.Configuration;

/// <summary>
/// A collection of <see cref="MetricsIntegrationSettings"/> instances, referenced by name.
/// </summary>
public class MetricsIntegrationSettingsCollection
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<MetricsIntegrationSettingsCollection>();

    private readonly MetricsIntegrationSettings[] _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsIntegrationSettingsCollection"/> class.
    /// </summary>
    /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
    public MetricsIntegrationSettingsCollection(IConfigurationSource source)
    {
        _settings = GetIntegrationSettings(source);
    }

    internal MetricsIntegrationSettings[] Settings => _settings;

    /// <summary>
    /// Gets the <see cref="IntegrationSettings"/> for the specified integration.
    /// </summary>
    /// <param name="integrationName">The name of the integration.</param>
    /// <returns>The integration-specific settings for the specified integration.</returns>
    public MetricsIntegrationSettings this[string integrationName]
    {
        get
        {
            if (ValuesRegistry<MetricsIntegrationId>.TryGetValue(integrationName, out var integrationId))
            {
                return _settings[(int)integrationId];
            }

            Log.Warning(
                "Accessed metrics integration settings for unknown integration {IntegrationName}. " +
                "Returning default settings, changes will not be saved",
                integrationName);

            return new MetricsIntegrationSettings(integrationName, source: null);
        }
    }

    private static MetricsIntegrationSettings[] GetIntegrationSettings(IConfigurationSource source)
    {
        var integrations = new MetricsIntegrationSettings[ValuesRegistry<MetricsIntegrationId>.Names.Length];

        for (int i = 0; i < integrations.Length; i++)
        {
            var name = ValuesRegistry<MetricsIntegrationId>.Names[i];

            if (name != null)
            {
                integrations[i] = new MetricsIntegrationSettings(name, source);
            }
        }

        return integrations;
    }
}
