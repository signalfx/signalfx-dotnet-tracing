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
            var methods = _headers.GetType().GetMethods().ToList();
            _tryGetLastBytes = methods.FirstOrDefault(m => m.Name == "TryGetLastBytes");
            _add = methods.FirstOrDefault(m => m.Name == "Add" && m.GetParameters().Length == 2);
            _remove = methods.FirstOrDefault(m => m.Name == "Remove");
        }

        public IEnumerable<string> GetValues(string name)
        {
            var parameters = new object[] { name, null };

            try
            {
                if (!(bool)_tryGetLastBytes.Invoke(_headers, parameters))
                {
                    Log.Information("Could not retrieve header {header}.", name);
                    return Enumerable.Empty<string>();
                }
            }
            catch (Exception)
            {
                Log.Warning($"Could not invoke \"TryGetLastBytes\" method of the {_headers.GetType()} class");
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
            try
            {
                _add.Invoke(_headers, new object[] { name, Encoding.UTF8.GetBytes(value) });
            }
            catch (Exception)
            {
                Log.Warning($"Could not invoke \"Add\" method of the {_headers.GetType()} class");
            }
        }

        public void Remove(string name)
        {
            try
            {
                _remove.Invoke(_headers, new object[] { name });
            }
            catch (Exception)
            {
                Log.Warning($"Could not invoke \"Remove\" method of the {_headers.GetType()} class");
            }
        }
    }
}
