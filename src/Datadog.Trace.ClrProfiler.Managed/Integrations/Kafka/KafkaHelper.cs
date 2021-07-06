using System;
using System.Reflection;
using SignalFx.Tracing;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Vendors.Serilog;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal static class KafkaHelper
    {
        private static readonly ILogger Log = SignalFxLogging.GetLogger(typeof(ConsumeKafkaIntegration));

        internal const int ConfluentKafkaAnyPartitionSentinel = -1;

        public static readonly Lazy<Type> LazyHeadersType = new(() =>
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.Load(ConfluentKafka.AssemblyName);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load ${ConfluentKafka.AssemblyName} assembly: {ex.Message}");
                return null;
            }

            Type headersType = null;
            try
            {
                headersType = assembly.GetType(ConfluentKafka.HeadersType, throwOnError: true);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get type ${ConfluentKafka.HeadersType}: {ex.Message}");
            }

            return headersType;
        });

        internal static T GetPropertyValue<T>(object obj, string propertyName)
        {
            if (!obj.TryGetPropertyValue(propertyName, out T property))
            {
                property = default;
                Log.Debug($"Unable to access {propertyName} property.");
            }

            return property;
        }

        internal static bool AlreadyInstrumented()
        {
            // The interfaces being instrumented can be wrapped by application code and we want to avoid
            // multiple spans for the same logical operation.
            var currentSpan = Tracer.Instance.ActiveScope?.Span;
            return currentSpan != null &&
                currentSpan.GetTag(Tags.InstrumentationName) == ConfluentKafka.IntegrationName;
        }
    }
}
