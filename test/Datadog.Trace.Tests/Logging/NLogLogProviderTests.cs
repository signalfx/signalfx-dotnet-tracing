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
            layout.Attributes.Add(new JsonAttribute(LoggingProviderTestHelpers.CustomPropertyName, Layout.FromString($"${{mdlc:item={LoggingProviderTestHelpers.CustomPropertyName}}}")));

            // MapDiagnosticLogicalContext attributes required by NLog v 4.6+
            layout.Attributes.Add(new JsonAttribute("trace_id", Layout.FromString("${mdlc:item=trace_id}")));
            layout.Attributes.Add(new JsonAttribute("span_id", Layout.FromString("${mdlc:item=span_id}")));
            layout.Attributes.Add(new JsonAttribute("service.name", Layout.FromString("${mdlc:item=service.name}")));
            layout.Attributes.Add(new JsonAttribute("deployment.environment", Layout.FromString("${mdlc:item=deployment.environment}")));

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

            // The first log should not have span_id or trace_id
            // Scope: N/A
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            Assert.DoesNotContain($"\"{CorrelationIdentifier.SpanIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.TraceIdKey}\"", logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);

            // Scope: Parent scope
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            AssertCorrelationIdentifiers(parentScope, logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);

            // Scope: Parent scope
            // Custom property: SET
            logString = filteredLogs[logIndex++];
            AssertCorrelationIdentifiers(parentScope, logString);
            Assert.Contains(string.Format(ExpectedStringFormat, LoggingProviderTestHelpers.CustomPropertyName, LoggingProviderTestHelpers.CustomPropertyValue), logString);

            // Scope: Child scope
            // Custom property: SET
            logString = filteredLogs[logIndex++];
            AssertCorrelationIdentifiers(childScope, logString);
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
            Assert.DoesNotContain($"\"{CorrelationIdentifier.SpanIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.TraceIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.ServiceNameKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.ServiceEnvironmentKey}\"", logString);
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
            AssertNoCorrelationIdentifiers(logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);

            // Scope: N/A
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            AssertNoCorrelationIdentifiers(logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);

            // Scope: N/A
            // Custom property: SET
            logString = filteredLogs[logIndex++];
            AssertNoCorrelationIdentifiers(logString);
            Assert.Contains(string.Format(ExpectedStringFormat, LoggingProviderTestHelpers.CustomPropertyName, LoggingProviderTestHelpers.CustomPropertyValue), logString);

            // Scope: N/A
            // Custom property: SET
            logString = filteredLogs[logIndex++];
            AssertNoCorrelationIdentifiers(logString);
            Assert.Contains(string.Format(ExpectedStringFormat, LoggingProviderTestHelpers.CustomPropertyName, LoggingProviderTestHelpers.CustomPropertyValue), logString);

            // Scope: N/A
            // Custom property: SET
            logString = filteredLogs[logIndex++];
            AssertNoCorrelationIdentifiers(logString);
            Assert.Contains(string.Format(ExpectedStringFormat, LoggingProviderTestHelpers.CustomPropertyName, LoggingProviderTestHelpers.CustomPropertyValue), logString);

            // Scope: N/A
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            AssertNoCorrelationIdentifiers(logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);

            // Scope: N/A
            // Custom property: N/A
            logString = filteredLogs[logIndex++];
            AssertNoCorrelationIdentifiers(logString);
            Assert.DoesNotContain($"\"{LoggingProviderTestHelpers.CustomPropertyName}\"", logString);
        }

        private static void AssertCorrelationIdentifiers(Scope scope, string logString)
        {
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.SpanIdKey, scope.Span.SpanId), logString);
            Assert.Contains(string.Format(ExpectedIdStringFormat, CorrelationIdentifier.TraceIdKey, scope.Span.TraceId), logString);
            Assert.Contains(string.Format(ExpectedStringFormat, CorrelationIdentifier.ServiceNameKey, LoggingProviderTestHelpers.ServiceName), logString);
            Assert.Contains(string.Format(ExpectedStringFormat, CorrelationIdentifier.ServiceEnvironmentKey, LoggingProviderTestHelpers.ServiceEnvironment), logString);
        }

        private static void AssertNoCorrelationIdentifiers(string logString)
        {
            Assert.DoesNotContain($"\"{CorrelationIdentifier.SpanIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.TraceIdKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.ServiceNameKey}\"", logString);
            Assert.DoesNotContain($"\"{CorrelationIdentifier.ServiceEnvironmentKey}\"", logString);
        }
    }
}
