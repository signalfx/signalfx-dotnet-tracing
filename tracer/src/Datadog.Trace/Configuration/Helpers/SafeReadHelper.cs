using System;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Configuration.Helpers
{
    internal static class SafeReadHelper
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(SafeReadHelper));

        /// <summary>
        /// Tries to read String from configuration source as Uri. In case of:
        /// Null source default is returned,
        /// Empty configuration default is returned,
        /// Invalid configuration error is logged and default is returned.
        /// </summary>
        /// <param name="source">Configuration source.</param>
        /// <param name="key">Configuration key.</param>
        /// <param name="defaultTo">In case of failure, default to this value.</param>
        /// <returns>Config uri or default.</returns>
        public static Uri SafeReadUri(this IConfigurationSource source, string key, Uri defaultTo)
        {
            string csValue = source?.GetString(key);

            if (string.IsNullOrWhiteSpace(csValue))
            {
                return defaultTo;
            }

            try
            {
                return new Uri(csValue, UriKind.Absolute);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[{key}]: Invalid configuration value '{csValue}'. Defaulting to '{defaultTo}'");
            }

            return defaultTo;
        }

        /// <summary>
        /// Tries to read Int32 from configuration source. In case of:
        /// Null source default is returned,
        /// Empty configuration default is returned,
        /// Invalid configuration error is logged and default is returned.
        /// </summary>
        /// <param name="source">Configuration source.</param>
        /// <param name="key">Configuration key.</param>
        /// <param name="defaultTo">In case of failure, default to this value.</param>
        /// <param name="validators">Value validators.</param>
        /// <returns>Config value or default.</returns>
        public static int SafeReadInt32(this IConfigurationSource source, string key, int defaultTo, params Predicate<int>[] validators)
        {
            int? csValue = source?.GetInt32(key);

            if (!csValue.HasValue)
            {
                return defaultTo;
            }

            if (validators.Length > 0)
            {
                foreach (var validator in validators)
                {
                    if (!validator(csValue.Value))
                    {
                        Log.Error($"[{key}]: Invalid configuration value '{csValue}'. Defaulting to '{defaultTo}'");

                        return defaultTo;
                    }
                }
            }

            return csValue.Value;
        }
    }
}
