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
        private readonly object _headers;
        private readonly MethodInfo _tryGetLastBytes;
        private readonly MethodInfo _add;
        private readonly MethodInfo _remove;

        public KafkaHeadersCollectionAdapter(object headers)
        {
            _headers = headers;
            _tryGetLastBytes = _headers.GetType().GetMethod("TryGetLastBytes");
            _add = _headers.GetType().GetMethod("Add");
            _remove = _headers.GetType().GetMethod("Remove");
        }

        public IEnumerable<string> GetValues(string name)
        {
            var parameters = new object[] { name, null };
            if (!(bool)_tryGetLastBytes.Invoke(_headers, parameters))
            {
                return Enumerable.Empty<string>();
            }

            var bytes = (byte[])parameters[1];
            try
            {
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
            catch (Exception ex)
            {
                Log.Information(ex, "Could not deserialize Kafka header {headerName}", name);
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
            _add.Invoke(_headers, new object[] { Encoding.UTF8.GetBytes(value) });
        }

        public void Remove(string name)
        {
            _remove.Invoke(_headers, new object[] { name });
        }
    }
}
