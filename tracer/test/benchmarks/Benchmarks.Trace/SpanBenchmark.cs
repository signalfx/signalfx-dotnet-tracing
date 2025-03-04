// Modified by Splunk Inc.

using BenchmarkDotNet.Attributes;
using Datadog.Trace;
using Datadog.Trace.BenchmarkDotNet;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;

namespace Benchmarks.Trace
{
    /// <summary>
    /// Span benchmarks
    /// </summary>
    [MemoryDiagnoser]
    [BenchmarkAgent6]
    public class SpanBenchmark
    {
        private static readonly Tracer Tracer;

        static SpanBenchmark()
        {
            var settings = new TracerSettings
            {
                TraceEnabled = false,
                StartupDiagnosticLogEnabled = false
            };

            Tracer = new Tracer(settings,  new DummyAgentWriter(), null, null, null);
        }

        /// <summary>
        /// Starts and finishes span benchmark
        /// </summary>
        [Benchmark]
        public void StartFinishSpan()
        {
            var span = Tracer.StartSpan("operation");
            span.SetTraceSamplingPriority(SamplingPriority.UserReject);
            span.Finish();
        }

        /// <summary>
        /// Starts and finishes an scope with span benchmark
        /// </summary>
        [Benchmark]
        public void StartFinishScope()
        {
            using (Scope scope = Tracer.StartActiveInternal("operation"))
            {
                scope.Span.SetTraceSamplingPriority(SamplingPriority.UserReject);
            }
        }
    }
}
