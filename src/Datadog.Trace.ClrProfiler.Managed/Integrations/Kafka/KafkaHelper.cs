using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using SignalFx.Tracing;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Vendors.Serilog;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal static class KafkaHelper
    {
        private const int ConfluentKafkaAnyPartitionSentinel = -1;

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

        internal static Scope CreateConsumeScopeFromConsumerResult(object consumeResult, DateTimeOffset startTime)
        {
            var tracer = Tracer.Instance;
            if (!tracer.Settings.IsIntegrationEnabled(ConfluentKafka.IntegrationName))
            {
                // integration disabled, don't create a scope/span, skip this trace
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
                string partitionValue = null;
                if (partition is not null)
                {
                    // Here the partition number is the actual partition where the data came from, no sentinel value.
                    partitionValue = GetPropertyValue<int>(partition, "Value").ToString(CultureInfo.InvariantCulture);
                }

                scope = tracer.StartActive(OpenTelemetryConsumeSpanName(topicName), propagatedContext, startTime: startTime);

                var span = scope.Span;
                if (partitionValue is not null)
                {
                    span.Tags.Add(Tags.Kafka.Partition, partitionValue);
                }

                if (message is not null)
                {
                    var timestamp = GetPropertyValue<object>(consumeResult, "Timestamp");
                    if (timestamp is not null)
                    {
                        var dateTime = GetPropertyValue<DateTime>(timestamp, "UtcDateTime");
                        if (dateTime != default)
                        {
                            var consumeTime = DateTime.UtcNow;
                            var queueTimeMs = Math.Max(0, (consumeTime - dateTime).TotalMilliseconds);
                            span.Tags.Add(Tags.Kafka.QueueTimeMs, queueTimeMs.ToString(CultureInfo.InvariantCulture));
                        }
                    }

                    var value = GetPropertyValue<object>(message, "Value");
                    if (value is null)
                    {
                        span.Tags.Add(Tags.Kafka.Tombstone, "true");
                    }

                    if (!string.IsNullOrEmpty(topicName))
                    {
                        span.Tags.Add(Tags.Messaging.Destination, topicName);
                    }
                }

                span.SetTag(Tags.InstrumentationName, ConfluentKafka.IntegrationName);
                span.SetTag(Tags.SpanKind, SpanKinds.Consumer);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }

        internal static Scope CreateConsumeScopeFromConsumer(object consumer, DateTimeOffset startTime)
        {
            var tracer = Tracer.Instance;
            if (!tracer.Settings.IsIntegrationEnabled(ConfluentKafka.IntegrationName))
            {
                // integration disabled, don't create a scope/span, skip this trace
                return null;
            }

            Scope scope = null;
            try
            {
                var topicNames = GetPropertyValue<List<string>>(consumer, "Subscription");
                var assignedPartitions = GetPropertyValue<List<object>>(consumer, "Assignment");

                string topicName = topicNames != null && topicNames.Count == 1 ? topicNames[0] : null;
                scope = tracer.StartActive(OpenTelemetryConsumeSpanName(topicName), startTime: startTime);

                var span = scope.Span;

                if (topicNames is not null)
                {
                    span.Tags.Add(Tags.Kafka.SubscribedTopics, string.Join(",", topicNames));
                }

                if (assignedPartitions is not null && assignedPartitions.Count > 0)
                {
                    if (assignedPartitions.Count == 1)
                    {
                        int partitionNumber = GetPropertyValue<int>(assignedPartitions[0], "Value");
                        span.Tags.Add(Tags.Kafka.AssignedPartitions, partitionNumber.ToString(CultureInfo.InvariantCulture));

                        if (partitionNumber != ConfluentKafkaAnyPartitionSentinel)
                        {
                            span.Tags.Add(Tags.Kafka.AssignedPartitions, partitionNumber.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                    else
                    {
                        var partitions = new int[assignedPartitions.Count];
                        for (int i = 0; i < assignedPartitions.Count; i++)
                        {
                            partitions[i] = GetPropertyValue<int>(assignedPartitions[i], "Value");
                        }

                        span.Tags.Add(Tags.Kafka.AssignedPartitions, string.Join(",", partitions));
                    }
                }

                span.SetTag(Tags.InstrumentationName, ConfluentKafka.IntegrationName);
                span.SetTag(Tags.SpanKind, SpanKinds.Consumer);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }

        internal static Scope CreateProduceScope(object producer, object topic, object message, string operationName)
        {
            string partitionValue = null;
            if (topic is string topicName)
            {
                return CreateProduceScopeImpl(producer, topicName, partitionValue, message, operationName);
            }

            topicName = GetPropertyValue<string>(topic, "Topic");
            var partition = GetPropertyValue<object>(topic, "Partition");
            if (partition is not null)
            {
                int partitionNumber = GetPropertyValue<int>(partition, "Value");
                if (partitionNumber != ConfluentKafkaAnyPartitionSentinel)
                {
                    partitionValue = partitionNumber.ToString(CultureInfo.InvariantCulture);
                }
            }

            return CreateProduceScopeImpl(producer, topicName, partitionValue, message, operationName);
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

        private static Scope CreateProduceScopeImpl(object producer, string topicName, string partitionValue, object message, string spanKind)
        {
            var tracer = Tracer.Instance;

            Scope scope = null;
            try
            {
                // Following OTel experimental semantic conventions:
                // https://github.com/open-telemetry/opentelemetry-specification/blob/5a19b53d71e967659517c02a69b801381d29bf1e/specification/trace/semantic_conventions/messaging.md#operation-names
                scope = tracer.StartActive(OpenTelemetryProduceSpanName(topicName));
                var span = scope.Span;
                span.SetTag(Tags.SpanKind, spanKind);
                span.SetTag(Tags.Messaging.System, ConfluentKafka.OpenTelemetrySystemName);
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

                var clientName = GetPropertyValue<string>(producer, "Name");
                if (clientName is not null)
                {
                    span.Tags.Add(Tags.Kafka.ClientName, clientName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }

        private static string OpenTelemetryProduceSpanName(string topicName)
        {
            const string OpenTelemetryProduceOperation = "send";
            if (string.IsNullOrEmpty(topicName))
            {
                return OpenTelemetryProduceOperation;
            }

            return topicName + " " + OpenTelemetryProduceOperation;
        }

        private static string OpenTelemetryConsumeSpanName(string topicName)
        {
            const string OpenTelemetryConsumeOperation = "receive";
            if (string.IsNullOrEmpty(topicName))
            {
                return OpenTelemetryConsumeOperation;
            }

            return topicName + " " + OpenTelemetryConsumeOperation;
        }
    }
}
