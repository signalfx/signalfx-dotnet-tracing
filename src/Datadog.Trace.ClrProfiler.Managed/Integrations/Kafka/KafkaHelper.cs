using System;
using System.Globalization;
using System.Reflection;
using SignalFx.Tracing;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Vendors.Serilog;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal static class KafkaHelper
    {
        private static readonly ILogger Log = SignalFxLogging.GetLogger(typeof(ConsumeKafkaIntegration));

        private static readonly Lazy<Type> HeadersType = new Lazy<Type>(() =>
        {
            Assembly assembly = null;
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

        internal static Scope CreateConsumeScope(object consumeResult)
        {
            var tracer = Tracer.Instance;
            if (!tracer.Settings.IsIntegrationEnabled(ConfluentKafka.IntegrationName))
            {
                // integration disabled, don't create a scope/span, skip this trace
                return null;
            }

            var parent = tracer.ActiveScope?.Span;
            if (parent is not null &&
                parent.OperationName == ConfluentKafka.ConsumeSyncOperationName &&
                parent.GetTag(Tags.InstrumentationName) != null)
            {
                return null;
            }

            SpanContext propagatedContext = null;

            // Try to extract propagated context from headers.
            var message = GetPropertyValue<object>(consumeResult, "Message");
            if (message is not null)
            {
                var headers = GetPropertyValue<object>(message, "Headers");
                if (headers is not null)
                {
                    var headersAdapter = new KafkaHeadersCollectionAdapter(headers);

                    try
                    {
                        propagatedContext = tracer.Propagator
                            .Extract(headersAdapter, (h, name) => h.GetValues(name));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error extracting propagated headers from Kafka message");
                    }
                }
            }

            Scope scope = null;
            try
            {
                var topicName = GetPropertyValue<string>(consumeResult, "Topic");
                var partition = GetPropertyValue<object>(consumeResult, "Partition");
                int? partitionValue = null;
                if (partition is not null)
                {
                    partitionValue = GetPropertyValue<int>(partition, "Value");
                }

                scope = tracer.StartActive(ConfluentKafka.ConsumeSyncOperationName, propagatedContext, tracer.DefaultServiceName);

                var span = scope.Span;
                if (partitionValue.HasValue)
                {
                    span.Tags.Add(Tags.Kafka.Partition, partitionValue.Value.ToString());
                }

                if (message is not null)
                {
                    var timestamp = GetPropertyValue<object>(consumeResult, "Timestamp");
                    if (timestamp is not null)
                    {
                        var dateTime = GetPropertyValue<DateTime>(timestamp, "UtcDateTime");
                        if (dateTime != default)
                        {
                            var consumeTime = span.StartTime.UtcDateTime;
                            var messageQueueTimeMs = Math.Max(0, (consumeTime - dateTime).TotalMilliseconds);
                            span.Tags.Add(Tags.Kafka.MessageQueueTimeMs, messageQueueTimeMs.ToString(CultureInfo.InvariantCulture));
                        }
                    }

                    var value = GetPropertyValue<object>(message, "Value");
                    span.Tags.Add(Tags.Kafka.Tombstone, value is null ? "true" : "false");

                    if (!string.IsNullOrEmpty(topicName))
                    {
                        span.Tags.Add("messaging.destination", topicName);
                    }
                }

                span.Type = SpanTypes.Kafka;
                span.SetTag(Tags.InstrumentationName, ConfluentKafka.IntegrationName);
                span.SetTag(Tags.SpanKind, SpanKinds.Client);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }

        internal static Scope CreateProduceScope(object topic, object message, string operationName)
        {
            string partitionValue = null;
            if (topic is string topicName)
            {
                return CreateProduceScopeImpl(topicName, partitionValue, message, operationName);
            }

            topicName = GetPropertyValue<string>(topic, "Topic");
            var partition = GetPropertyValue<object>(topic, "Partition");
            if (partition is not null)
            {
                const int ConfluentKafkaAnyPartitionSentinel = -1;
                int partitionNumber = GetPropertyValue<int>(partition, "Value");
                if (partitionNumber != ConfluentKafkaAnyPartitionSentinel)
                {
                    partitionValue = partitionNumber.ToString(CultureInfo.InvariantCulture);
                }
            }

            return CreateProduceScopeImpl(topicName, partitionValue, message, operationName);
        }

        internal static T GetPropertyValue<T>(object obj, string propertyName)
        {
            if (!obj.TryGetPropertyValue(propertyName, out T property))
            {
                property = default;
                Log.Debug($"Unable to access {propertyName} property.");
            }

            return property;
        }

        internal static object CreateHeaders(object message)
        {
            if (message == null || HeadersType.Value == null)
            {
                // Not expected but we want to avoid throwing and catching exceptions in this case.
                return null;
            }

            try
            {
                var headers = Activator.CreateInstance(HeadersType.Value);
                var headersProperty = message.GetType().GetProperty("Headers")
                    ?? throw new ArgumentException("Message object doesn't have the 'Headers' property");
                var setter = headersProperty.GetSetMethod(nonPublic: false)
                    ?? throw new ArgumentException("Message object doesn't have a setter for the 'Headers' property");
                setter.Invoke(message, new[] { headers });

                return headers;
            }
            catch (Exception ex)
            {
                Log.Debug("Failed to create header for Kafka message {Exception}", ex.Message);
                return null;
            }
        }

        private static Scope CreateProduceScopeImpl(string topicName, string partitionValue, object message, string spanKind)
        {
            var tracer = Tracer.Instance;

            Scope scope = null;
            try
            {
                // Following OTel experimental semantic conventions:
                // https://github.com/open-telemetry/opentelemetry-specification/blob/5a19b53d71e967659517c02a69b801381d29bf1e/specification/trace/semantic_conventions/messaging.md#operation-names
                scope = tracer.StartActive(OpenTelemetryProduceSpanName(topicName, spanKind));
                var span = scope.Span;
                span.SetTag(Tags.SpanKind, spanKind);
                span.SetTag("messaging.system", ConfluentKafka.OpenTelemetrySystemName);
                span.SetTag("messaging.destination", topicName);

                // Kafka specific tags.

                if (partitionValue != null)
                {
                    span.SetTag(Tags.Kafka.Partition, partitionValue);
                }

                if (message != null)
                {
                    // Not required per OTel spec but could be potentially added:
                    //
                    //   1. "messaging.kafka.message_key", i.e., the type of TKey of Message<TKey, TValue>.
                    //      It should be omitted if Null.
                    //   2. "messaging.kafka.client_id", the IProducer.IClient.Name is a string with instance number.

                    var value = GetPropertyValue<object>(message, "Value");
                    if (value is null)
                    {
                        span.Tags.Add(Tags.Kafka.Tombstone, "true");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }

        private static string OpenTelemetryProduceSpanName(string topicName, string spanKind)
        {
            const string OpenTelemetryProduceOperation = "send";
            if (string.IsNullOrEmpty(topicName))
            {
                return OpenTelemetryProduceOperation;
            }

            return topicName + " " + OpenTelemetryProduceOperation;
        }
    }
}
