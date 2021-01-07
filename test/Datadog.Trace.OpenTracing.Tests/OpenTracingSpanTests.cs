// Modifed by SignalFx
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using OpenTracing;
using SignalFx.Tracing;
using SignalFx.Tracing.Agent;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.OpenTracing;
using SignalFx.Tracing.Sampling;
using Xunit;

namespace Datadog.Trace.OpenTracing.Tests
{
    public class OpenTracingSpanTests
    {
        private readonly OpenTracingTracer _tracer;

        public OpenTracingSpanTests()
        {
            var settings = new TracerSettings();
            var writerMock = new Mock<IAgentWriter>();
            var samplerMock = new Mock<ISampler>();

            var datadogTracer = new Tracer(settings, writerMock.Object, samplerMock.Object, scopeManager: null, statsd: null);
            _tracer = new OpenTracingTracer(datadogTracer);
        }

        [Fact]
        public void SetTag_Tags_TagsAreProperlySet()
        {
            ISpan span = GetScope("Op1").Span;

            span.SetTag("StringKey", "What's tracing");
            span.SetTag("IntKey", 42);
            span.SetTag("DoubleKey", 1.618);
            span.SetTag("BoolKey", true);

            var otSpan = (OpenTracingSpan)span;
            Assert.Equal("What's tracing", otSpan.GetTag("StringKey"));
            Assert.Equal("42", otSpan.GetTag("IntKey"));
            Assert.Equal("1.618", otSpan.GetTag("DoubleKey"));
            Assert.Equal("True", otSpan.GetTag("BoolKey"));
        }

        [Fact]
        public void Log_Logs_LogsAreProperlySet()
        {
            ISpan span = GetScope("Op1").Span;

            span.Log("Some Event");
            Thread.Sleep(TimeSpan.FromMilliseconds(15));

            var doubleDict = new Dictionary<string, object>() { { "event name", 123.45 } };
            span.Log(doubleDict);
            Thread.Sleep(TimeSpan.FromMilliseconds(15));

            var ex = new Exception("Some Exception");
            var exDict = new Dictionary<string, object>() { { "another event name", ex } };
            span.Log(exDict);
            Thread.Sleep(TimeSpan.FromMilliseconds(15));

            var now = DateTime.UtcNow.AddMilliseconds(1); // Add 1 msec to ensure different time than calls above.
            var then = now.AddMilliseconds(2); // TODO: currently if Log receives same timestamp previous are overwritten.
            span.Log(now, "Another Event");
            span.Log(then, exDict);

            var otSpan = (OpenTracingSpan)span;
            var logs = otSpan.Span.Logs;

            var sortedLogs = new SortedDictionary<DateTimeOffset, Dictionary<string, string>>(logs);
            Assert.Equal("Some Event", sortedLogs.ElementAt(0).Value["event"]);
            Assert.Equal("123.45", sortedLogs.ElementAt(1).Value["event name"]);
            Assert.Equal(ex.ToString(), sortedLogs.ElementAt(2).Value["another event name"]);
            Assert.Equal("Another Event", sortedLogs[now]["event"]);
            Assert.Equal(ex.ToString(), sortedLogs[then]["another event name"]);
            Assert.True(logs.Count == 5);
        }

        [Fact]
        public void SetOperationName_ValidOperationName_OperationNameIsProperlySet()
        {
            ISpan span = GetScope("Op0").Span;

            span.SetOperationName("Op1");

            Assert.Equal("Op1", ((OpenTracingSpan)span).OperationName);
        }

        [Fact]
        public void Finish_StartTimeInThePastWithNoEndTime_DurationProperlyComputed()
        {
            TimeSpan expectedDuration = TimeSpan.FromMinutes(1);
            var startTime = DateTimeOffset.UtcNow - expectedDuration;

            ISpan span = GetScope("Op1", startTime).Span;
            span.Finish();

            double durationDifference = Math.Abs((((OpenTracingSpan)span).Duration - expectedDuration).TotalMilliseconds);
            Assert.True(durationDifference < 100);
        }

        /*
        [Fact]
        public async Task Finish_NoEndTimeProvided_SpanWriten()
        {
            var span = new OpenTracingSpan(_tracer.StartActive(null, null, null, null));
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            span.Finish();

            _writerMock.Verify(x => x.WriteTrace(It.IsAny<List<Span>>()), Times.Once);
            Assert.True(span.DDSpan.Duration > TimeSpan.Zero);
        }
        */

        [Fact]
        public void Finish_EndTimeProvided_SpanWritenWithCorrectDuration()
        {
            var startTime = DateTimeOffset.UtcNow;
            var endTime = startTime.AddMilliseconds(10);

            ISpan span = GetScope("Op1", startTime).Span;
            span.Finish(endTime);

            Assert.Equal(endTime - startTime, ((OpenTracingSpan)span).Duration);
        }

        [Fact]
        public void Finish_EndTimeInThePast_DurationIs0()
        {
            var startTime = DateTimeOffset.UtcNow;
            var endTime = startTime.AddMilliseconds(-10);

            ISpan span = GetScope("Op1", startTime).Span;
            span.Finish(endTime);

            Assert.Equal(TimeSpan.Zero, ((OpenTracingSpan)span).Duration);
        }

        [Fact]
        public void Dispose_ExitUsing_SpanWriten()
        {
            OpenTracingSpan span;

            using (IScope scope = GetScope("Op1"))
            {
                span = (OpenTracingSpan)scope.Span;
            }

            Assert.True(span.Duration > TimeSpan.Zero);
        }

        [Fact]
        public void Context_TwoCalls_ContextStaysEqual()
        {
            ISpan span;
            global::OpenTracing.ISpanContext firstContext;

            using (IScope scope = GetScope("Op1"))
            {
                span = scope.Span;
                firstContext = span.Context;
            }

            var secondContext = span.Context;

            Assert.Same(firstContext, secondContext);
        }

        private IScope GetScope(string operationName, DateTimeOffset? startTime = null)
        {
            ISpanBuilder spanBuilder = new OpenTracingSpanBuilder(_tracer, operationName);

            if (startTime != null)
            {
                spanBuilder = spanBuilder.WithStartTimestamp(startTime.Value);
            }

            return spanBuilder.StartActive(finishSpanOnDispose: true);
        }
    }
}
