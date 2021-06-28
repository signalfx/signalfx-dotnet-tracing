// Modified by SignalFx

using System;
using Datadog.Trace.ClrProfiler.Emit;
using SignalFx.Tracing;
using SignalFx.Tracing.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    /// <summary>
    /// Tracer integration for Kafka Produce method.
    /// </summary>
    public static class ProduceKafkaIntegration
    {
        private const string ProduceSyncOperationName = "kafka.produce";

        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.GetLogger(typeof(ProduceKafkaIntegration));

        /// <summary>
        /// Traces a synchronous Produce call to Kafka.
        /// </summary>
        /// <param name="producer">The producer for the original method.</param>
        /// <param name="topic">The topic to produce the message to.</param>
        /// <param name="message">The message to produce.</param>
        /// <param name="deliveryHandler">A delegate that will be called with a delivery report corresponding to the produce request (if enabled).</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The original result</returns>
        [InterceptMethod(
            CallerAssembly = Constants.ConfluentKafkaAssemblyName,
            TargetAssembly = Constants.ConfluentKafkaAssemblyName,
            TargetType = Constants.ProducerType,
            TargetSignatureTypes = new[] { ClrNames.Void, Constants.TopicPartitionTypeName, Constants.MessageTypeName, Constants.ActionOfDeliveryReportTypeName },
            TargetMinimumVersion = Constants.MinimumVersion,
            TargetMaximumVersion = Constants.MaximumVersion)]
        public static object Produce(
            object producer,
            object topic,
            object message,
            object deliveryHandler,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            if (producer == null)
            {
                throw new ArgumentNullException(nameof(producer));
            }

            var scope = CreateScope(topic, message, ProduceSyncOperationName);

            var headers = KafkaHelper.GetPropertyValue<object>(message, "Headers");
            var headerAdapter = new KafkaHeadersCollectionAdapter(headers);
            Tracer.Instance.Propagator
                .Inject(scope.Span.Context, headerAdapter, (collectionAdapter, key, value) => collectionAdapter.Add(key, value));

            const string methodName = nameof(Produce);
            Func<object, object, object, object> produce;
            var producerType = producer.GetType();

            try
            {
                produce =
                    MethodBuilder<Func<object, object, object, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(producerType)
                       .WithParameters(topic, message, deliveryHandler)
                       .WithNamespaceAndNameFilters(ClrNames.Void, Constants.TopicPartitionTypeName, Constants.MessageTypeName, Constants.ActionOfDeliveryReportTypeName)
                       .Build();
            }
            catch (Exception ex)
            {
                // profiled app will not continue working as expected without this method
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: Constants.ProducerType,
                    methodName: methodName,
                    instanceType: producer.GetType().AssemblyQualifiedName);
                throw;
            }

            try
            {
                var returned = produce(topic, message, deliveryHandler);
                return returned;
            }
            catch (Exception ex) when (scope.Span.SetExceptionForFilter(ex))
            {
                // unreachable code
                throw;
            }
            finally
            {
                scope.Dispose();
            }
        }

        private static Scope CreateScope(string topicName, object message, string operationName)
        {
            if (!Tracer.Instance.Settings.IsIntegrationEnabled(Constants.IntegrationName))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            var tracer = Tracer.Instance;

            var parentSpan = tracer.ActiveScope?.Span;
            if (parentSpan is not null &&
                parentSpan.OperationName == operationName &&
                parentSpan.GetTag(Tags.KafkaTopic) == topicName)
            {
                // we are already instrumenting this
                return null;
            }

            Scope scope = null;
            try
            {
                scope = tracer.StartActive(operationName, serviceName: tracer.DefaultServiceName);
                var span = scope.Span;
                span.Type = SpanTypes.Kafka;
                span.SetTag(Tags.InstrumentationName, Constants.IntegrationName);
                span.SetTag(Tags.SpanKind, SpanKinds.Client);
                span.SetTag(Tags.KafkaTopic, topicName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }

        private static Scope CreateScope(object topicPartition, object message, string operationName)
        {
            var topicName = KafkaHelper.GetPropertyValue<string>(topicPartition, "Topic");
            return CreateScope(topicName, message, operationName);
        }
    }
}
