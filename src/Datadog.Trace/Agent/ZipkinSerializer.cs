// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.ExtensionMethods;

namespace SignalFx.Tracing.Agent
{
    internal class ZipkinSerializer
    {
        // Don't serialize with BOM
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false);

        public void Serialize(Stream stream, Span[][] traces, TracerSettings settings)
        {
            // Don't close stream as IDisposable
            using (var sw = new StreamWriter(stream, DefaultEncoding, 4096, true))
            using (var writer = new JsonTextWriter(sw))
            {
                writer.WriteStartArray();
                for (var i = 0; i < traces.Length; ++i)
                {
                    var trace = traces[i];
                    traces[i] = null;
                    for (var j = 0; j < trace.Length; ++j)
                    {
                        var span = trace[j];
                        trace[j] = null;
                        WriteSpanAsJson(writer, span, settings);
                    }
                }

                writer.WriteEndArray();
            }
        }

        private void WriteSpanAsJson(JsonTextWriter writer, Span span, TracerSettings settings)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("id");
            writer.WriteValue(span.SpanId.ToString("x16"));

            writer.WritePropertyName("traceId");
            writer.WriteValue(span.TraceId.ToString());

            if (span.Context.ParentId != null)
            {
                writer.WritePropertyName("parentId");
                writer.WriteValue(span.Context.ParentId.Value.ToString("x16"));
            }

            writer.WritePropertyName("name");
            writer.WriteValue(span.OperationName);

            writer.WritePropertyName("timestamp");
            writer.WriteValue(span.StartTime.ToUnixTimeMicroseconds());

            writer.WritePropertyName("duration");
            writer.WriteValue(span.Duration.ToMicroseconds());

            var spanKind = span.GetTag(Tracing.Tags.SpanKind);
            if (spanKind != null)
            {
                writer.WritePropertyName("kind");
                writer.WriteValue(spanKind.ToUpper());
            }

            var actualServiceName = settings.ServiceNamePerSpanEnabled && !string.IsNullOrWhiteSpace(span.ServiceName)
                ? span.ServiceName
                : Tracer.Instance.DefaultServiceName;
            writer.WritePropertyName("localEndpoint");
            writer.WriteStartObject();
            writer.WritePropertyName("serviceName");
            writer.WriteValue(actualServiceName);
            writer.WriteEndObject();

            SerializeTags(writer, span, settings);

            SerializeLogs(writer, span, settings);

            writer.WriteEndObject();
        }

        private void SerializeTags(JsonTextWriter writer, Span span, TracerSettings settings)
        {
            writer.WritePropertyName("tags");
            writer.WriteStartObject();

            if (span.Tags?.Count > 0)
            {
                var recordedValueMaxLength = settings.RecordedValueMaxLength;
                foreach (var entry in span.Tags)
                {
                    if (entry.Key.Equals(Tracing.Tags.SpanKind, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var truncatedValue = entry.Value.Truncate(recordedValueMaxLength);
                    if (settings.SanitizeSqlStatements && entry.Key.Equals(Tracing.Tags.DbStatement, StringComparison.OrdinalIgnoreCase))
                    {
                        truncatedValue = truncatedValue.SanitizeSqlStatement();
                    }

                    writer.WritePropertyName(entry.Key);
                    writer.WriteValue(truncatedValue);
                }
            }

            // Store Resource and Type when unique as tags so as not to lose
            if (!string.Equals(span.OperationName, span.ResourceName))
            {
                writer.WritePropertyName(Tracing.Tags.ResourceName);
                writer.WriteValue(span.ResourceName);
            }

            if (span.Type != null)
            {
                writer.WritePropertyName(Tracing.Tags.SpanType);
                writer.WriteValue(span.Type);
            }

            if (span.Error)
            {
                writer.WritePropertyName(Tracing.Tags.Error);
                writer.WriteValue("true");
            }

            writer.WriteEndObject();
        }

        private void SerializeLogs(JsonTextWriter writer, Span span, TracerSettings settings)
        {
            writer.WritePropertyName("annotations");
            writer.WriteStartArray();

            foreach (var log in span.Logs)
            {
                // Zipkin doesn't support an enumeration of objects for a single
                // timestamp as suggested by OpenTracing. For Zipkin it is necessary
                // to repeat the stamp multiple time.
                foreach (var entry in log.Value)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("timestamp");
                    writer.WriteValue(log.Key.ToUnixTimeMicroseconds());

                    // Zipkin doesn't have a way to represent the key, a mismatch to
                    // OpenTracing representation.
                    writer.WritePropertyName("value");
                    writer.WriteValue(entry.Value.Truncate(settings.RecordedValueMaxLength));

                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
        }
    }
}
