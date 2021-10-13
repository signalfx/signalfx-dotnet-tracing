using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Datadog.Trace.ClrProfiler.Emit;
using SignalFx.Tracing;
using SignalFx.Tracing.DuckTyping;
using SignalFx.Tracing.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// Tracing integration for RabbitMQ.Client
    /// </summary>
    public static class RabbitMQIntegration
    {
        internal const string IntegrationName = "RabbitMQ";

        private const string OperationName = "amqp.command";
        private const string SystemName = "rabbitmq";

        private const string Major3Minor6Patch9 = "3.6.9";
        private const string Major5 = "5";
        private const string Major6 = "6";
        private const string RabbitMQAssembly = "RabbitMQ.Client";
        private const string RabbitMQIModel = "RabbitMQ.Client.IModel";
        private const string RabbitMQImplModelBase = "RabbitMQ.Client.Impl.ModelBase";
        private const string RabbitMQDefaultBasicConsumer = "RabbitMQ.Client.DefaultBasicConsumer";
        private const string AmqpBasicDeliverCommand = "basic.deliver";
        private const string AmqpBasicGetCommand = "basic.get";
        private const string AmqpBasicPublishCommand = "basic.publish";

        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.GetLogger(typeof(RabbitMQIntegration));
        private static readonly string[] DeliveryModeStrings = { null, "1", "2" };

        private static Action<IDictionary<string, object>, string, string> headersSetter = ((carrier, key, value) =>
        {
            carrier[key] = Encoding.UTF8.GetBytes(value);
        });

        private static Func<IDictionary<string, object>, string, IEnumerable<string>> headersGetter = ((carrier, key) =>
        {
            if (carrier.TryGetValue(key, out object value) && value is byte[] bytes)
            {
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        });

        /// <summary>
        /// Wrap the original method by adding instrumentation code around it
        /// </summary>
        /// <param name="model">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="consumerTag">The original consumerTag argument</param>
        /// <param name="deliveryTag">The original deliveryTag argument</param>
        /// <param name="redelivered">The original redelivered argument</param>
        /// <param name="exchange">Name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="basicProperties">The message properties.</param>
        /// <param name="body">The message body.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        [InterceptMethod(
            TargetAssembly = RabbitMQAssembly,
            TargetType = RabbitMQDefaultBasicConsumer,
            TargetMethod = "HandleBasicDeliver",
            TargetSignatureTypes = new[] { ClrNames.Void, ClrNames.String, ClrNames.UInt64, ClrNames.Bool, ClrNames.String, ClrNames.String, ClrNames.Ignore, ClrNames.Ignore },
            TargetMinimumVersion = Major3Minor6Patch9,
            TargetMaximumVersion = Major5)]
        public static void BasicDeliver(
            object model,
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            object basicProperties,
            byte[] body,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            if (model == null) { throw new ArgumentNullException(nameof(model)); }

            const string methodName = "HandleBasicDeliver";
            Action<object, string, ulong, bool, string, string, object, byte[]> instrumentedMethod;
            var modelType = model.GetType();

            try
            {
                instrumentedMethod =
                    MethodBuilder<Action<object, string, ulong, bool, string, string, object, byte[]>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(modelType)
                       .WithParameters(consumerTag, deliveryTag, redelivered, exchange, routingKey, basicProperties, body)
                       .WithNamespaceAndNameFilters(ClrNames.Void, ClrNames.String, ClrNames.UInt64, ClrNames.Bool, ClrNames.String, ClrNames.String, ClrNames.Ignore, ClrNames.Ignore)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: RabbitMQDefaultBasicConsumer,
                    methodName: methodName,
                    instanceType: modelType.AssemblyQualifiedName);
                throw;
            }

            SpanContext propagatedContext = null;
            if (basicProperties.TryDuckCast<IBasicProperties>(out var basicPropertiesValue))
            {
                // try to extract propagated context values from headers
                if (basicPropertiesValue.Headers != null)
                {
                    try
                    {
                        propagatedContext = Tracer.Instance.Propagator.Extract(basicPropertiesValue.Headers, headersGetter);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error extracting propagated headers.");
                    }
                }

                using (var scope = CreateScope(Tracer.Instance, AmqpBasicDeliverCommand, parentContext: propagatedContext, exchange: exchange, routingKey: routingKey))
                {
                    var messageSize = body?.Length.ToString(CultureInfo.InvariantCulture) ?? "0";
                    scope?.Span.Tags.Add(Tags.Messaging.MessagePayloadSizeBytes, messageSize);

                    try
                    {
                        instrumentedMethod(model, consumerTag, deliveryTag, redelivered, exchange, routingKey, basicProperties, body);
                    }
                    catch (Exception ex)
                    {
                        scope?.Span.SetException(ex);
                        throw;
                    }
                }
            }
            else
            {
                instrumentedMethod(model, consumerTag, deliveryTag, redelivered, exchange, routingKey, basicProperties, body);
            }
        }

        /// <summary>
        /// Wrap the original method by adding instrumentation code around it
        /// </summary>
        /// <param name="model">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="consumerTag">The original consumerTag argument</param>
        /// <param name="deliveryTag">The original deliveryTag argument</param>
        /// <param name="redelivered">The original redelivered argument</param>
        /// <param name="exchange">Name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="basicProperties">The message properties.</param>
        /// <param name="body">The message body.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        [InterceptMethod(
            TargetAssembly = RabbitMQAssembly,
            TargetType = RabbitMQDefaultBasicConsumer,
            TargetMethod = "HandleBasicDeliver",
            TargetSignatureTypes = new[] { ClrNames.Void, ClrNames.String, ClrNames.UInt64, ClrNames.Bool, ClrNames.String, ClrNames.String, ClrNames.Ignore, ClrNames.Ignore },
            TargetMinimumVersion = Major6,
            TargetMaximumVersion = Major6)]
        public static void BasicDeliverV6(
            object model,
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            object basicProperties,
            object body,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            if (model == null) { throw new ArgumentNullException(nameof(model)); }

            const string methodName = "HandleBasicDeliver";
            Action<object, string, ulong, bool, string, string, object, object> instrumentedMethod;
            var modelType = model.GetType();

            try
            {
                instrumentedMethod =
                    MethodBuilder<Action<object, string, ulong, bool, string, string, object, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(modelType)
                       .WithParameters(consumerTag, deliveryTag, redelivered, exchange, routingKey, basicProperties, body)
                       .WithNamespaceAndNameFilters(ClrNames.Void, ClrNames.String, ClrNames.UInt64, ClrNames.Bool, ClrNames.String, ClrNames.String, ClrNames.Ignore, ClrNames.Ignore)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: RabbitMQDefaultBasicConsumer,
                    methodName: methodName,
                    instanceType: modelType.AssemblyQualifiedName);
                throw;
            }

            SpanContext propagatedContext = null;
            if (basicProperties.TryDuckCast<IBasicProperties>(out var basicPropertiesValue))
            {
                // try to extract propagated context values from headers
                if (basicPropertiesValue.Headers != null)
                {
                    try
                    {
                        propagatedContext = Tracer.Instance.Propagator.Extract(basicPropertiesValue.Headers, headersGetter);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error extracting propagated headers.");
                    }
                }

                using (var scope = CreateScope(Tracer.Instance, AmqpBasicDeliverCommand, parentContext: propagatedContext, exchange: exchange, routingKey: routingKey))
                {
                    if (body != null && body.TryDuckCast<BodyStruct>(out var bodyStruct))
                    {
                        var messageSize = bodyStruct.Length.ToString(CultureInfo.InvariantCulture);
                        scope?.Span.Tags.Add(Tags.Messaging.MessagePayloadSizeBytes, messageSize);
                    }

                    try
                    {
                        instrumentedMethod(model, consumerTag, deliveryTag, redelivered, exchange, routingKey, basicProperties, body);
                    }
                    catch (Exception ex)
                    {
                        scope?.Span.SetException(ex);
                        throw;
                    }
                }
            }
            else
            {
                instrumentedMethod(model, consumerTag, deliveryTag, redelivered, exchange, routingKey, basicProperties, body);
            }
        }

        /// <summary>
        /// Wrap the original method by adding instrumentation code around it
        /// </summary>
        /// <param name="model">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="queue">The queue name of the message</param>
        /// <param name="autoAck">The original autoAck argument</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The original return value.</returns>
        [InterceptMethod(
            TargetAssembly = RabbitMQAssembly,
            TargetType = RabbitMQIModel,
            TargetMethod = "BasicGet",
            TargetSignatureTypes = new[] { ClrNames.Ignore, ClrNames.String, ClrNames.Bool },
            TargetMinimumVersion = Major3Minor6Patch9,
            TargetMaximumVersion = Major6)]
        public static object BasicGet(
            object model,
            string queue,
            bool autoAck,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            if (model == null) { throw new ArgumentNullException(nameof(model)); }

            const string methodName = "BasicGet";
            Func<object, string, bool, object> instrumentedMethod;
            var modelType = model.GetType();

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<object, string, bool, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(modelType)
                       .WithParameters(queue, autoAck)
                       .WithNamespaceAndNameFilters(ClrNames.Ignore, ClrNames.String, ClrNames.Bool)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: RabbitMQImplModelBase,
                    methodName: methodName,
                    instanceType: modelType.AssemblyQualifiedName);
                throw;
            }

            object result = null;
            Exception exception = null;
            DateTimeOffset startTime = DateTimeOffset.UtcNow; // Save the "start time" for the deferred Span creation

            try
            {
                // Defer the creation of the Span until the original method returns
                // because the incoming message may have distributed tracing headers
                result = instrumentedMethod(model, queue, autoAck);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                SpanContext propagatedContext = null;
                string messageSize = "0";

                if (result != null && result.TryDuckCast<BasicGetResultStruct>(out var basicGetResult))
                {
                    messageSize = basicGetResult.Body.Length.ToString(CultureInfo.InvariantCulture);
                    var basicPropertiesHeaders = basicGetResult.BasicProperties?.Headers;

                    // try to extract propagated context values from headers
                    if (basicPropertiesHeaders != null)
                    {
                        try
                        {
                            propagatedContext = Tracer.Instance.Propagator.Extract(basicPropertiesHeaders, headersGetter);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error extracting propagated headers.");
                        }
                    }
                }

                if (result != null || exception != null)
                {
                    // Only create the span to reported retrieved data or an exception.
                    using (var scope = CreateScope(Tracer.Instance, AmqpBasicGetCommand, parentContext: propagatedContext, queue: queue, startTime: startTime))
                    {
                        if (scope != null)
                        {
                            string queueDisplayName = string.IsNullOrEmpty(queue) || !queue.StartsWith("amq.gen-") ? queue : "<generated>";
                            scope.Span.ResourceName = $"{AmqpBasicGetCommand} {queueDisplayName}";

                            scope.Span.Tags.Add(Tags.Messaging.MessagePayloadSizeBytes, messageSize);

                            if (exception != null)
                            {
                                scope.Span.SetException(exception);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Wrap the original method by adding instrumentation code around it
        /// </summary>
        /// <param name="model">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="exchange">Name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="mandatory">The mandatory routing flag.</param>
        /// <param name="basicProperties">The message properties.</param>
        /// <param name="body">The message body.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        [InterceptMethod(
            TargetAssembly = RabbitMQAssembly,
            TargetType = RabbitMQImplModelBase,
            TargetMethod = "_Private_BasicPublish",
            TargetSignatureTypes = new[] { ClrNames.Void, ClrNames.String, ClrNames.String, ClrNames.Bool, ClrNames.Ignore, ClrNames.Ignore },
            TargetMinimumVersion = Major3Minor6Patch9,
            TargetMaximumVersion = Major5)]
        public static void BasicPublish(
            object model,
            string exchange,
            string routingKey,
            bool mandatory,
            object basicProperties,
            byte[] body,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            if (model == null) { throw new ArgumentNullException(nameof(model)); }

            const string methodName = "_Private_BasicPublish";
            Action<object, string, string, bool, object, byte[]> instrumentedMethod;
            var modelType = model.GetType();

            try
            {
                instrumentedMethod =
                    MethodBuilder<Action<object, string, string, bool, object, byte[]>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(modelType)
                       .WithParameters(exchange, routingKey, mandatory, basicProperties, body)
                       .WithNamespaceAndNameFilters(ClrNames.Void, ClrNames.String, ClrNames.String, ClrNames.Bool, ClrNames.Ignore, ClrNames.Ignore)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: RabbitMQImplModelBase,
                    methodName: methodName,
                    instanceType: modelType.AssemblyQualifiedName);
                throw;
            }

            using (var scope = CreateScope(Tracer.Instance, AmqpBasicPublishCommand, exchange: exchange, routingKey: routingKey))
            {
                if (scope != null)
                {
                    var messageSize = body?.Length.ToString(CultureInfo.InvariantCulture) ?? "0";
                    var tags = scope.Span.Tags;
                    tags.Add(Tags.Messaging.MessagePayloadSizeBytes, messageSize);

                    if (basicProperties != null && basicProperties.TryDuckCast<IBasicProperties>(out var basicPropertiesValue))
                    {
                        if (basicPropertiesValue.IsDeliveryModePresent())
                        {
                            tags.Add(Tags.RabbitMQ.DeliveryMode, DeliveryModeStrings[0x3 & basicPropertiesValue.DeliveryMode]);
                        }

                        // add distributed tracing headers to the message
                        if (basicPropertiesValue.Headers == null)
                        {
                            basicPropertiesValue.Headers = new Dictionary<string, object>();
                        }

                        Tracer.Instance.Propagator.Inject(scope.Span.Context, basicPropertiesValue.Headers, headersSetter);
                    }
                }

                try
                {
                    instrumentedMethod(model, exchange, routingKey, mandatory, basicProperties, body);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Wrap the original method by adding instrumentation code around it
        /// </summary>
        /// <param name="model">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="exchange">Name of the exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="mandatory">The mandatory routing flag.</param>
        /// <param name="basicProperties">The message properties.</param>
        /// <param name="body">The message body.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        [InterceptMethod(
            TargetAssembly = RabbitMQAssembly,
            TargetType = RabbitMQImplModelBase,
            TargetMethod = "_Private_BasicPublish",
            TargetSignatureTypes = new[] { ClrNames.Void, ClrNames.String, ClrNames.String, ClrNames.Bool, ClrNames.Ignore, ClrNames.Ignore },
            TargetMinimumVersion = Major6,
            TargetMaximumVersion = Major6)]
        public static void BasicPublishV6(
            object model,
            string exchange,
            string routingKey,
            bool mandatory,
            object basicProperties,
            object body,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            if (model == null) { throw new ArgumentNullException(nameof(model)); }

            const string methodName = "_Private_BasicPublish";
            Action<object, string, string, bool, object, object> instrumentedMethod;
            var modelType = model.GetType();

            try
            {
                instrumentedMethod =
                    MethodBuilder<Action<object, string, string, bool, object, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(modelType)
                       .WithParameters(exchange, routingKey, mandatory, basicProperties, body)
                       .WithNamespaceAndNameFilters(ClrNames.Void, ClrNames.String, ClrNames.String, ClrNames.Bool, ClrNames.Ignore, ClrNames.Ignore)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: RabbitMQImplModelBase,
                    methodName: methodName,
                    instanceType: modelType.AssemblyQualifiedName);
                throw;
            }

            using (var scope = CreateScope(Tracer.Instance, AmqpBasicPublishCommand, exchange: exchange, routingKey: routingKey))
            {
                if (scope != null)
                {
                    string messageSize = body != null && body.TryDuckCast<BodyStruct>(out var bodyStruct)
                        ? bodyStruct.Length.ToString(CultureInfo.InvariantCulture)
                        : "0";
                    var tags = scope.Span.Tags;
                    tags.Add(Tags.Messaging.MessagePayloadSizeBytes, messageSize);

                    if (basicProperties != null && basicProperties.TryDuckCast<IBasicProperties>(out var basicPropertiesValue))
                    {
                        if (basicPropertiesValue.IsDeliveryModePresent())
                        {
                            tags.Add(Tags.RabbitMQ.DeliveryMode, DeliveryModeStrings[0x3 & basicPropertiesValue.DeliveryMode]);
                        }

                        // add distributed tracing headers to the message
                        if (basicPropertiesValue.Headers == null)
                        {
                            basicPropertiesValue.Headers = new Dictionary<string, object>();
                        }

                        Tracer.Instance.Propagator.Inject(scope.Span.Context, basicPropertiesValue.Headers, headersSetter);
                    }
                }

                try
                {
                    instrumentedMethod(model, exchange, routingKey, mandatory, basicProperties, body);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Wrap the original method by adding instrumentation code around it
        /// </summary>
        /// <param name="model">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="exchange">Name of the exchange.</param>
        /// <param name="type">Type of the exchange.</param>
        /// <param name="passive">The original passive setting</param>
        /// <param name="durable">The original durable setting</param>
        /// <param name="autoDelete">The original autoDelete setting</param>
        /// <param name="internal">The original internal setting</param>
        /// <param name="nowait">The original nowait setting</param>
        /// <param name="arguments">The original arguments setting</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        [InterceptMethod(
            TargetAssembly = RabbitMQAssembly,
            TargetType = RabbitMQImplModelBase,
            TargetMethod = "_Private_ExchangeDeclare",
            TargetSignatureTypes = new[] { ClrNames.Void, ClrNames.String, ClrNames.String, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Ignore },
            TargetMinimumVersion = Major3Minor6Patch9,
            TargetMaximumVersion = Major6)]
        public static void ExchangeDeclare(
            object model,
            string exchange,
            string type,
            bool passive,
            bool durable,
            bool autoDelete,
            bool @internal,
            bool nowait,
            object arguments,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            if (model == null) { throw new ArgumentNullException(nameof(model)); }

            const string methodName = "_Private_ExchangeDeclare";
            const string command = "exchange.declare";
            Action<object, string, string, bool, bool, bool, bool, bool, object> instrumentedMethod;
            var modelType = model.GetType();

            try
            {
                instrumentedMethod =
                    MethodBuilder<Action<object, string, string, bool, bool, bool, bool, bool, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(modelType)
                       .WithParameters(exchange, type, passive, durable, autoDelete, @internal, nowait, arguments)
                       .WithNamespaceAndNameFilters(ClrNames.Void, ClrNames.String, ClrNames.String, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Ignore)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: RabbitMQImplModelBase,
                    methodName: methodName,
                    instanceType: modelType.AssemblyQualifiedName);
                throw;
            }

            using (var scope = CreateScope(Tracer.Instance, command, exchange: exchange))
            {
                try
                {
                    instrumentedMethod(model, exchange, type, passive, durable, autoDelete, @internal, nowait, arguments);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Wrap the original method by adding instrumentation code around it
        /// </summary>
        /// <param name="model">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="queue">Name of the queue.</param>
        /// <param name="exchange">The original exchange argument.</param>
        /// <param name="routingKey">The original routingKey argument.</param>
        /// <param name="nowait">The original nowait argument.</param>
        /// <param name="arguments">The original arguments setting</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        [InterceptMethod(
            TargetAssembly = RabbitMQAssembly,
            TargetType = RabbitMQImplModelBase,
            TargetMethod = "_Private_QueueBind",
            TargetSignatureTypes = new[] { ClrNames.Void, ClrNames.String, ClrNames.String, ClrNames.String, ClrNames.Bool, ClrNames.Ignore },
            TargetMinimumVersion = Major3Minor6Patch9,
            TargetMaximumVersion = Major6)]
        public static void QueueBind(
            object model,
            string queue,
            string exchange,
            string routingKey,
            bool nowait,
            object arguments,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            if (model == null) { throw new ArgumentNullException(nameof(model)); }

            const string methodName = "_Private_QueueBind";
            const string command = "queue.bind";
            Action<object, string, string, string, bool, object> instrumentedMethod;
            var modelType = model.GetType();

            try
            {
                instrumentedMethod =
                    MethodBuilder<Action<object, string, string, string, bool, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(modelType)
                       .WithParameters(queue, exchange, routingKey, nowait, arguments)
                       .WithNamespaceAndNameFilters(ClrNames.Void, ClrNames.String, ClrNames.String, ClrNames.String, ClrNames.Bool, ClrNames.Ignore)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: RabbitMQImplModelBase,
                    methodName: methodName,
                    instanceType: modelType.AssemblyQualifiedName);
                throw;
            }

            using (var scope = CreateScope(Tracer.Instance, command, queue: queue, exchange: exchange, routingKey: routingKey))
            {
                try
                {
                    instrumentedMethod(model, queue, exchange, routingKey, nowait, arguments);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Wrap the original method by adding instrumentation code around it
        /// </summary>
        /// <param name="model">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="queue">Name of the queue.</param>
        /// <param name="passive">The original passive setting</param>
        /// <param name="durable">The original duable setting</param>
        /// <param name="exclusive">The original exclusive settings</param>
        /// <param name="autoDelete">The original autoDelete setting</param>
        /// <param name="nowait">The original nowait setting</param>
        /// <param name="arguments">The original arguments setting</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        [InterceptMethod(
            TargetAssembly = RabbitMQAssembly,
            TargetType = RabbitMQImplModelBase,
            TargetMethod = "_Private_QueueDeclare",
            TargetSignatureTypes = new[] { ClrNames.Void, ClrNames.String, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Ignore },
            TargetMinimumVersion = Major3Minor6Patch9,
            TargetMaximumVersion = Major6)]
        public static void QueueDeclare(
            object model,
            string queue,
            bool passive,
            bool durable,
            bool exclusive,
            bool autoDelete,
            bool nowait,
            object arguments,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            if (model == null) { throw new ArgumentNullException(nameof(model)); }

            const string methodName = "_Private_QueueDeclare";
            const string command = "queue.declare";
            Action<object, string, bool, bool, bool, bool, bool, object> instrumentedMethod;
            var modelType = model.GetType();

            try
            {
                instrumentedMethod =
                    MethodBuilder<Action<object, string, bool, bool, bool, bool, bool, object>>
                       .Start(moduleVersionPtr, mdToken, opCode, methodName)
                       .WithConcreteType(modelType)
                       .WithParameters(queue, passive, durable, exclusive, autoDelete, nowait, arguments)
                       .WithNamespaceAndNameFilters(ClrNames.Void, ClrNames.String, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Bool, ClrNames.Ignore)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: RabbitMQImplModelBase,
                    methodName: methodName,
                    instanceType: modelType.AssemblyQualifiedName);
                throw;
            }

            using (var scope = CreateScope(Tracer.Instance, command, queue: queue))
            {
                try
                {
                    instrumentedMethod(model, queue, passive, durable, exclusive, autoDelete, nowait, arguments);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        internal static Scope CreateScope(Tracer tracer, string command, ISpanContext parentContext = null, DateTimeOffset? startTime = null, string queue = null, string exchange = null, string routingKey = null)
        {
            if (!tracer.Settings.IsIntegrationEnabled(IntegrationName))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            Scope scope = null;

            try
            {
                var normalizedExchangeName = string.IsNullOrEmpty(exchange) ? "<default>" : exchange;
                var normalizedRoutingKey = string.IsNullOrEmpty(routingKey) ? "<all>" : routingKey.StartsWith("amq.gen-") ? "<generated>" : routingKey;
                string operationName = null;
                string spanKind = null;
                if (ReferenceEquals(command, AmqpBasicDeliverCommand) || ReferenceEquals(command, AmqpBasicGetCommand))
                {
                    operationName = normalizedExchangeName + " receive";
                    spanKind = SpanKinds.Consumer;
                }
                else if (ReferenceEquals(command, AmqpBasicPublishCommand))
                {
                    operationName = normalizedExchangeName + " send";
                    spanKind = SpanKinds.Producer;
                }
                else
                {
                    operationName = OperationName + ": " + command;
                    normalizedRoutingKey = routingKey;
                    spanKind = SpanKinds.Client;
                }

                scope = tracer.StartActive(operationName, parent: parentContext, startTime: startTime);
                var span = scope.Span;
                var tags = span.Tags;

                tags.Add(Tags.InstrumentationName, IntegrationName);
                tags.Add(Tags.SpanKind, spanKind);
                tags.Add(Tags.Messaging.System, SystemName);
                tags.Add(Tags.Messaging.DestinationKind, "queue");
                tags.Add(Tags.Messaging.Destination, normalizedExchangeName);
                tags.Add(OperationName, command);

                if (normalizedRoutingKey != null)
                {
                    tags.Add(Tags.RabbitMQ.RoutingKey, normalizedRoutingKey);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            // always returns the scope, even if it's null because we couldn't create it,
            // or we couldn't populate it completely (some tags is better than no tags)
            return scope;
        }

        /********************
         * Duck Typing Types
         */
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1201 // Elements must appear in the correct order
#pragma warning disable SA1600 // Elements must be documented
        [DuckCopy]
        public struct BasicGetResultStruct
        {
            /// <summary>
            /// Gets the message body of the result
            /// </summary>
            public BodyStruct Body;

            /// <summary>
            /// Gets the message properties
            /// </summary>
            public IBasicProperties BasicProperties;
        }

        public interface IBasicProperties
        {
            /// <summary>
            /// Gets or sets the headers of the message
            /// </summary>
            /// <returns>Message headers</returns>
            IDictionary<string, object> Headers { get; set; }

            /// <summary>
            /// Gets the delivery mode of the message
            /// </summary>
            byte DeliveryMode { get; }

            /// <summary>
            /// Returns true if the DeliveryMode property is present
            /// </summary>
            /// <returns>true if the DeliveryMode property is present</returns>
            bool IsDeliveryModePresent();
        }

        [DuckCopy]
        public struct BodyStruct
        {
            /// <summary>
            /// Gets the length of the message body
            /// </summary>
            public int Length;
        }
    }
}
