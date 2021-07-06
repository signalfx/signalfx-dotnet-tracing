using System;
using Datadog.Trace.ClrProfiler.Emit;
using SignalFx.Tracing;
using SignalFx.Tracing.Vendors.Serilog;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal static class ConsumeKafkaIntegrationHelper
    {
        internal static object Consume<T>(
               object consumer,
               T input,
               int opCode,
               int mdToken,
               long moduleVersionPtr,
               ILogger log)
        {
            if (consumer == null)
            {
                throw new ArgumentNullException(nameof(consumer));
            }

            var inputType = typeof(T).FullName;
            const string methodName = ConfluentKafka.ConsumeSyncMethodName;
            Func<object, T, object> consume;
            var consumerType = consumer.GetType();

            var activeScope = Tracer.Instance.ActiveScope;
            var currentSpan = activeScope?.Span;
            if (currentSpan?.OperationName == ConfluentKafka.ConsumeSyncOperationName)
            {
                activeScope.Dispose();
            }

            try
            {
                consume =
                    MethodBuilder<Func<object, T, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(consumerType)
                       .WithParameters(input)
                       .WithNamespaceAndNameFilters("Confluent.Kafka.ConsumeResult`2", inputType)
                       .Build();
            }
            catch (Exception ex)
            {
                // profiled app will not continue working as expected without this method
                log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: ConfluentKafka.ConsumerType,
                    methodName: methodName,
                    instanceType: consumer.GetType().AssemblyQualifiedName);
                throw;
            }

            object result = null;
            Scope scope = null;
            DateTimeOffset startTimeOffset = DateTimeOffset.UtcNow;
            try
            {
                result = consume(consumer, input);
                scope = result != null
                    ? KafkaHelper.CreateConsumeScopeFromConsumerResult(result, consumer, startTimeOffset)
                    : KafkaHelper.CreateConsumeScopeFromConsumer(consumer, startTimeOffset);
            }
            catch (Exception ex)
            {
                var tracer = Tracer.Instance;
                if (tracer.Settings.IsIntegrationEnabled(ConfluentKafka.IntegrationName) && !KafkaHelper.AlreadyInstrumented())
                {
                    // Integration is enabled but the scope was not created since consume raised an exception.
                    // Create a span to record the exception with the available info.
                    scope = KafkaHelper.CreateConsumeScopeFromConsumer(consumer, startTimeOffset);
                    if (ex is not OperationCanceledException)
                    {
                        // OperationCanceledException is expected in case of a clean shutdown and shouldn't
                        // be reported as an exception.
                        scope.Span.SetException(ex);
                    }
                }

                throw;
            }
            finally
            {
                scope?.Dispose();
            }

            return result;
        }
    }
}
