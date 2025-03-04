// Modified by Splunk Inc.

using System.IO;
using BenchmarkDotNet.Attributes;
using Datadog.Trace;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.NLog.LogsInjection;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Benchmarks.Trace
{
    [MemoryDiagnoser]
    [BenchmarkAgent3]
    public class NLogBenchmark
    {
        private static readonly Tracer LogInjectionTracer;
        private static readonly NLog.Logger Logger;

        static NLogBenchmark()
        {
            var logInjectionSettings = new TracerSettings
            {
                StartupDiagnosticLogEnabled = false,
                LogsInjectionEnabled = true,
                Environment = "deployment.environment",
                ServiceVersion = "version"
            };

            LogInjectionTracer = new Tracer(logInjectionSettings, new DummyAgentWriter(), null, null, null);
            Tracer.UnsafeSetTracerInstance(LogInjectionTracer);

            var config = new LoggingConfiguration();

#if DEBUG
            var writer = System.Console.Out;
#else
            var writer = TextWriter.Null;
#endif

            var target = new TextWriterTarget(writer)
            {
                Layout = "${longdate}|${uppercase:${level}}|${logger}|{deployment.environment=${mdlc:item=deployment.environment},service.name=${mdlc:item=service.name},service.version=${mdlc:item=service.version},trace_id=${mdlc:item=trace_id},span_id=${mdlc:item=span_id}}|${message}"
            };

            config.AddRuleForAllLevels(target);

            LogManager.Configuration = config;
            Logger = LogManager.GetCurrentClassLogger();

            // Run the automatic instrumentation initialization code once outside of the microbenchmark
            _ = DiagnosticContextHelper.Cache<NLog.Logger>.Mdlc;
        }

        [Benchmark]
        public void EnrichedLog()
        {
            using (LogInjectionTracer.StartActive("Test"))
            {
                using (LogInjectionTracer.StartActive("Child"))
                {
                    // None of the arguments are used directly
                    // First arg is a marker type, so needs to be an NLog type
                    // Remainder can be any object
                    var callTargetState = LoggerImplWriteIntegration.OnMethodBegin(Logger, typeof(NLog.Logger), Logger, Logger, Logger);

                    Logger.Info("Hello");

                    LoggerImplWriteIntegration.OnMethodEnd(Logger, exception: null, callTargetState);
                }
            }
        }

        private class TextWriterTarget : TargetWithLayout
        {
            private readonly TextWriter _writer;

            public TextWriterTarget(TextWriter textWriter)
            {
                _writer = textWriter;
            }

            protected override void Write(LogEventInfo logEvent)
            {
                _writer.WriteLine(RenderLogEvent(Layout, logEvent));
            }
        }
    }
}
