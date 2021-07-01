using System;
using System.Globalization;
using System.Reflection;
using SignalFx.Tracing;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Vendors.Serilog;

namespace Datadog.Trace.ClrProfiler.Integrations.Kafka
{
    internal static class KafkaHelper
    {
        private static readonly ILogger Log = SignalFxLogging.GetLogger(typeof(ConsumeKafkaIntegration));

        internal static Scope CreateConsumeScope(object consumeResult)
        {
            var tracer = Tracer.Instance;
            if (!tracer.Settings.IsIntegrationEnabled(Constants.IntegrationName))
            {
                // integration disabled, don't create a scope/span, skip this trace
                return null;
            }

            var parent = tracer.ActiveScope?.Span;
            if (parent is not null &&
                parent.OperationName == Constants.ConsumeSyncOperationName &&
                parent.GetTag(Tags.InstrumentationName) != null)
            {
                return null;
            }

            SpanContext propagatedContext = null;

            // Try to extract propagated context from headers.
            var message = GetPropertyValue<object>(consumeResult, "Message");
            if (message is not null)
            {
                var headers = GetPropertyValue<object>(message, "Headers");
                if (headers is not null)
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
            }

            Scope scope = null;
            try
            {
                var topicName = GetPropertyValue<string>(consumeResult, "Topic");
                var partition = GetPropertyValue<object>(consumeResult, "Partition");

                scope = tracer.StartActive(Constants.ConsumeSyncOperationName, propagatedContext, tracer.DefaultServiceName);

                var span = scope.Span;
                if (partition is not null)
                {
                    span.Tags.Add(Tags.KafkaPartition, partition.ToString());
                }

                if (message is not null)
                {
                    var timestamp = GetPropertyValue<object>(consumeResult, "Timestamp");
                    if (timestamp is not null)
                    {
                        var dateTime = GetPropertyValue<DateTime>(timestamp, "UtcDateTime");
                        if (dateTime != default)
                        {
                            var consumeTime = span.StartTime.UtcDateTime;
                            var messageQueueTimeMs = Math.Max(0, (consumeTime - dateTime).TotalMilliseconds);
                            span.Tags.Add(Tags.KafkaMessageQueueTimeMs, messageQueueTimeMs.ToString(CultureInfo.InvariantCulture));
                        }
                    }

                    var value = GetPropertyValue<object>(message, "Value");
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

        internal static Scope CreateProduceScope(string topicName, object message, string operationName)
        {
            if (!Tracer.Instance.Settings.IsIntegrationEnabled(Constants.IntegrationName))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            var tracer = Tracer.Instance;

            var parentSpan = tracer.ActiveScope?.Span;
            if (parentSpan is not null &&
                parentSpan.OperationName == operationName &&
                parentSpan.GetTag(Tags.KafkaTopic) == topicName)
            {
                // we are already instrumenting this
                return null;
            }

            Scope scope = null;
            try
            {
                scope = tracer.StartActive(operationName, serviceName: tracer.DefaultServiceName);
                var span = scope.Span;
                span.Type = SpanTypes.Kafka;
                span.SetTag(Tags.InstrumentationName, Constants.IntegrationName);
                span.SetTag(Tags.SpanKind, SpanKinds.Client);
                span.SetTag(Tags.KafkaTopic, topicName);

                if (message != null)
                {
                    var value = GetPropertyValue<object>(message, "Value");
                    span.Tags.Add(Tags.KafkaTombstone, value is null ? "true" : "false");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }

        public static Scope CreateProduceScope(object topicPartition, object message, string operationName)
        {
            var topicName = GetPropertyValue<string>(topicPartition, "Topic");
            return CreateProduceScope(topicName, message, operationName);
        }

        internal static T GetPropertyValue<T>(object obj, string propertyName)
        {
            if (!obj.TryGetPropertyValue(propertyName, out T property))
            {
                property = default;
                Log.Warning($"Unable to access {propertyName} property.");
            }

            return property;
        }

        internal static object CreateHeaders(object message)
        {
            try
            {
                var headers = Activator.CreateInstance(Assembly.Load(Constants.ConfluentKafkaAssemblyName).GetType(Constants.HeadersType));
                var headersProperty = message.GetType().GetProperty("Headers");
                var setter = headersProperty.GetSetMethod(nonPublic: false);
                setter.Invoke(message, new[] { headers });

                return headers;
            }
            catch (Exception)
            {
                Log.Warning("Failed to create headers");
                return null;
            }
        }
    }
}
