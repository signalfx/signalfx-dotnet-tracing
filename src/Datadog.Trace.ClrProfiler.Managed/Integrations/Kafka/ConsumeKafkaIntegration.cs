using System;
using System.Threading;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Vendors.Serilog;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    /// <summary>
    /// Tracer integration for Kafka Consume method.
    /// </summary>
    public static class ConsumeKafkaIntegration
    {
        private static readonly ILogger Log = SignalFxLogging.GetLogger(typeof(ConsumeKafkaIntegration));

        /// <summary>
        /// Traces a synchronous Consume call to Kafka.
        /// </summary>
        /// <param name="consumer">The consumer for the original method.</param>
        /// <param name="millisecondsTimeout">The wait timeout in ms.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The original result</returns>
        [InterceptMethod(
            TargetAssembly = ConfluentKafka.AssemblyName,
            TargetType = ConfluentKafka.ConsumerType,
            TargetMethod = ConfluentKafka.ConsumeSyncMethodName,
            TargetSignatureTypes = new[] { ConfluentKafka.ConsumeResultTypeName, ClrNames.Int32 },
            TargetMinimumVersion = ConfluentKafka.MinimumVersion,
            TargetMaximumVersion = ConfluentKafka.MaximumVersion)]
        public static object ConsumeInt32(
            object consumer,
            int millisecondsTimeout,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return ConsumeKafkaIntegrationHelper.Consume(
                consumer,
                millisecondsTimeout,
                opCode,
                mdToken,
                moduleVersionPtr,
                Log);
        }

        /// <summary>
        /// Traces a synchronous Consume call to Kafka.
        /// </summary>
        /// <param name="consumer">The consumer for the original method.</param>
        /// <param name="boxedCancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The original result</returns>
        [InterceptMethod(
            TargetAssembly = ConfluentKafka.AssemblyName,
            TargetType = ConfluentKafka.ConsumerType,
            TargetMethod = ConfluentKafka.ConsumeSyncMethodName,
            TargetSignatureTypes = new[] { ConfluentKafka.ConsumeResultTypeName, ClrNames.CancellationToken },
            TargetMinimumVersion = ConfluentKafka.MinimumVersion,
            TargetMaximumVersion = ConfluentKafka.MaximumVersion)]
        public static object ConsumeCancellationToken(
            object consumer,
            object boxedCancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return ConsumeKafkaIntegrationHelper.Consume(
                consumer,
                (CancellationToken)boxedCancellationToken,
                opCode,
                mdToken,
                moduleVersionPtr,
                Log);
        }

        /// <summary>
        /// Traces a synchronous Consume call to Kafka.
        /// </summary>
        /// <param name="consumer">The consumer for the original method.</param>
        /// <param name="boxedTimeSpan">The <see cref="TimeSpan"/>.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The original result</returns>
        [InterceptMethod(
            TargetAssembly = ConfluentKafka.AssemblyName,
            TargetType = ConfluentKafka.ConsumerType,
            TargetMethod = ConfluentKafka.ConsumeSyncMethodName,
            TargetSignatureTypes = new[] { ConfluentKafka.ConsumeResultTypeName, ClrNames.TimeSpan },
            TargetMinimumVersion = ConfluentKafka.MinimumVersion,
            TargetMaximumVersion = ConfluentKafka.MaximumVersion)]
        public static object ConsumeTimeSpan(
            object consumer,
            object boxedTimeSpan,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            return ConsumeKafkaIntegrationHelper.Consume(
                consumer,
                (TimeSpan)boxedTimeSpan,
                opCode,
                mdToken,
                moduleVersionPtr,
                Log);
        }
    }
}
