using System;
using System.IO;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using NLog;
using NLog.Config;
using NLog.Targets;
using SignalFx.Tracing;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.Logging;

namespace NLog46
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    [MemoryDiagnoser]
    public class Benchmarks
    {

        private static readonly Tracer BaselineTracer;
        private static readonly Tracer OldLogInjectionTracer;
        private static readonly Tracer NewLogInjectionTracer;
        private static readonly NLog.Logger NLogger = LogManager.GetCurrentClassLogger();

        static Benchmarks()
        {
            var baselineSettings = new TracerSettings
            {
                LogsInjectionEnabled = false,
            };
            BaselineTracer = new Tracer(baselineSettings);

            var oldLogInjectionSettings = new TracerSettings
            {
                LogsInjectionEnabled = true,
            };
            OldLogInjectionTracer = new Tracer(oldLogInjectionSettings);

            var newLogInjectionSettings = new TracerSettings
            {
                LogsInjectionEnabled = true,
            };
            NewLogInjectionTracer = new Tracer(newLogInjectionSettings);

#if DEBUG
            var writer = Console.Out;
#else
            var writer = TextWriter.Null;
#endif

            var target = new TextWriterTarget(writer)
            {
                Layout = "${longdate}|${uppercase:${level}}|${logger}|{signalfx.environment=${mdlc:item=signalfx.environment},signalfx.service=${mdlc:item=signalfx.service},signalfx.trace_id=${mdlc:item=signalfx.trace_id},signalfx.span_id=${mdlc:item=signalfx.span_id}}|${message}"
            };

            var config = new LoggingConfiguration();
            config.AddRuleForAllLevels(target);

            LogManager.Configuration = config;
        }

        [BenchmarkCategory("NoActiveSpanSwitch")]
        [Benchmark(Baseline = true)]
        public void NoTagging()
        {
            using (BaselineTracer.StartActive("NLog"))
            {
                NLogger.Info("Message during a trace.");
            }
        }

        [BenchmarkCategory("NoActiveSpanSwitch")]
        [Benchmark]
        public void OldTagging()
        {
            using (OldLogInjectionTracer.StartActive("NLog"))
            {
                NLogger.Info("Message during a trace.");
            }
        }

        [BenchmarkCategory("NoActiveSpanSwitch")]
        [Benchmark]
        public void NewNLog46Tagging()
        {
            using (NewLogInjectionTracer.StartActive("NLog"))
            {
                NLogger.Info("Message during a trace.");
            }
        }

        [BenchmarkCategory("ActiveSpanSwitch")]
        [Benchmark(Baseline = true)]
        public void NoTaggingSpanSwitch()
        {
            using (BaselineTracer.StartActive("Parent"))
            using (BaselineTracer.StartActive("Child"))
            {
                NLogger.Info("Message during a trace.");
            }
        }

        [BenchmarkCategory("ActiveSpanSwitch")]
        [Benchmark]
        public void OldTaggingSpanSwitch()
        {
            using (OldLogInjectionTracer.StartActive("Parent"))
            using (OldLogInjectionTracer.StartActive("Child"))
            {
                NLogger.Info("Message during a trace.");
            }
        }

        [BenchmarkCategory("ActiveSpanSwitch")]
        [Benchmark]
        public void NewNLog46TaggingSpanSwitch()
        {
            using (NewLogInjectionTracer.StartActive("Parent"))
            using (NewLogInjectionTracer.StartActive("Child"))
            {
                NLogger.Info("Message during a trace.");
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
