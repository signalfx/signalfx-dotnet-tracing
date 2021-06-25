// Modified by SignalFx

using System;
using Datadog.Trace.ClrProfiler.Emit;
using SignalFx.Tracing;
using SignalFx.Tracing.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// Tracer integration for Kafka.
    /// </summary>
    public static class KafkaIntegration
    {
        private const string IntegrationName = "Kafka";
        private const string ProducerType = "Confluent.Kafka.Producer`2";
        private const string ConfluentKafkaAssemblyName = "Confluent.Kafka";
        private const string MinimumVersion = "1.4.0";
        private const string MaximumVersion = "1.*.*";

        private const string TopicPartitionTypeName = "Confluent.Kafka.TopicPartition";
        private const string MessageTypeName = "Confluent.Kafka.Message`2[!0,!1]";
        private const string ActionOfDeliveryReportTypeName = "System.Action`1[Confluent.Kafka.DeliveryReport`2[!0,!1]]";

        private const string ProduceOperationName = "kafka.produce";

        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.GetLogger(typeof(KafkaIntegration));

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
            CallerAssembly = ConfluentKafkaAssemblyName,
            TargetAssembly = ConfluentKafkaAssemblyName,
            TargetType = ProducerType,
            TargetSignatureTypes = new[] { ClrNames.Void, TopicPartitionTypeName, MessageTypeName, ActionOfDeliveryReportTypeName },
            TargetMinimumVersion = MinimumVersion,
            TargetMaximumVersion = MaximumVersion)]
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
                       .WithNamespaceAndNameFilters(ClrNames.Void, TopicPartitionTypeName, MessageTypeName, ActionOfDeliveryReportTypeName)
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
                    instrumentedType: ProducerType,
                    methodName: methodName,
                    instanceType: producer.GetType().AssemblyQualifiedName);
                throw;
            }

            using var scope = CreateScope(topic);
            try
            {
                var returned = produce(topic, message, deliveryHandler);
                return returned;
            }
            catch (Exception ex) when (scope?.Span.SetExceptionForFilter(ex) ?? false)
            {
                // unreachable code
                throw;
            }
        }

        private static Scope CreateScope(object topic)
        {
            if (!Tracer.Instance.Settings.IsIntegrationEnabled(IntegrationName))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            var tracer = Tracer.Instance;
            if (topic.TryGetPropertyValue("Topic", out string topicName))
            {
                topicName = string.Empty;
                Log.Warning("Unable to access DatabaseName property.");
            }

            var parentSpan = tracer.ActiveScope?.Span;

            if (parentSpan != null &&
                parentSpan.GetTag(Tags.DbType) == SpanTypes.MongoDb &&
                parentSpan.OperationName == ProduceOperationName &&
                parentSpan.GetTag(Tags.KafkaTopic) == topicName)
            {
                // we are already instrumenting this,
                return null;
            }

            Scope scope = null;
            try
            {
                scope = tracer.StartActive(ProduceOperationName, serviceName: tracer.DefaultServiceName);
                var span = scope.Span;
                span.Type = SpanTypes.Kafka;
                span.SetTag(Tags.InstrumentationName, IntegrationName);
                span.SetTag(Tags.SpanKind, SpanKinds.Client);
                span.SetTag(Tags.KafkaTopic, topicName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }
    }
}
