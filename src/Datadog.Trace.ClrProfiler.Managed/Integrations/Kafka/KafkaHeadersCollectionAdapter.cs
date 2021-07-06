using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SignalFx.Tracing.Headers;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Vendors.Serilog;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal readonly struct KafkaHeadersCollectionAdapter : IHeadersCollection
    {
        private static readonly ILogger Log = SignalFxLogging.GetLogger(typeof(KafkaHeadersCollectionAdapter));
        private static readonly MethodInfo TryGetLastBytesMethodInfo;
        private static readonly MethodInfo AddMethodInfo;
        private static readonly MethodInfo RemoveMethodInfo;

        private readonly object _headers;

        static KafkaHeadersCollectionAdapter()
        {
            var methods = KafkaHelper.LazyHeadersType.Value?.GetMethods();
            if (methods == null)
            {
                // Information already logged when attempting to build LazyHeadersType.Value.
                return;
            }

            TryGetLastBytesMethodInfo = methods.FirstOrDefault(m => m.Name == "TryGetLastBytes");
            if (TryGetLastBytesMethodInfo == null)
            {
                Log.Warning("Missing expected method TryGetLastBytes on type " + ConfluentKafka.HeadersType);
            }

            AddMethodInfo = methods.FirstOrDefault(m => m.Name == "Add" && m.GetParameters().Length == 2);
            if (AddMethodInfo == null)
            {
                Log.Warning("Missing expected method Add on type " + ConfluentKafka.HeadersType);
            }

            RemoveMethodInfo = methods.FirstOrDefault(m => m.Name == "Remove");
            if (RemoveMethodInfo == null)
            {
                Log.Warning("Missing expected method Remove on type " + ConfluentKafka.HeadersType);
            }
        }

        public KafkaHeadersCollectionAdapter(object headers)
        {
            _headers = headers;
        }

        public IEnumerable<string> GetValues(string name)
        {
            var parameters = new object[] { name, null };
            if (!(bool)TryGetLastBytesMethodInfo?.Invoke(_headers, parameters))
            {
                Log.Debug("Could not retrieve header {headerName}.", name);
                return Enumerable.Empty<string>();
            }

            var bytes = (byte[])parameters[1];
            try
            {
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Could not deserialize Kafka header {headerName}", name);
            }

            return Enumerable.Empty<string>();
        }

        public void Set(string name, string value)
        {
            Remove(name);
            Add(name, value);
        }

        public void Add(string name, string value)
        {
            AddMethodInfo?.Invoke(_headers, new object[] { name, Encoding.UTF8.GetBytes(value) });
        }

        public void Remove(string name)
        {
            RemoveMethodInfo?.Invoke(_headers, new object[] { name });
        }
    }
}
