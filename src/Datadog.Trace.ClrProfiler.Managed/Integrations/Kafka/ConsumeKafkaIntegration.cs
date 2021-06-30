using System;
using System.Globalization;
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
        private const string ConsumeSyncOperationName = "kafka.consume";

        private static readonly ILogger Log = SignalFxLogging.GetLogger(typeof(ConsumeKafkaIntegration));

        /// <summary>
        /// Traces a synchronous Consume call to Kafka.
        /// </summary>
        /// <param name="consumer">The consumer for the original method.</param>
        /// <param name="boxedMillisecondsTimeout">The wait timeout in ms.</param>
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
            object boxedMillisecondsTimeout,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            Log.Information(">>> ENTERING CONSUME INTERCEPT");

            if (consumer == null)
            {
                throw new ArgumentNullException(nameof(consumer));
            }

            const string methodName = nameof(Consume);
            Func<object, int, object> consume;
            var consumerType = consumer.GetType();

            var activeScope = Tracer.Instance.ActiveScope;
            var currentSpan = activeScope?.Span;
            if (currentSpan?.OperationName == ConsumeSyncOperationName)
            {
                activeScope.Dispose();
            }

            try
            {
                Log.Information(">>> Building consume method");
                consume =
                    MethodBuilder<Func<object, int, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(consumerType)
                       .WithParameters(10000)
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

            Log.Information(">>> Calling consume method");
            var result = consume(consumer, 10000);
            Log.Information(">>> Creating scope");
            var scope = CreateScope(result);

            scope.Dispose();

            return result;
        }

        private static Scope CreateScope(object result)
        {
            var tracer = Tracer.Instance;
            if (!tracer.Settings.IsIntegrationEnabled(Constants.IntegrationName))
            {
                // integration disabled, don't create a scope/span, skip this trace
                return null;
            }

            var parent = tracer.ActiveScope?.Span;
            if (parent is not null &&
                parent.OperationName == ConsumeSyncOperationName &&
                parent.GetTag(Tags.InstrumentationName) != null)
            {
                return null;
            }

            SpanContext propagatedContext = null;

            // Try to extract propagated context from headers.
            var headers = KafkaHelper.GetPropertyValue<object>(result, "Headers");
            if (headers != null)
            {
                var headersAdapter = new KafkaHeadersCollectionAdapter(headers);

                try
                {
                    propagatedContext = tracer.Propagator
                        .Extract(headersAdapter, (h, name) => h.GetValues(name));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error extracting propagated headers from Kafka message");
                }
            }

            Scope scope = null;
            try
            {
                var message = KafkaHelper.GetPropertyValue<object>(result, "Message");
                var topicName = KafkaHelper.GetPropertyValue<string>(result, "Topic");
                var partition = KafkaHelper.GetPropertyValue<object>(result, "Partition");

                scope = tracer.StartActive(ConsumeSyncOperationName, propagatedContext, tracer.DefaultServiceName);
                var span = scope.Span;
                if (partition is not null)
                {
                    span.Tags.Add(Tags.KafkaPartition, partition.ToString());
                }

                if (message is not null)
                {
                    var timestamp = KafkaHelper.GetPropertyValue<object>(result, "Timestamp");
                    if (timestamp is not null)
                    {
                        var dateTime = KafkaHelper.GetPropertyValue<DateTime>(timestamp, "UtcDateTime");
                        if (dateTime != default)
                        {
                            var consumeTime = span.StartTime.UtcDateTime;
                            var messageQueueTimeMs = Math.Max(0, (consumeTime - dateTime).TotalMilliseconds);
                            span.Tags.Add(Tags.KafkaMessageQueueTimeMs, messageQueueTimeMs.ToString(CultureInfo.InvariantCulture));
                        }
                    }

                    var value = KafkaHelper.GetPropertyValue<object>(message, "Value");
                    span.Tags.Add(Tags.KafkaTombstone, value is null ? "true" : "false");

                    if (!string.IsNullOrEmpty(topicName))
                    {
                        span.Tags.Add(Tags.KafkaTopic, topicName);
                    }
                }

                span.Type = SpanTypes.Kafka;
                span.SetTag(Tags.InstrumentationName, Constants.IntegrationName);
                span.SetTag(Tags.SpanKind, SpanKinds.Client);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }
    }
}
