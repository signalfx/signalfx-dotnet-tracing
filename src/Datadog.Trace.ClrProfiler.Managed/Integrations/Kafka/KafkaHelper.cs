using System;
using System.Reflection;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Vendors.Serilog;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal static class KafkaHelper
    {
        private static readonly ILogger Log = SignalFxLogging.GetLogger(typeof(ConsumeKafkaIntegration));

        internal static T GetPropertyValue<T>(object obj, string propertyName)
        {
            if (!obj.TryGetPropertyValue(propertyName, out T property))
            {
                property = default;
                Log.Warning($"Unable to access {propertyName} property.");
            }

            return property;
        }

        internal static object CreateHeaders(object message)
        {
            try
            {
                var headers = Activator.CreateInstance(Assembly.Load(Constants.ConfluentKafkaAssemblyName).GetType(Constants.HeadersType));
                var headersProperty = message.GetType().GetProperty("Headers");
                var setter = headersProperty.GetSetMethod(nonPublic: false);
                setter.Invoke(message, new object[] { headers });

                return headers;
            }
            catch (Exception)
            {
                Log.Warning("Failed to create headers");
                return null;
            }
        }
    }
}
