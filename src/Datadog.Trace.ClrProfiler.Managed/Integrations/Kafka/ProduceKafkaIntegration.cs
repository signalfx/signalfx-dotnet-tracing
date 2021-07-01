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
        /// <returns>The original result</returns>
        [InterceptMethod(
            TargetAssembly = Constants.ConfluentKafkaAssemblyName,
            TargetType = Constants.ProducerType,
            TargetMethod = Constants.ProduceSyncMethodName,
            TargetSignatureTypes = new[] { ClrNames.Void, Constants.TopicPartitionTypeName, Constants.MessageTypeName, Constants.ActionOfDeliveryReportTypeName },
            TargetMinimumVersion = Constants.MinimumVersion,
            TargetMaximumVersion = Constants.MaximumVersion)]
        public static object ProduceWithTopicPartitionTopic(
            object producer,
            object topic,
            object message,
            object deliveryHandler,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return ProducKafkaIntegrationHelper.Produce(
                producer,
                topic,
                message,
                deliveryHandler,
                opCode,
                mdToken,
                moduleVersionPtr,
                Constants.ProduceSyncOperationName,
                Constants.TopicPartitionTypeName,
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
        /// <returns>The original result</returns>
        [InterceptMethod(
            TargetAssembly = Constants.ConfluentKafkaAssemblyName,
            TargetType = Constants.ProducerType,
            TargetMethod = Constants.ProduceSyncMethodName,
            TargetSignatureTypes = new[] { ClrNames.Void, ClrNames.String, Constants.MessageTypeName, Constants.ActionOfDeliveryReportTypeName },
            TargetMinimumVersion = Constants.MinimumVersion,
            TargetMaximumVersion = Constants.MaximumVersion)]
        public static object ProduceWithStringTopic(
            object producer,
            object topic,
            object message,
            object deliveryHandler,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return ProducKafkaIntegrationHelper.Produce(
                producer,
                topic,
                message,
                deliveryHandler,
                opCode,
                mdToken,
                moduleVersionPtr,
                Constants.ProduceSyncOperationName,
                ClrNames.String,
                Log);
        }
    }
}
