using System;
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
                scope = KafkaHelper.CreateProduceScope(producer, topic, message, SpanKinds.Producer);
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
                scope = KafkaHelper.CreateProduceScope(producer, topic, message, SpanKinds.Client);
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
            var headers = KafkaHelper.GetPropertyValue<object>(message, "Headers") ?? KafkaHelper.CreateHeaders(message);
            if (headers is null)
            {
                return;
            }

            var headerAdapter = new KafkaHeadersCollectionAdapter(headers);
            Tracer.Instance.Propagator
                  .Inject(scope.Span.Context, headerAdapter, (collectionAdapter, key, value) => collectionAdapter.Set(key, value));
        }
    }
}
