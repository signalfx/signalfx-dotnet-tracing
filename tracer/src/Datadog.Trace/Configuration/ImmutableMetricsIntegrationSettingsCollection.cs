using Datadog.Trace.Logging;

namespace Datadog.Trace.Configuration;

/// <summary>
/// A collection of <see cref="ImmutableMetricsIntegrationSettings"/> instances, referenced by name.
/// </summary>
public class ImmutableMetricsIntegrationSettingsCollection
{
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<MetricsIntegrationSettingsCollection>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableMetricsIntegrationSettingsCollection"/> class.
        /// </summary>
        /// <param name="settings">The <see cref="MetricsIntegrationSettingsCollection"/> to populate the immutable settings.</param>
        internal ImmutableMetricsIntegrationSettingsCollection(
            MetricsIntegrationSettingsCollection settings)
        {
            Settings = GetIntegrationSettingsById(settings);
        }

        internal ImmutableMetricsIntegrationSettings[] Settings { get; }

        /// <summary>
        /// Gets the <see cref="ImmutableIntegrationSettings"/> for the specified integration.
        /// </summary>
        /// <param name="integrationName">The name of the integration.</param>
        /// <returns>The integration-specific settings for the specified integration.</returns>
        public ImmutableMetricsIntegrationSettings this[string integrationName]
        {
            get
            {
                if (IntegrationRegistry.TryGetIntegrationId(integrationName, out var integrationId))
                {
                    return Settings[(int)integrationId];
                }

                Log.Warning(
                    "Accessed integration settings for unknown integration {IntegrationName}. Returning default settings",
                    integrationName);

                return new ImmutableMetricsIntegrationSettings(integrationName);
            }
        }

        internal ImmutableMetricsIntegrationSettings this[IntegrationId integration]
            => Settings[(int)integration];

        private static ImmutableMetricsIntegrationSettings[] GetIntegrationSettingsById(
            MetricsIntegrationSettingsCollection settings)
        {
            var integrations = new ImmutableMetricsIntegrationSettings[settings.Settings.Length];
            for (var i = 0; i < integrations.Length; i++)
            {
                integrations[i] = new ImmutableMetricsIntegrationSettings(settings.Settings[i]);
            }

            return integrations;
        }
    }
