// Modified by SignalFx
using System;
using Moq;
using SignalFx.Tracing;
using SignalFx.Tracing.Agent;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.OpenTracing;
using SignalFx.Tracing.Sampling;
using Xunit;

namespace Datadog.Trace.OpenTracing.Tests
{
    public class OpenTracingScopeTests
    {
        private readonly OpenTracingTracer _tracer;
        private readonly Tracer _datadogTracer;

        public OpenTracingScopeTests()
        {
            var settings = new TracerSettings();
            var writerMock = new Mock<IAgentWriter>();
            var samplerMock = new Mock<ISampler>();

            _datadogTracer = new Tracer(settings, writerMock.Object, samplerMock.Object, scopeManager: null, statsd: null);
            _tracer = new OpenTracingTracer(_datadogTracer);
        }

        [Fact]
        public void NewScope_FromExistingDDScope_FinishOnDispose()
        {
            var ddScope = _datadogTracer.StartActive("testSpan");
            OpenTracingScope otScope = new OpenTracingScope(ddScope);

            var otSpan = (OpenTracingSpan)otScope.Span;
            var ddSpan = otSpan.Span;
            Assert.True(ddSpan == ddScope.Span);

            otSpan.SetTag("SetInOT", "123");
            Assert.Equal("123", ddSpan.GetTag("SetInOT"));

            Assert.False(ddSpan.IsFinished);
            otScope.Dispose();
            Assert.True(ddSpan.IsFinished);
        }

        [Fact]
        public void NewScope_FromExistingDDScope_WithoutFinishOnDispose()
        {
            var ddScope = _datadogTracer.StartActive("testSpan", finishOnClose: false);
            OpenTracingScope otScope = new OpenTracingScope(ddScope);

            var otSpan = (OpenTracingSpan)otScope.Span;
            var ddSpan = otSpan.Span;

            Assert.False(ddSpan.IsFinished);
            otScope.Dispose();
            Assert.False(ddSpan.IsFinished);

            otSpan.Finish();
            Assert.True(ddSpan.IsFinished);
        }
    }
}
