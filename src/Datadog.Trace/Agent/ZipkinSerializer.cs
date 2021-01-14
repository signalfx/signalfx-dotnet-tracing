// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SignalFx.Tracing;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.ExtensionMethods;

namespace SignalFx.Tracing.Agent
{
    internal class ZipkinSerializer
    {
        private readonly JsonSerializer serializer = new JsonSerializer();

        // Don't serialize with BOM
        private readonly Encoding utf8 = new UTF8Encoding(false);

        public static IDictionary<string, string> BuildTags(Span span, TracerSettings settings)
        {
            var tags = new Dictionary<string, string>();
            var recordedValueMaxLength = settings.RecordedValueMaxLength;
            foreach (var entry in span.Tags)
            {
                if (!entry.Key.Equals(Tracing.Tags.SpanKind))
                {
                    var truncatedValue = entry.Value.Truncate(recordedValueMaxLength);
                    tags[entry.Key] = truncatedValue;
                }
            }

            // Perform any DB statement sanitization only after truncating the string to avoid replacing truncated
            // part of the statement.
            if (settings.SanitizeSqlStatements && tags.TryGetValue(Tracing.Tags.DbStatement, out var dbStatement))
            {
                var sanitizedDbStatement = dbStatement.SanitizeSqlStatement();
                if (!ReferenceEquals(dbStatement, sanitizedDbStatement))
                {
                    tags[Tracing.Tags.DbStatement] = sanitizedDbStatement;
                }
            }

            // Store Resource and Type when unique as tags so as not to lose
            if (!string.Equals(span.OperationName, span.ResourceName))
            {
                tags[Tracing.Tags.ResourceName] = span.ResourceName;
            }

            if (span.Type != null)
            {
                tags[Tracing.Tags.SpanType] = span.Type;
            }

            if (span.Error)
            {
                tags[Tracing.Tags.Error] = "true";
            }

            return tags;
        }

        public void Serialize(Stream stream, Span[][] traces, TracerSettings settings)
        {
            var zipkinTraces = new List<ZipkinSpan>();

            foreach (var trace in traces)
            {
                foreach (var span in trace)
                {
                    var zspan = new ZipkinSpan(span, settings);
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
            private static IDictionary<string, string> emptyTags = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
            private static ReadOnlyCollection<ReadOnlyDictionary<string, object>> emptyAnnotations = (new List<ReadOnlyDictionary<string, object>>()).AsReadOnly();

            private readonly Span _span;
            private readonly TracerSettings _settings;
            private readonly IDictionary<string, string> _tags;

            public ZipkinSpan(Span span, TracerSettings settings)
            {
                _span = span;
                _settings = settings;
                _tags = span.Tags != null ? BuildTags(span, settings) : emptyTags;
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
                get => _span.GetTag(Tracing.Tags.SpanKind)?.ToUpper();
            }

            public Dictionary<string, string> LocalEndpoint
            {
                get
                {
                    var actualServiceName = _settings.ServiceNamePerSpanEnabled && !string.IsNullOrWhiteSpace(_span.ServiceName)
                        ? _span.ServiceName
                        : Tracer.Instance.DefaultServiceName;

                    return new Dictionary<string, string>() { { "serviceName", actualServiceName } };
                }
            }

            public IDictionary<string, string> Tags
            {
                get => _tags;
            }

            public IEnumerable<IDictionary<string, object>> Annotations
            {
                get
                {
                    if (_span.Logs.Count == 0)
                    {
                        return emptyAnnotations;
                    }

                    var annotations = new List<IDictionary<string, object>>(_span.Logs.Count);
                    foreach (var e in _span.Logs)
                    {
                        foreach (var kvp in e.Value.ToList())
                        {
                            var truncated = kvp.Value.Truncate(_settings.RecordedValueMaxLength);
                            if (!ReferenceEquals(kvp.Value, truncated))
                            {
                                e.Value[kvp.Key] = truncated;
                            }
                        }

                        var ts = e.Key.ToUnixTimeMicroseconds();
                        var fields = JsonConvert.SerializeObject(e.Value);
                        var item = new Dictionary<string, object>() { { "timestamp", ts }, { "value", fields } };
                        annotations.Add(item);
                    }

                    return annotations;
                }
            }

            // Methods below are used by Newtonsoft JSON serializer to decide if should serialize
            // some properties when they are null.

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
                return _span.Tags != null && _span.Tags.ContainsKey(Tracing.Tags.SpanKind);
            }
        }
    }
}
