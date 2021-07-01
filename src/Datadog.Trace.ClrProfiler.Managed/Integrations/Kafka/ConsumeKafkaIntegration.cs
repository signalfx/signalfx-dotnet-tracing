using System;
using Datadog.Trace.ClrProfiler.Emit;
using SignalFx.Tracing;
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
            TargetSignatureTypes = new[] { Constants.ConsumeResultTypeName, ClrNames.Int32 },
            TargetMinimumVersion = Constants.MinimumVersion,
            TargetMaximumVersion = Constants.MaximumVersion)]
        public static object Consume(
            object consumer,
            int millisecondsTimeout,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            if (consumer == null)
            {
                throw new ArgumentNullException(nameof(consumer));
            }

            const string methodName = nameof(Consume);
            Func<object, int, object> consume;
            var consumerType = consumer.GetType();

            var activeScope = Tracer.Instance.ActiveScope;
            var currentSpan = activeScope?.Span;
            if (currentSpan?.OperationName == Constants.ConsumeSyncOperationName)
            {
                activeScope.Dispose();
            }

            try
            {
                consume =
                    MethodBuilder<Func<object, int, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(consumerType)
                       .WithParameters(millisecondsTimeout)
                       .WithNamespaceAndNameFilters("Confluent.Kafka.ConsumeResult`2", ClrNames.Int32)
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
                    instrumentedType: Constants.ConsumerType,
                    methodName: methodName,
                    instanceType: consumer.GetType().AssemblyQualifiedName);
                throw;
            }

            var result = consume(consumer, millisecondsTimeout);
            var scope = KafkaHelper.CreateConsumeScope(result);

            scope.Dispose();

            return result;
        }
    }
}
