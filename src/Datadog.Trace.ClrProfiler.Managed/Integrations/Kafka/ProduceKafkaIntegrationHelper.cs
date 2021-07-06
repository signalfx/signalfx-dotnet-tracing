using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.Emit;
using Datadog.Trace.ClrProfiler.Helpers;
using SignalFx.Tracing;
using SignalFx.Tracing.Vendors.Serilog;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal static class ProduceKafkaIntegrationHelper
    {
        private static readonly Type IProducerType = Type.GetType(ConfluentKafka.IProducerTypeName + ", " + ConfluentKafka.AssemblyName);
        private static readonly Type ProduceAsyncResponseType = Type.GetType("Confluent.Kafka.DeliveryResult`2, Confluent.Kafka");

        public static void Produce(
            object producer,
            object topic,
            object message,
            object deliveryHandler,
            int opCode,
            int mdToken,
            long moduleVersionPtr,
            string topicType,
            ILogger log)
        {
            if (producer is null)
            {
                throw new ArgumentNullException(nameof(producer));
            }

            Scope scope = null;
            if (Tracer.Instance.Settings.IsIntegrationEnabled(ConfluentKafka.IntegrationName) && !KafkaHelper.AlreadyInstrumented())
            {
                // Pay-for-play: only create scope and inject headers if instrumentation is enabled.
                // Produce method is of type PRODUCER per OTel spec since it doesn't wait for a response.
                scope = CreateProduceScope(producer, topic, message, SpanKinds.Producer);
                InjectHeaders(message, scope);
            }

            Action<object, object, object, object> produce;
            var producerType = producer.GetType();

            try
            {
                produce =
                    MethodBuilder<Action<object, object, object, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, ConfluentKafka.ProduceSyncMethodName)
                       .WithConcreteType(producerType)
                       .WithParameters(topic, message, deliveryHandler)
                       .WithNamespaceAndNameFilters(ClrNames.Void, topicType, ConfluentKafka.MessageTypeName, ConfluentKafka.ActionOfDeliveryReportTypeName)
                       .Build();
            }
            catch (Exception ex)
            {
                LogError(producer, opCode, mdToken, moduleVersionPtr, log, ex, ConfluentKafka.ProduceSyncMethodName);
                throw;
            }

            try
            {
                produce(producer, topic, message, deliveryHandler);
            }
            catch (Exception ex)
            {
                scope?.Span.SetExceptionForFilter(ex);
                throw;
            }
            finally
            {
                scope?.Dispose();
            }
        }

        public static object ProduceAsync(
            object producer,
            object topic,
            object message,
            object boxedCancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr,
            string topicType,
            ILogger log)
        {
            if (producer is null)
            {
                throw new ArgumentNullException(nameof(producer));
            }

            var cancellationToken = (CancellationToken)boxedCancellationToken;

            var genericResponseArguments = producer.GetType().GetGenericArguments();
            var genericResponseType = ProduceAsyncResponseType.MakeGenericType(genericResponseArguments);

            Func<object, object, object, CancellationToken, object> produce;

            try
            {
                produce =
                    MethodBuilder<Func<object, object, object, CancellationToken, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, ConfluentKafka.ProduceAsyncMethodName)
                       .WithConcreteType(producer.GetType())
                       .WithParameters(topic, message, cancellationToken)
                       .WithNamespaceAndNameFilters(ClrNames.GenericTask, topicType, ConfluentKafka.MessageTypeName, ClrNames.CancellationToken)
                       .ForceMethodDefinitionResolution()
                       .Build();
            }
            catch (Exception ex)
            {
                LogError(producer, opCode, mdToken, moduleVersionPtr, log, ex, ConfluentKafka.ProduceAsyncMethodName);
                throw;
            }

            return AsyncHelper.InvokeGenericTaskDelegate(
                owningType: IProducerType.MakeGenericType(genericResponseArguments),
                taskResultType: genericResponseType,
                nameOfIntegrationMethod: nameof(CallProduceAsyncInternal),
                integrationType: typeof(ProduceKafkaIntegrationHelper),
                producer,
                topic,
                message,
                boxedCancellationToken,
                produce);
        }

        internal static Scope CreateProduceScope(object producer, object topic, object message, string operationName)
        {
            string partitionValue = null;
            if (topic is string topicName)
            {
                return CreateProduceScopeImpl(producer, topicName, partitionValue, message, operationName);
            }

            topicName = KafkaHelper.GetPropertyValue<string>(topic, "Topic");
            var partition = KafkaHelper.GetPropertyValue<object>(topic, "Partition");
            if (partition is not null)
            {
                int partitionNumber = KafkaHelper.GetPropertyValue<int>(partition, "Value");
                if (partitionNumber != KafkaHelper.ConfluentKafkaAnyPartitionSentinel)
                {
                    partitionValue = partitionNumber.ToString(CultureInfo.InvariantCulture);
                }
            }

            return CreateProduceScopeImpl(producer, topicName, partitionValue, message, operationName);
        }

        private static async Task<T> CallProduceAsyncInternal<T>(
            object producer,
            object topic,
            object message,
            CancellationToken cancellationToken,
            Func<object, object, object, CancellationToken, object> produce)
        {
            Scope scope = null;
            if (Tracer.Instance.Settings.IsIntegrationEnabled(ConfluentKafka.IntegrationName) && !KafkaHelper.AlreadyInstrumented())
            {
                // Pay-for-play: only create scope and inject headers if instrumentation is enabled.
                // ProduceAsync method is of type CLIENT per OTel spec since it awaits for a response.
                scope = CreateProduceScope(producer, topic, message, SpanKinds.Client);
                InjectHeaders(message, scope);
            }

            try
            {
                return await (Task<T>)produce(producer, topic, message, cancellationToken);
            }
            catch (Exception ex)
            {
                scope?.Span.SetExceptionForFilter(ex);
                throw;
            }
            finally
            {
                scope?.Dispose();
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
                span.SetTag(Tags.Messaging.Destination, topicName);
                span.SetTag(Tags.InstrumentationName, ConfluentKafka.IntegrationName);

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

                    var value = KafkaHelper.GetPropertyValue<object>(message, "Value");
                    if (value is null)
                    {
                        span.Tags.Add(Tags.Kafka.Tombstone, "true");
                    }
                }

                var clientName = KafkaHelper.GetPropertyValue<string>(producer, "Name");
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
            const string openTelemetryProduceOperation = "send";
            if (string.IsNullOrEmpty(topicName))
            {
                return openTelemetryProduceOperation;
            }

            return topicName + " " + openTelemetryProduceOperation;
        }

        private static void LogError(object producer, int opCode, int mdToken, long moduleVersionPtr, ILogger log, Exception ex, string methodName)
        {
            // profiled app will not continue working as expected without this method
            log.ErrorRetrievingMethod(
                exception: ex,
                moduleVersionPointer: moduleVersionPtr,
                mdToken: mdToken,
                opCode: opCode,
                instrumentedType: ConfluentKafka.IProducerTypeName,
                methodName: methodName,
                instanceType: producer.GetType().AssemblyQualifiedName);
        }

        private static void InjectHeaders(object message, Scope scope)
        {
            var headers = KafkaHelper.GetPropertyValue<object>(message, "Headers") ?? CreateHeaders(message);
            if (headers is null)
            {
                return;
            }

            var headerAdapter = new KafkaHeadersCollectionAdapter(headers);
            Tracer.Instance.Propagator
                  .Inject(scope.Span.Context, headerAdapter, (collectionAdapter, key, value) => collectionAdapter.Set(key, value));
        }

        private static object CreateHeaders(object message)
        {
            if (message == null || KafkaHelper.LazyHeadersType.Value == null)
            {
                // Not expected but we want to avoid throwing and catching exceptions in this case.
                return null;
            }

            try
            {
                var headers = Activator.CreateInstance(KafkaHelper.LazyHeadersType.Value);
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
    }
}
