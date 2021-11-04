// <copyright file="RabbitMQIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.RabbitMQ;
using Datadog.Trace.ClrProfiler.Emit;
using Datadog.Trace.Configuration;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// Tracing integration for RabbitMQ.Client
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class RabbitMQIntegration
    {
        internal const string IntegrationName = nameof(IntegrationIds.RabbitMQ);

        private const string SystemName = "rabbitmq";

        private const string Major3Minor6Patch9 = "3.6.9";
        private const string Major5 = "5";
        private const string Major6 = "6";
        private const string RabbitMQAssembly = "RabbitMQ.Client";
        private const string RabbitMQIModel = "RabbitMQ.Client.IModel";
        private const string RabbitMQImplModelBase = "RabbitMQ.Client.Impl.ModelBase";
        private const string RabbitMQDefaultBasicConsumer = "RabbitMQ.Client.DefaultBasicConsumer";
        private const string IBasicPropertiesTypeName = "RabbitMQ.Client.IBasicProperties";
        private const string IDictionaryArgumentsTypeName = "System.Collections.Generic.IDictionary`2[System.String,System.Object]";

        internal const string OperationSetup = "setup";
        internal const string OperationProcess = "process";
        internal const string OperationReceive = "receive";
        internal const string OperationSend = "send";

        internal const string DeliverCommand = "basic.deliver";
        internal const string GetCommand = "basic.get";
        internal const string PublishCommand = "basic.publish";

        internal static readonly IntegrationInfo IntegrationId = IntegrationRegistry.GetIntegrationInfo(IntegrationName);
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(RabbitMQIntegration));
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
            const string command = DeliverCommand;
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
                Tracer tracer = Tracer.Instance;

                // try to extract propagated context values from headers
                if (basicPropertiesValue.Headers != null)
                {
                    try
                    {
                        propagatedContext = tracer.Propagator.Extract(basicPropertiesValue.Headers, headersGetter);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error extracting propagated headers.");
                    }
                }

                using (var scope = CreateScope(tracer, out RabbitMQTags tags, command, parentContext: propagatedContext, spanKind: SpanKinds.Consumer, exchange: exchange, routingKey: routingKey))
                {
                    if (tags != null)
                    {
                        tags.MessageSize = body?.Length.ToString() ?? "0";

                        SetTagsFromBasicProperties(tags, basicPropertiesValue);
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
            const string command = DeliverCommand;
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

            Tracer tracer = Tracer.Instance;
            SpanContext propagatedContext = null;

            if (basicProperties.TryDuckCast<IBasicProperties>(out var basicPropertiesValue))
            {
                // try to extract propagated context values from headers
                if (basicPropertiesValue.Headers != null)
                {
                    try
                    {
                        propagatedContext = tracer.Propagator.Extract(basicPropertiesValue.Headers, headersGetter);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error extracting propagated headers.");
                    }
                }

                using (var scope = CreateScope(tracer, out RabbitMQTags tags, command, parentContext: propagatedContext, spanKind: SpanKinds.Consumer, exchange: exchange, routingKey: routingKey))
                {
                    if (tags != null)
                    {
                        SetTagsFromBasicProperties(tags, basicPropertiesValue);

                        if (body != null && body.TryDuckCast<BodyStruct>(out var bodyStruct))
                        {
                            tags.MessageSize = bodyStruct.Length.ToString() ?? "0";
                        }
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
            const string command = GetCommand;
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
            RabbitMQTags tags = null;
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
                Tracer tracer = Tracer.Instance;
                SpanContext propagatedContext = null;
                string messageSize = null;

                if (result != null && result.TryDuckCast<BasicGetResultStruct>(out var basicGetResult))
                {
                    messageSize = basicGetResult.Body.Length.ToString();
                    var basicPropertiesHeaders = basicGetResult.BasicProperties?.Headers;

                    // try to extract propagated context values from headers
                    if (basicPropertiesHeaders != null)
                    {
                        try
                        {
                            propagatedContext = tracer.Propagator.Extract(basicPropertiesHeaders, headersGetter);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error extracting propagated headers.");
                        }
                    }
                }

                using (var scope = CreateScope(tracer, out tags, command, parentContext: propagatedContext, spanKind: SpanKinds.Consumer, queue: queue, startTime: startTime))
                {
                    if (scope != null)
                    {
                        string queueDisplayName = string.IsNullOrEmpty(queue) || !queue.StartsWith("amq.gen-") ? queue : "<generated>";
                        scope.Span.ResourceName = $"{command} {queueDisplayName}";

                        if (tags != null && messageSize != null)
                        {
                            tags.MessageSize = messageSize;
                        }

                        if (exception != null)
                        {
                            scope.Span.SetException(exception);
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
            const string command = PublishCommand;
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

            Tracer tracer = Tracer.Instance;

            using (var scope = CreateScope(tracer, out RabbitMQTags tags, command, spanKind: SpanKinds.Producer, exchange: exchange, routingKey: routingKey))
            {
                if (scope != null)
                {
                    string exchangeDisplayName = string.IsNullOrEmpty(exchange) ? "<default>" : exchange;
                    string routingKeyDisplayName = string.IsNullOrEmpty(routingKey) ? "<all>" : routingKey.StartsWith("amq.gen-") ? "<generated>" : routingKey;
                    scope.Span.ResourceName = $"{command} {exchangeDisplayName} -> {routingKeyDisplayName}";

                    if (tags != null)
                    {
                        tags.MessageSize = body?.Length.ToString() ?? "0";
                    }

                    if (basicProperties != null && basicProperties.TryDuckCast<IBasicProperties>(out var basicPropertiesValue))
                    {
                        if (tags != null)
                        {
                            SetTagsFromBasicProperties(tags, basicPropertiesValue);
                        }

                        // add distributed tracing headers to the message
                        if (basicPropertiesValue.Headers == null)
                        {
                            basicPropertiesValue.Headers = new Dictionary<string, object>();
                        }

                        tracer.Propagator.Inject(scope.Span.Context, basicPropertiesValue.Headers, headersSetter);
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
            const string command = PublishCommand;
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

            Tracer tracer = Tracer.Instance;

            using (var scope = CreateScope(tracer, out RabbitMQTags tags, command, spanKind: SpanKinds.Producer, exchange: exchange, routingKey: routingKey))
            {
                if (scope != null)
                {
                    string exchangeDisplayName = string.IsNullOrEmpty(exchange) ? "<default>" : exchange;
                    string routingKeyDisplayName = string.IsNullOrEmpty(routingKey) ? "<all>" : routingKey.StartsWith("amq.gen-") ? "<generated>" : routingKey;
                    scope.Span.ResourceName = $"{command} {exchangeDisplayName} -> {routingKeyDisplayName}";

                    if (tags != null)
                    {
                        if (body != null && body.TryDuckCast<BodyStruct>(out var bodyStruct))
                        {
                            tags.MessageSize = bodyStruct.Length.ToString();
                        }
                        else
                        {
                            tags.MessageSize = "0";
                        }
                    }

                    if (basicProperties != null && basicProperties.TryDuckCast<IBasicProperties>(out var basicPropertiesValue))
                    {
                        if (tags != null)
                        {
                            SetTagsFromBasicProperties(tags, basicPropertiesValue);
                        }

                        // add distributed tracing headers to the message
                        if (basicPropertiesValue.Headers == null)
                        {
                            basicPropertiesValue.Headers = new Dictionary<string, object>();
                        }

                        tracer.Propagator.Inject(scope.Span.Context, basicPropertiesValue.Headers, headersSetter);
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

            using (var scope = CreateScope(Tracer.Instance, out _, command, SpanKinds.Client, exchange: exchange))
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

            using (var scope = CreateScope(Tracer.Instance, out _, command, SpanKinds.Client, queue: queue, exchange: exchange, routingKey: routingKey))
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

            using (var scope = CreateScope(Tracer.Instance, out _, command, SpanKinds.Client, queue: queue))
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

        internal static Scope CreateScope(Tracer tracer, out RabbitMQTags tags, string command, string spanKind, ISpanContext parentContext = null, DateTimeOffset? startTime = null, string queue = null, string exchange = null, string routingKey = null)
        {
            tags = null;

            if (!tracer.Settings.IsIntegrationEnabled(IntegrationId))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            Scope scope = null;

            try
            {
                Span parent = tracer.ActiveScope?.Span;

                tags = new RabbitMQTags(spanKind);
                string operation = CommandToOperation(command);
                string operationName = string.IsNullOrWhiteSpace(exchange)
                    ? operation
                    : $"{exchange} {operation}";

                scope = tracer.StartActiveWithTags(operationName, parent: parentContext, tags: tags, startTime: startTime);
                var span = scope.Span;

                span.Type = SpanTypes.Queue;
                span.LogicScope = "amqp.command";
                span.ResourceName = command;

                // Network tags
                // TODO: tags.PeerName = "";
                // TODO: tags.PeerIP = "";

                // Messaging Tags
                tags.System = SystemName;
                tags.Destination = exchange;
                tags.DestinationKind = SpanTypes.Queue;
                // TODO: tags.TempDestination = "TODO";
                // TODO: tags.Protocol = "TODO";
                // TODO: tags.ProtocolVersion = "TODO";
                // TODO: tags.Url = "TODO";

                if (ReferenceEquals(operation, OperationReceive) ||
                    ReferenceEquals(operation, OperationProcess))
                {
                    tags.Operation = operation;

                    // tags.ConsumerId = "TODO";
                }

                // RabbitMq Tags
                tags.InstrumentationName = IntegrationName;
                tags.Queue = queue;
                tags.Exchange = exchange;
                tags.Command = command;
                tags.RoutingKey = routingKey;

                tags.SetAnalyticsSampleRate(IntegrationId, tracer.Settings, enabledWithGlobalSetting: false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            // always returns the scope, even if it's null because we couldn't create it,
            // or we couldn't populate it completely (some tags is better than no tags)
            return scope;
        }

        private static string CommandToOperation(string command)
        {
            if (ReferenceEquals(command, DeliverCommand) ||
                ReferenceEquals(command, GetCommand))
            {
                return OperationReceive;
            }
            else if (ReferenceEquals(command, PublishCommand))
            {
                return OperationSend;
            }

            return OperationSetup;
        }

        internal static void SetTagsFromBasicProperties(RabbitMQTags tags, IBasicProperties basicProperties)
        {
            tags.ConversationId = basicProperties.CorrelationId;
            tags.MessageId = basicProperties.MessageId;

            if (basicProperties.IsDeliveryModePresent())
            {
                tags.DeliveryMode = DeliveryModeStrings[0x3 & basicProperties.DeliveryMode];
            }
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
