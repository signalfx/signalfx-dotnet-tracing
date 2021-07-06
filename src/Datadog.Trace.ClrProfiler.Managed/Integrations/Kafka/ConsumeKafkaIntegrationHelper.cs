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

            object result;
            Scope scope = null;
            var startTimeOffset = DateTimeOffset.UtcNow;
            try
            {
                result = consume(consumer, input);
                scope = result != null
                    ? CreateConsumeScopeFromConsumerResult(result, consumer, startTimeOffset)
                    : CreateConsumeScopeFromConsumer(consumer, startTimeOffset);
            }
            catch (Exception ex)
            {
                var tracer = Tracer.Instance;
                if (!tracer.Settings.IsIntegrationEnabled(ConfluentKafka.IntegrationName) || KafkaHelper.AlreadyInstrumented())
                {
                    throw;
                }

                // Integration is enabled but the scope was not created since consume raised an exception.
                // Create a span to record the exception with the available info.
                scope = CreateConsumeScopeFromConsumer(consumer, startTimeOffset);
                if (ex is not OperationCanceledException)
                {
                    // OperationCanceledException is expected in case of a clean shutdown and shouldn't
                    // be reported as an exception.
                    scope.Span.SetException(ex);
                }

                throw;
            }
            finally
            {
                scope?.Dispose();
            }

            return result;
        }

        internal static Scope CreateConsumeScopeFromConsumerResult(object consumeResult, object consumer, DateTimeOffset startTime)
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

                scope = tracer.StartActive(OpenTelemetryConsumeSpanName(topicName), propagatedContext, startTime: startTime);

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

        internal static Scope CreateConsumeScopeFromConsumer(object consumer, DateTimeOffset startTime)
        {
            var tracer = Tracer.Instance;
            if (!tracer.Settings.IsIntegrationEnabled(ConfluentKafka.IntegrationName) || KafkaHelper.AlreadyInstrumented())
            {
                // integration disabled, don't create a scope/span, skip this trace
                return null;
            }

            Scope scope = null;
            try
            {
                var topicNames = KafkaHelper.GetPropertyValue<List<string>>(consumer, "Subscription");
                var assignedPartitions = KafkaHelper.GetPropertyValue<List<object>>(consumer, "Assignment");

                var topicName = topicNames != null && topicNames.Count == 1 ? topicNames[0] : null;
                scope = tracer.StartActive(OpenTelemetryConsumeSpanName(topicName), startTime: startTime);

                var span = scope.Span;

                span.SetTag(Tags.InstrumentationName, ConfluentKafka.IntegrationName);
                span.SetTag(Tags.SpanKind, SpanKinds.Consumer);
                span.SetTag(Tags.Messaging.System, ConfluentKafka.OpenTelemetrySystemName);

                // When constructing span from consumer it means that could mean an exception (to be
                // recorded later) or that there no messages on the topic or that the topic end was
                // reached. Create a tag to make that fact clear.
                span.SetTag(Tags.Kafka.MessagedReceived, "false");

                if (topicNames is not null)
                {
                    span.Tags.Add(Tags.Kafka.SubscribedTopics, string.Join(",", topicNames));
                }

                if (assignedPartitions is not null && assignedPartitions.Count > 0)
                {
                    if (assignedPartitions.Count == 1)
                    {
                        var partitionNumber = KafkaHelper.GetPropertyValue<int>(assignedPartitions[0], "Value");
                        span.Tags.Add(Tags.Kafka.AssignedPartitions, partitionNumber.ToString(CultureInfo.InvariantCulture));

                        if (partitionNumber != KafkaHelper.ConfluentKafkaAnyPartitionSentinel)
                        {
                            span.Tags.Add(Tags.Kafka.AssignedPartitions, partitionNumber.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                    else
                    {
                        var partitions = new int[assignedPartitions.Count];
                        for (var i = 0; i < assignedPartitions.Count; i++)
                        {
                            partitions[i] = KafkaHelper.GetPropertyValue<int>(assignedPartitions[i], "Value");
                        }

                        span.Tags.Add(Tags.Kafka.AssignedPartitions, string.Join(",", partitions));
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
                Log.Error(ex, "Error creating or populating consumer scope.");
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
