// Modified by SignalFx

using SignalFx.Tracing.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    /// <summary>
    /// Tracer integration for Kafka Produce method.
    /// </summary>
    public static class ProduceKafkaIntegration
    {
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
        [InterceptMethod(
            TargetAssembly = ConfluentKafka.AssemblyName,
            TargetType = ConfluentKafka.IProducerTypeName,
            TargetMethod = ConfluentKafka.ProduceSyncMethodName,
            TargetSignatureTypes = new[] { ClrNames.Void, ConfluentKafka.TopicPartitionTypeName, ConfluentKafka.MessageTypeName, ConfluentKafka.ActionOfDeliveryReportTypeName },
            TargetMinimumVersion = ConfluentKafka.MinimumVersion,
            TargetMaximumVersion = ConfluentKafka.MaximumVersion)]
        public static void ProduceWithTopicPartitionTopic(
            object producer,
            object topic,
            object message,
            object deliveryHandler,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            ProduceKafkaIntegrationHelper.Produce(
                producer,
                topic,
                message,
                deliveryHandler,
                opCode,
                mdToken,
                moduleVersionPtr,
                ConfluentKafka.TopicPartitionTypeName,
                Log);
        }

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
        [InterceptMethod(
            TargetAssembly = ConfluentKafka.AssemblyName,
            TargetType = ConfluentKafka.IProducerTypeName,
            TargetMethod = ConfluentKafka.ProduceSyncMethodName,
            TargetSignatureTypes = new[] { ClrNames.Void, ClrNames.String, ConfluentKafka.MessageTypeName, ConfluentKafka.ActionOfDeliveryReportTypeName },
            TargetMinimumVersion = ConfluentKafka.MinimumVersion,
            TargetMaximumVersion = ConfluentKafka.MaximumVersion)]
        public static void ProduceWithStringTopic(
            object producer,
            object topic,
            object message,
            object deliveryHandler,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            ProduceKafkaIntegrationHelper.Produce(
                producer,
                topic,
                message,
                deliveryHandler,
                opCode,
                mdToken,
                moduleVersionPtr,
                ClrNames.String,
                Log);
        }

        /// <summary>
        /// Traces an asynchronous Produce call to Kafka.
        /// </summary>
        /// <param name="producer">The producer for the original method.</param>
        /// <param name="topic">The topic to produce the message to.</param>
        /// <param name="message">The message to produce.</param>
        /// <param name="cancellationToken">A cancellation token to observe whilst waiting the returned task to complete.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The original result</returns>
        [InterceptMethod(
            TargetAssembly = ConfluentKafka.AssemblyName,
            TargetType = ConfluentKafka.IProducerTypeName,
            TargetMethod = ConfluentKafka.ProduceAsyncMethodName,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<Confluent.Kafka.DeliveryResult`2<T, T>>", ConfluentKafka.TopicPartitionTypeName, ConfluentKafka.MessageTypeName, ClrNames.CancellationToken },
            TargetMinimumVersion = ConfluentKafka.MinimumVersion,
            TargetMaximumVersion = ConfluentKafka.MaximumVersion)]
        public static object ProduceAsyncTopicPartitionTopic(
            object producer,
            object topic,
            object message,
            object cancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return ProduceKafkaIntegrationHelper.ProduceAsync(
                producer,
                topic,
                message,
                cancellationToken,
                opCode,
                mdToken,
                moduleVersionPtr,
                ConfluentKafka.TopicPartitionTypeName,
                Log);
        }

        /// <summary>
        /// Traces an asynchronous Produce call to Kafka.
        /// </summary>
        /// <param name="producer">The producer for the original method.</param>
        /// <param name="topic">The topic to produce the message to.</param>
        /// <param name="message">The message to produce.</param>
        /// <param name="cancellationToken">A cancellation token to observe whilst waiting the returned task to complete.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The original result</returns>
        [InterceptMethod(
            TargetAssembly = ConfluentKafka.AssemblyName,
            TargetType = ConfluentKafka.IProducerTypeName,
            TargetMethod = ConfluentKafka.ProduceAsyncMethodName,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<Confluent.Kafka.DeliveryResult`2<T, T>>", ClrNames.String, ConfluentKafka.MessageTypeName, ClrNames.CancellationToken },
            TargetMinimumVersion = ConfluentKafka.MinimumVersion,
            TargetMaximumVersion = ConfluentKafka.MaximumVersion)]
        public static object ProduceAsyncWithStringTopic(
            object producer,
            object topic,
            object message,
            object cancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return ProduceKafkaIntegrationHelper.ProduceAsync(
                producer,
                topic,
                message,
                cancellationToken,
                opCode,
                mdToken,
                moduleVersionPtr,
                ClrNames.String,
                Log);
        }
    }
}
