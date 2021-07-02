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
            TargetAssembly = Constants.ConfluentKafkaAssemblyName,
            TargetType = Constants.ConsumerType,
            TargetMethod = Constants.ConsumeSyncMethodName,
            TargetSignatureTypes = new[] { Constants.ConsumeResultTypeName, ClrNames.Int32 },
            TargetMinimumVersion = Constants.MinimumVersion,
            TargetMaximumVersion = Constants.MaximumVersion)]
        public static object ConsumeInt32(
            object consumer,
            int millisecondsTimeout,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            System.Console.WriteLine(">>>> ConsumeInt32");
            return ConsumeKafkaIntegrationHelper.Consume(
                consumer,
                millisecondsTimeout,
                opCode,
                mdToken,
                moduleVersionPtr,
                ClrNames.Int32,
                Log);
        }

        /// <summary>
        /// Traces a synchronous Consume call to Kafka.
        /// </summary>
        /// <param name="consumer">The consumer for the original method.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The original result</returns>
        [InterceptMethod(
            TargetAssembly = Constants.ConfluentKafkaAssemblyName,
            TargetType = Constants.ConsumerType,
            TargetMethod = Constants.ConsumeSyncMethodName,
            TargetSignatureTypes = new[] { Constants.ConsumeResultTypeName, ClrNames.CancellationToken },
            TargetMinimumVersion = Constants.MinimumVersion,
            TargetMaximumVersion = Constants.MaximumVersion)]
        public static object ConsumeCancellationToken(
            object consumer,
            object cancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            System.Console.WriteLine(">>>> ConsumeCancellationToken");
            return ConsumeKafkaIntegrationHelper.Consume(
                consumer,
                (CancellationToken)cancellationToken,
                opCode,
                mdToken,
                moduleVersionPtr,
                ClrNames.CancellationToken,
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
            TargetAssembly = Constants.ConfluentKafkaAssemblyName,
            TargetType = Constants.ConsumerType,
            TargetMethod = Constants.ConsumeSyncMethodName,
            TargetSignatureTypes = new[] { Constants.ConsumeResultTypeName, ClrNames.TimeSpan },
            TargetMinimumVersion = Constants.MinimumVersion,
            TargetMaximumVersion = Constants.MaximumVersion)]
        public static object ConsumeTimeSpan(
            object consumer,
            object boxedTimeSpan,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            Console.WriteLine(">>>> ConsumeTimeSpan");
            return ConsumeKafkaIntegrationHelper.Consume(
                consumer,
                (TimeSpan)boxedTimeSpan,
                opCode,
                mdToken,
                moduleVersionPtr,
                ClrNames.TimeSpan,
                Log);
        }
    }
}
