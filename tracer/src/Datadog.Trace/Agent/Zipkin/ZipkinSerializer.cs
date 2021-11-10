using System.Collections.Generic;
using System.IO;
using System.Text;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Vendors.Newtonsoft.Json;
using Datadog.Trace.Vendors.Newtonsoft.Json.Serialization;

namespace Datadog.Trace.Agent
{
    internal class ZipkinSerializer
    {
        // Don't serialize with BOM
        private static readonly Encoding UTF8Enconding = new UTF8Encoding(false);

        private readonly JsonSerializer _serializer = new JsonSerializer();

        private readonly TracerSettings _settings;

        public ZipkinSerializer(TracerSettings settings)
        {
            _settings = settings;
        }

        public void Serialize(Stream stream, Span[][] traces)
        {
            var zipkinTraces = new List<ZipkinSpan>();

            foreach (var trace in traces)
            {
                foreach (var span in trace)
                {
                    var zspan = new ZipkinSpan(span, _settings);
                    zipkinTraces.Add(zspan);
                }
            }

            using (var sw = new StreamWriter(stream, UTF8Enconding, 4096, true))
            {
                _serializer.Serialize(sw, zipkinTraces);
            }
        }

        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy), ItemNullValueHandling = NullValueHandling.Ignore)]
        internal class ZipkinSpan
        {
            private readonly Span _span;
            private readonly IDictionary<string, string> _tags;

            public ZipkinSpan(Span span, TracerSettings settings)
            {
                _span = span;
                _tags = BuildTags(span, settings);
            }

            public string Id
            {
                get => _span.Context.SpanId.ToString("x16");
            }

            public string TraceId
            {
                get => _span.Context.TraceId.ToString();
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
                // Per Zipkin convention these are always upper case.
                get => _span.GetTag(Trace.Tags.SpanKind)?.ToUpperInvariant();
            }

            public Dictionary<string, string> LocalEndpoint
            {
                get
                {
                    var actualServiceName = !string.IsNullOrWhiteSpace(_span.ServiceName)
                        ? _span.ServiceName
                        : Tracer.Instance.DefaultServiceName;

                    // TODO: Save this allocation.
                    return new Dictionary<string, string>() { { "serviceName", actualServiceName } };
                }
            }

            public IDictionary<string, string> Tags
            {
                get => _tags;
            }

            private static IDictionary<string, string> BuildTags(Span span, TracerSettings settings)
            {
                var spanTags = span?.Tags?.GetAllTags();
                var tags = new Dictionary<string, string>(spanTags.Count);
                var recordedValueMaxLength = settings.RecordedValueMaxLength;
                foreach (var entry in spanTags)
                {
                    if (!entry.Key.Equals(Trace.Tags.SpanKind))
                    {
                        var truncatedValue = entry.Value.Truncate(recordedValueMaxLength);
                        tags[entry.Key] = truncatedValue;
                    }
                }

                // report error status according to SFx convention
                if (span?.Status.StatusCode == StatusCode.Error)
                {
                    tags[Trace.Tags.Error] = "true";
                }

                return tags;
            }
        }
    }
}
