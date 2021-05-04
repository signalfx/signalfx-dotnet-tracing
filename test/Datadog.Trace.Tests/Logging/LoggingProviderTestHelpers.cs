// Modified by SignalFx
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Moq;
using Serilog.Formatting.Display;
using SignalFx.Tracing;
using SignalFx.Tracing.Agent;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Sampling;
using Xunit;

namespace Datadog.Trace.Tests.Logging
{
    internal static class LoggingProviderTestHelpers
    {
        public const string ServiceName = "LogCorrelationTest";
        public const string ServiceEnvironment = "TestEnv";
        internal static readonly string CustomPropertyName = "custom";
        internal static readonly int CustomPropertyValue = 1;
        internal static readonly string LogPrefix = "[Datadog.Trace.Tests.Logging]";

        private const string Log4NetExpectedStringFormat = "\"{0}\":\"{1:x16}\"";
        private const string SerilogExpectedStringFormat = "{0}: \"{1:x16}\"";

        internal static Tracer InitializeTracer(bool enableLogsInjection)
        {
            var settings = new TracerSettings { ServiceName = ServiceName, Environment = ServiceEnvironment };
            var writerMock = new Mock<IAgentWriter>();
            var samplerMock = new Mock<ISampler>();

            settings.LogsInjectionEnabled = enableLogsInjection;

            return new Tracer(settings, writerMock.Object, samplerMock.Object, scopeManager: null, statsd: null);
        }

        internal static void PerformParentChildScopeSequence(Tracer tracer, ILog logger, Func<string, object, bool, IDisposable> openMappedContext, out Scope parentScope, out Scope childScope)
        {
            logger.Log(LogLevel.Info, () => $"{LogPrefix}Logged before starting/activating a scope");

            parentScope = tracer.StartActive("parent");
            logger.Log(LogLevel.Info, () => $"{LogPrefix}Started and activated parent scope.");

            var customPropertyContext = openMappedContext(CustomPropertyName, CustomPropertyValue, false);
            logger.Log(LogLevel.Info, () => $"{LogPrefix}Added custom property to MDC");

            childScope = tracer.StartActive("child");
            logger.Log(LogLevel.Info, () => $"{LogPrefix}Started and activated child scope.");

            childScope.Close();
            logger.Log(LogLevel.Info, () => $"{LogPrefix}Closed child scope and reactivated parent scope.");

            customPropertyContext.Dispose();
            logger.Log(LogLevel.Info, () => $"{LogPrefix}Removed custom property from MDC");

            parentScope.Close();
            logger.Log(LogLevel.Info, () => $"{LogPrefix}Closed child scope so there is no active scope.");
        }

        internal static void Contains(this log4net.Core.LoggingEvent logEvent, Scope scope)
        {
            logEvent.Contains(scope.Span.TraceId, scope.Span.SpanId, ServiceName, ServiceEnvironment);
        }

        internal static void Contains(this log4net.Core.LoggingEvent logEvent, TraceId traceId, ulong spanId, string service, string environment)
        {
            // First, verify that the properties are attached to the LogEvent
            Assert.Contains(CorrelationIdentifier.TraceIdKey, logEvent.Properties.GetKeys());
            Assert.Equal(traceId, TraceId.CreateFromString(logEvent.Properties[CorrelationIdentifier.TraceIdKey].ToString()));
            Assert.Contains(CorrelationIdentifier.SpanIdKey, logEvent.Properties.GetKeys());
            Assert.Equal<ulong>(spanId, Convert.ToUInt64(logEvent.Properties[CorrelationIdentifier.SpanIdKey].ToString(), 16));
            Assert.Contains(CorrelationIdentifier.ServiceNameKey, logEvent.Properties.GetKeys());
            Assert.Equal(service, logEvent.Properties[CorrelationIdentifier.ServiceNameKey].ToString());
            Assert.Contains(CorrelationIdentifier.ServiceEnvironmentKey, logEvent.Properties.GetKeys());
            Assert.Equal(environment, logEvent.Properties[CorrelationIdentifier.ServiceEnvironmentKey].ToString());

            // Second, verify that the message formatting correctly encloses the
            // values in quotes, since they are string values
            var layout = new Log4NetLogProviderTests.Log4NetJsonLayout();
            string formattedMessage = layout.Format(logEvent);
            Assert.Contains(string.Format(Log4NetExpectedStringFormat, CorrelationIdentifier.TraceIdKey, traceId), formattedMessage);
            Assert.Contains(string.Format(Log4NetExpectedStringFormat, CorrelationIdentifier.SpanIdKey, spanId), formattedMessage);
        }

        internal static void Contains(this Serilog.Events.LogEvent logEvent, Scope scope)
        {
            string SanitizedProperty(string correlationIdentifier)
            {
                return logEvent.Properties[correlationIdentifier].ToString().Trim(new[] { '\"' });
            }

            var traceId = scope.Span.TraceId;
            var spanId = scope.Span.SpanId;
            // First, verify that the properties are attached to the LogEvent
            Assert.True(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
            Assert.Equal(traceId, TraceId.CreateFromString(SanitizedProperty(CorrelationIdentifier.TraceIdKey)));
            Assert.True(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.Equal<ulong>(spanId, Convert.ToUInt64(SanitizedProperty(CorrelationIdentifier.SpanIdKey), fromBase: 16));
            Assert.True(logEvent.Properties.ContainsKey(CorrelationIdentifier.ServiceNameKey));
            Assert.Equal(ServiceName, SanitizedProperty(CorrelationIdentifier.ServiceNameKey));
            Assert.True(logEvent.Properties.ContainsKey(CorrelationIdentifier.ServiceEnvironmentKey));
            Assert.Equal(ServiceEnvironment, SanitizedProperty(CorrelationIdentifier.ServiceEnvironmentKey));

            // Second, verify that the message formatting correctly encloses the
            // values in quotes, since they are string values

            // Use the built-in formatting to render the message like the console output would,
            // but this must write to a TextWriter so use a StringWriter/StringBuilder to shuttle
            // the message to our in-memory list
            const string outputTemplate = "{Message}|{Properties}";
            var textFormatter = new MessageTemplateTextFormatter(outputTemplate, CultureInfo.InvariantCulture);
            var sw = new StringWriter(new StringBuilder());
            textFormatter.Format(logEvent, sw);
            var formattedMessage = sw.ToString();

            Assert.Contains(string.Format(SerilogExpectedStringFormat, CorrelationIdentifier.TraceIdKey, traceId), formattedMessage);
            Assert.Contains(string.Format(SerilogExpectedStringFormat, CorrelationIdentifier.SpanIdKey, spanId), formattedMessage);
        }
    }
}
