// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Datadog.Trace;
using Datadog.Trace.ExtensionMethods;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Datadog.Trace.Agent
{
    internal class ZipkinSerializer
    {
        private readonly JsonSerializer serializer = new JsonSerializer();

        // Don't serialize with BOM
        private readonly Encoding utf8 = new UTF8Encoding(false);

        public static IDictionary<string, string> BuildTags(Span span)
        {
            var tags = new Dictionary<string, string>();
            foreach (var entry in span.Tags)
            {
                if (!entry.Key.Equals(Trace.Tags.SpanKind))
                {
                    tags[entry.Key] = entry.Value;
                }
            }

            // Store Resource and Type when unique as tags so as not to lose
            if (!string.Equals(span.OperationName, span.ResourceName))
            {
                tags[Trace.Tags.ResourceName] = span.ResourceName;
            }

            if (span.Type != null)
            {
                tags[Trace.Tags.SpanType] = span.Type;
            }

            if (span.Error)
            {
                tags[Trace.Tags.Error] = "true";
            }

            return tags;
        }

        public void Serialize(Stream stream, IList<List<Span>> traces)
        {
            var zipkinTraces = new List<ZipkinSpan>();

            foreach (var trace in traces)
            {
                foreach (var span in trace)
                {
                    var zspan = new ZipkinSpan(span);
                    zipkinTraces.Add(zspan);
                }
            }

            // Don't close stream as IDisposable
            using (var sw = new StreamWriter(stream, utf8, 4096, true))
            {
                serializer.Serialize(sw, zipkinTraces);
            }
        }

        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        internal class ZipkinSpan
        {
            private readonly Span _span;

            private readonly IDictionary<string, string> _tags;

            public ZipkinSpan(Span span)
            {
                _span = span;
                if (span.Tags != null)
                {
                    _tags = BuildTags(span);
                }
            }

            public string Id
            {
                get => _span.Context.SpanId.ToString("x16");
            }

            public string TraceId
            {
                get => _span.Context.TraceId.ToString("x16");
            }

            public string ParentId
            {
                get => _span.Context.ParentId?.ToString("x16");
            }

            public string Name
            {
                get => _span.OperationName;
            }

            public long Timestamp
            {
                get => _span.StartTime.ToUnixTimeMicroseconds();
            }

            public long Duration
            {
                get => _span.Duration.ToMicroseconds();
            }

            public string Kind
            {
                get => _span.Tags[Trace.Tags.SpanKind].ToUpper();
            }

            public Dictionary<string, string> LocalEndpoint
            {
                // Don't allow overriding service name per span
                get => new Dictionary<string, string>() { { "serviceName", Tracer.Instance.DefaultServiceName } };
            }

            public IDictionary<string, string> Tags
            {
                get => _tags;
            }

            public List<Dictionary<string, object>> Annotations
            {
                get
                {
                    List<Dictionary<string, object>> annotations = new List<Dictionary<string, object>>();

                    foreach (var e in _span.Logs)
                    {
                        var ts = e.Key.ToUnixTimeMicroseconds();
                        var fields = JsonConvert.SerializeObject(e.Value);
                        var item = new Dictionary<string, object>() { { "timestamp", ts }, { "value", fields } };
                        annotations.Add(item);
                    }

                    return annotations;
                }
            }

            public bool ShouldSerializeTags()
            {
                return _tags != null;
            }

            public bool ShouldSerializeAnnotations()
            {
                return _span.Logs != null;
            }

            public bool ShouldSerializeParentId()
            {
                return _span.Context.ParentId != null;
            }

            public bool ShouldSerializeKind()
            {
                return _span.Tags != null && _span.Tags.ContainsKey(Trace.Tags.SpanKind);
            }
        }
    }
}