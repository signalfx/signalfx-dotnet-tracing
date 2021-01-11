// Modified by SignalFx
using System;
using System.Collections.Generic;
using Serilog;
using Serilog.Events;
using SignalFx.Tracing;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Logging.LogProviders;
using Xunit;

namespace Datadog.Trace.Tests.Logging
{
    [Collection(nameof(Datadog.Trace.Tests.Logging))]
    public class SerilogLogProviderTests
    {
        private readonly ILogProvider _logProvider;
        private readonly ILog _logger;
        private readonly List<LogEvent> _logEvents;

        public SerilogLogProviderTests()
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Observers(obs => obs.Subscribe(logEvent => _logEvents.Add(logEvent)))
                .CreateLogger();
            _logEvents = new List<LogEvent>();

            _logProvider = new SerilogLogProvider();
            LogProvider.SetCurrentLogProvider(_logProvider);
            _logger = new LoggerExecutionWrapper(_logProvider.GetLogger("Test"));
        }

        [Fact]
        public void EnabledLibLogSubscriberAddsTraceData()
        {
            // Assert that the Serilog log provider is correctly being used
            Assert.IsType<SerilogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.PerformParentChildScopeSequence(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            _logEvents.RemoveAll(log => !log.MessageTemplate.ToString().Contains(LoggingProviderTestHelpers.LogPrefix));

            var logIndex = 0;
            LogEvent logEvent;

            // The first log should not have signalfx.span_id or signalfx.trace_id
            // Scope: N/A
            // Custom property: N/A
            logEvent = _logEvents[logIndex++];
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
            Assert.False(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));

            // Scope: Parent scope
            // Custom property: N/A
            logEvent = _logEvents[logIndex++];
            logEvent.Contains(parentScope);
            Assert.False(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));

            // Scope: Parent scope
            // Custom property: SET
            logEvent = _logEvents[logIndex++];
            logEvent.Contains(parentScope);
            Assert.True(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));
            Assert.Equal<int>(LoggingProviderTestHelpers.CustomPropertyValue, int.Parse(logEvent.Properties[LoggingProviderTestHelpers.CustomPropertyName].ToString()));

            // Scope: Child scope
            // Custom property: SET
            logEvent = _logEvents[logIndex++];
            logEvent.Contains(childScope);
            Assert.True(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));
            Assert.Equal<int>(LoggingProviderTestHelpers.CustomPropertyValue, int.Parse(logEvent.Properties[LoggingProviderTestHelpers.CustomPropertyName].ToString()));

            // Scope: Parent scope
            // Custom property: SET
            logEvent = _logEvents[logIndex++];
            logEvent.Contains(parentScope);
            Assert.True(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));
            Assert.Equal<int>(LoggingProviderTestHelpers.CustomPropertyValue, int.Parse(logEvent.Properties[LoggingProviderTestHelpers.CustomPropertyName].ToString()));

            // Scope: Parent scope
            // Custom property: N/A
            logEvent = _logEvents[logIndex++];
            logEvent.Contains(parentScope);
            Assert.False(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));

            // Scope: N/A
            // Custom property: N/A
            logEvent = _logEvents[logIndex++];
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
            Assert.False(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));
        }

        [Fact]
        public void DisabledLibLogSubscriberDoesNotAddTraceData()
        {
            // Assert that the Serilog log provider is correctly being used
            Assert.IsType<SerilogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: false);
            LoggingProviderTestHelpers.PerformParentChildScopeSequence(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            _logEvents.RemoveAll(log => !log.MessageTemplate.ToString().Contains(LoggingProviderTestHelpers.LogPrefix));

            int logIndex = 0;
            LogEvent logEvent;

            // Scope: N/A
            // Custom property: N/A
            logEvent = _logEvents[logIndex++];
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
            Assert.False(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));

            // Scope: N/A
            // Custom property: N/A
            logEvent = _logEvents[logIndex++];
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
            Assert.False(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));

            // Scope: N/A
            // Custom property: SET
            logEvent = _logEvents[logIndex++];
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
            Assert.True(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));
            Assert.Equal<int>(LoggingProviderTestHelpers.CustomPropertyValue, int.Parse(logEvent.Properties[LoggingProviderTestHelpers.CustomPropertyName].ToString()));

            // Scope: N/A
            // Custom property: SET
            logEvent = _logEvents[logIndex++];
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
            Assert.True(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));
            Assert.Equal<int>(LoggingProviderTestHelpers.CustomPropertyValue, int.Parse(logEvent.Properties[LoggingProviderTestHelpers.CustomPropertyName].ToString()));

            // Scope: N/A
            // Custom property: SET
            logEvent = _logEvents[logIndex++];
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
            Assert.True(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));
            Assert.Equal<int>(LoggingProviderTestHelpers.CustomPropertyValue, int.Parse(logEvent.Properties[LoggingProviderTestHelpers.CustomPropertyName].ToString()));

            // Scope: N/A
            // Custom property: N/A
            logEvent = _logEvents[logIndex++];
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
            Assert.False(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));

            // Scope: N/A
            // Custom property: N/A
            logEvent = _logEvents[logIndex++];
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
            Assert.False(logEvent.Properties.ContainsKey(LoggingProviderTestHelpers.CustomPropertyName));
        }
    }
}
