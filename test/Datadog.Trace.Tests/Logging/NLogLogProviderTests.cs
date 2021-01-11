// Modified by SignalFx
using System.Collections.Generic;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using SignalFx.Tracing;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Logging.LogProviders;
using Xunit;

namespace Datadog.Trace.Tests.Logging
{
    [Collection(nameof(Datadog.Trace.Tests.Logging))]
    [TestCaseOrderer("Datadog.Trace.TestHelpers.AlphabeticalOrderer", "Datadog.Trace.TestHelpers")]
    public class NLogLogProviderTests
    {
        private const string ExpectedIdStringFormat = "\"{0}\": \"{1:x16}\"";
        private const string ExpectedStringFormat = "\"{0}\": \"{1}\"";

        private readonly ILogProvider _logProvider;
        private readonly ILog _logger;
        private readonly MemoryTarget _target;

        public NLogLogProviderTests()
        {
            var config = new LoggingConfiguration();
            var layout = new JsonLayout();
            layout.IncludeMdc = true;
            layout.Attributes.Add(new JsonAttribute("time", Layout.FromString("${longdate}")));
            layout.Attributes.Add(new JsonAttribute("level", Layout.FromString("${level:uppercase=true}")));
            layout.Attributes.Add(new JsonAttribute("message", Layout.FromString("${message}")));
            _target = new MemoryTarget
            {
                Layout = layout
            };

            config.AddTarget("memory", _target);
            config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Trace, _target));
            LogManager.Configuration = config;
            SimpleConfigurator.ConfigureForTargetLogging(_target, NLog.LogLevel.Trace);

            _logProvider = new NLogLogProvider();
            LogProvider.SetCurrentLogProvider(_logProvider);
            _logger = new LoggerExecutionWrapper(_logProvider.GetLogger("test"));
        }

        [Fact]
        public void EnabledLibLogSubscriberAddsTraceData()
        {
            // Assert that the NLog log provider is correctly being used
            Assert.IsType<NLogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.PerformParentChildScopeSequence(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            List<string> filteredLogs = new List<string>(_target.Logs);
            filteredLogs.RemoveAll(log => !log.Contains(LoggingProviderTestHelpers.LogPrefix));

            int logIndex = 0;
            string logString;

            // The first log should not have signalfx.span_id or signalfx.trace_id
            // Scope: N/A
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            Assert.DoesNotContain($"\"{CorrelationIdentifier.SpanIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.TraceIdKey}\"", logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);

            // Scope: Parent scope
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.SpanIdKey, parentScope.Span.SpanId), logString);
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.TraceIdKey, parentScope.Span.TraceId), logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);

            // Scope: Parent scope
            // Custom property: SET
            logString = filteredLogs[logIndex++];
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.SpanIdKey, parentScope.Span.SpanId), logString);
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.TraceIdKey, parentScope.Span.TraceId), logString);
            Assert.Contains(string.Format(ExpectedStringFormat, LoggingProviderTestHelpers.CustomPropertyName, LoggingProviderTestHelpers.CustomPropertyValue), logString);

            // Scope: Child scope
            // Custom property: SET
            logString = filteredLogs[logIndex++];
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.SpanIdKey, childScope.Span.SpanId), logString);
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.TraceIdKey, childScope.Span.TraceId), logString);
            Assert.Contains(string.Format(ExpectedStringFormat, LoggingProviderTestHelpers.CustomPropertyName, LoggingProviderTestHelpers.CustomPropertyValue), logString);

            // Scope: Parent scope
            // Custom property: SET
            logString = filteredLogs[logIndex++];
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.SpanIdKey, parentScope.Span.SpanId), logString);
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.TraceIdKey, childScope.Span.TraceId), logString);
            Assert.Contains(string.Format(ExpectedStringFormat, LoggingProviderTestHelpers.CustomPropertyName, LoggingProviderTestHelpers.CustomPropertyValue), logString);

            // Scope: Parent scope
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.SpanIdKey, parentScope.Span.SpanId), logString);
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.TraceIdKey, childScope.Span.TraceId), logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);

            // Scope: Default values of TraceId=0,SpanId=0
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.SpanIdKey, 0), logString);
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.TraceIdKey, 0), logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);
        }

        [Fact]
        public void DisabledLibLogSubscriberDoesNotAddTraceData()
        {
            // Assert that the NLog log provider is correctly being used
            Assert.IsType<NLogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: false);
            LoggingProviderTestHelpers.PerformParentChildScopeSequence(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            List<string> filteredLogs = new List<string>(_target.Logs);
            filteredLogs.RemoveAll(log => !log.Contains(LoggingProviderTestHelpers.LogPrefix));

            int logIndex = 0;
            string logString;

            // Scope: N/A
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            Assert.DoesNotContain($"\"{CorrelationIdentifier.SpanIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.TraceIdKey}\"", logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);

            // Scope: N/A
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            Assert.DoesNotContain($"\"{CorrelationIdentifier.SpanIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.TraceIdKey}\"", logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);

            // Scope: N/A
            // Custom property: SET
            logString = filteredLogs[logIndex++];
            Assert.DoesNotContain($"\"{CorrelationIdentifier.SpanIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.TraceIdKey}\"", logString);
            Assert.Contains(string.Format(ExpectedStringFormat, LoggingProviderTestHelpers.CustomPropertyName, LoggingProviderTestHelpers.CustomPropertyValue), logString);

            // Scope: N/A
            // Custom property: SET
            logString = filteredLogs[logIndex++];
            Assert.DoesNotContain($"\"{CorrelationIdentifier.SpanIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.TraceIdKey}\"", logString);
            Assert.Contains(string.Format(ExpectedStringFormat, LoggingProviderTestHelpers.CustomPropertyName, LoggingProviderTestHelpers.CustomPropertyValue), logString);

            // Scope: N/A
            // Custom property: SET
            logString = filteredLogs[logIndex++];
            Assert.DoesNotContain($"\"{CorrelationIdentifier.SpanIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.TraceIdKey}\"", logString);
            Assert.Contains(string.Format(ExpectedStringFormat, LoggingProviderTestHelpers.CustomPropertyName, LoggingProviderTestHelpers.CustomPropertyValue), logString);

            // Scope: N/A
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            Assert.DoesNotContain($"\"{CorrelationIdentifier.SpanIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.TraceIdKey}\"", logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);

            // Scope: N/A
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            Assert.DoesNotContain($"\"{CorrelationIdentifier.SpanIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.TraceIdKey}\"", logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);
        }
    }
}
