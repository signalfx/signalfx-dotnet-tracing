using System;
using System.Collections.Generic;
using System.Globalization;
using Datadog.Trace.ClrProfiler.Emit;
using SignalFx.Tracing;
using SignalFx.Tracing.Vendors.Serilog;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal static class ConsumeKafkaIntegrationHelper
    {
        internal static object Consume<T>(
               object consumer,
               T input,
               int opCode,
               int mdToken,
               long moduleVersionPtr,
               ILogger log)
        {
            if (consumer == null)
            {
                throw new ArgumentNullException(nameof(consumer));
            }

            var inputType = typeof(T).FullName;
            const string methodName = ConfluentKafka.ConsumeSyncMethodName;
            Func<object, T, object> consume;
            var consumerType = consumer.GetType();

            var activeScope = Tracer.Instance.ActiveScope;
            var currentSpan = activeScope?.Span;
            if (currentSpan?.OperationName == ConfluentKafka.ConsumeSyncOperationName)
            {
                activeScope.Dispose();
            }

            try
            {
                consume =
                    MethodBuilder<Func<object, T, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(consumerType)
                       .WithParameters(input)
                       .WithNamespaceAndNameFilters("Confluent.Kafka.ConsumeResult`2", inputType)
                       .Build();
            }
            catch (Exception ex)
            {
                // profiled app will not continue working as expected without this method
                log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: ConfluentKafka.ConsumerType,
                    methodName: methodName,
                    instanceType: consumer.GetType().AssemblyQualifiedName);
                throw;
            }

            object result = consume(consumer, input);
            if (result is not null)
            {
                using var scope = CreateConsumeScopeFromConsumerResult(result, consumer);
            }

            return result;
        }

        internal static Scope CreateConsumeScopeFromConsumerResult(object consumeResult, object consumer)
        {
            var tracer = Tracer.Instance;
            if (!tracer.Settings.IsIntegrationEnabled(ConfluentKafka.IntegrationName) || KafkaHelper.AlreadyInstrumented())
            {
                // integration disabled, don't create a scope/span, skip this trace
                return null;
            }

            SpanContext propagatedContext = null;

            // Try to extract propagated context from headers.
            var message = KafkaHelper.GetPropertyValue<object>(consumeResult, "Message");
            if (message is not null)
            {
                var headers = KafkaHelper.GetPropertyValue<object>(message, "Headers");
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
                var topicName = KafkaHelper.GetPropertyValue<string>(consumeResult, "Topic");
                var partition = KafkaHelper.GetPropertyValue<object>(consumeResult, "Partition");
                string partitionValue = null;
                if (partition is not null)
                {
                    // Here the partition number is the actual partition where the data came from, no sentinel value.
                    partitionValue = KafkaHelper.GetPropertyValue<int>(partition, "Value").ToString(CultureInfo.InvariantCulture);
                }

                scope = tracer.StartActive(OpenTelemetryConsumeSpanName(topicName), propagatedContext);

                var span = scope.Span;
                span.SetTag(Tags.InstrumentationName, ConfluentKafka.IntegrationName);
                span.SetTag(Tags.SpanKind, SpanKinds.Consumer);

                if (partitionValue is not null)
                {
                    span.Tags.Add(Tags.Kafka.Partition, partitionValue);
                }

                if (message is not null)
                {
                    var timestamp = KafkaHelper.GetPropertyValue<object>(consumeResult, "Timestamp");
                    if (timestamp is not null)
                    {
                        var dateTime = KafkaHelper.GetPropertyValue<DateTime>(timestamp, "UtcDateTime");
                        if (dateTime != default)
                        {
                            var consumeTime = DateTime.UtcNow;
                            var queueTimeMs = Math.Max(0, (consumeTime - dateTime).TotalMilliseconds);
                            span.Tags.Add(Tags.Kafka.QueueTimeMs, queueTimeMs.ToString(CultureInfo.InvariantCulture));
                        }
                    }

                    var value = KafkaHelper.GetPropertyValue<object>(message, "Value");
                    if (value is null)
                    {
                        span.Tags.Add(Tags.Kafka.Tombstone, "true");
                    }

                    if (!string.IsNullOrEmpty(topicName))
                    {
                        span.Tags.Add(Tags.Messaging.Destination, topicName);
                    }
                }

                var groupId = KafkaHelper.GetPropertyValue<string>(consumer, "MemberId");
                if (!string.IsNullOrWhiteSpace(groupId))
                {
                    span.Tags.Add(Tags.Kafka.ConsumerGroup, groupId);
                }

                var clientName = KafkaHelper.GetPropertyValue<string>(consumer, "Name");
                if (!string.IsNullOrWhiteSpace(clientName))
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

        private static string OpenTelemetryConsumeSpanName(string topicName)
        {
            const string openTelemetryConsumeOperation = "receive";
            if (string.IsNullOrEmpty(topicName))
            {
                return openTelemetryConsumeOperation;
            }

            return topicName + " " + openTelemetryConsumeOperation;
        }
    }
}
