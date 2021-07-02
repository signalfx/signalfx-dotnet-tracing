using System;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.Emit;
using SignalFx.Tracing;
using SignalFx.Tracing.Vendors.Serilog;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal static class ProducKafkaIntegrationHelper
    {
        public static object Produce(
            object producer,
            object topic,
            object message,
            object deliveryHandler,
            int opCode,
            int mdToken,
            long moduleVersionPtr,
            string operationName,
            string topicType,
            ILogger log)
        {
            if (producer is null)
            {
                throw new ArgumentNullException(nameof(producer));
            }

            var scope = KafkaHelper.CreateProduceScope(topic, message, operationName);

            InjectHeaders(message, scope);

            const string methodName = Constants.ProduceSyncMethodName;
            Action<object, object, object, object> produce;
            var producerType = producer.GetType();

            try
            {
                produce =
                    MethodBuilder<Action<object, object, object, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(producerType)
                       .WithParameters(topic, message, deliveryHandler)
                       .WithNamespaceAndNameFilters(ClrNames.Void, topicType, Constants.MessageTypeName, Constants.ActionOfDeliveryReportTypeName)
                       .Build();
            }
            catch (Exception ex)
            {
                LogError(producer, opCode, mdToken, moduleVersionPtr, log, ex, methodName);
                throw;
            }

            try
            {
                produce(producer, topic, message, deliveryHandler);
                return null;
            }
            catch (Exception ex) when (scope.Span.SetExceptionForFilter(ex))
            {
                throw;
            }
            finally
            {
                scope.Dispose();
            }
        }

        public static Task<object> ProduceAsync(
            object producer,
            object topic,
            object message,
            object cancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr,
            string operationName,
            string topicType,
            ILogger log)
        {
            if (producer is null)
            {
                throw new ArgumentNullException(nameof(producer));
            }

            var scope = KafkaHelper.CreateProduceScope(topic, message, operationName);

            InjectHeaders(message, scope);

            const string methodName = Constants.ProduceAsyncMethodName;
            Func<object, object, object, object, Task<object>> produce;
            var producerType = producer.GetType();

            try
            {
                produce =
                    MethodBuilder<Func<object, object, object, object, Task<object>>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(producerType)
                       .WithParameters(topic, message, cancellationToken)
                       .WithNamespaceAndNameFilters(ClrNames.GenericTask, topicType, Constants.MessageTypeName, ClrNames.CancellationToken)
                       .Build();
            }
            catch (Exception ex)
            {
                LogError(producer, opCode, mdToken, moduleVersionPtr, log, ex, methodName);
                throw;
            }

            try
            {
                return produce(producer, topic, message, cancellationToken);
            }
            catch (Exception ex) when (scope.Span.SetExceptionForFilter(ex))
            {
                throw;
            }
            finally
            {
                scope.Dispose();
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
                instrumentedType: Constants.ProducerType,
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
