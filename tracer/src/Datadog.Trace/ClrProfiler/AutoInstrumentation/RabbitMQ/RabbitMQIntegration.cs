// <copyright file="RabbitMQIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using Datadog.Trace.Configuration;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.RabbitMQ
{
    /// <summary>
    /// Tracing integration for RabbitMQ.Client
    /// </summary>
    internal static class RabbitMQIntegration
    {
        internal const string IntegrationName = nameof(Configuration.IntegrationId.RabbitMQ);

        private const string ServiceName = "rabbitmq";
        private const string SystemName = "rabbitmq";

        internal const string OperationSetup = "setup";
        internal const string OperationProcess = "process";
        internal const string OperationReceive = "receive";
        internal const string OperationSend = "send";

        internal const string DeliverCommand = "basic.deliver";
        internal const string GetCommand = "basic.get";
        internal const string PublishCommand = "basic.publish";

        internal const IntegrationId IntegrationId = Configuration.IntegrationId.RabbitMQ;
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(RabbitMQIntegration));

        private static readonly string[] DeliveryModeStrings = { null, "1", "2" };

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
                tags = new RabbitMQTags(spanKind);
                string operation = CommandToOperation(command);
                string operationName = string.IsNullOrWhiteSpace(exchange)
                    ? $"(default) {operation}"
                    : $"{exchange} {operation}";

                string serviceName = tracer.Settings.GetServiceName(tracer, ServiceName);
                scope = tracer.StartActiveInternal(operationName, parent: parentContext, tags: tags, serviceName: serviceName, startTime: startTime);
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
        internal struct BasicGetResultStruct
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
        internal struct BodyStruct
        {
            /// <summary>
            /// Gets the length of the message body
            /// </summary>
            public int Length;
        }
    }
}
