// Modified by SignalFx

using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.Sampling;
using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.OpenTracing.Tests
{
    public class OpenTracingScopeTests
    {
        private readonly Tracer _signalFxTracer;

        public OpenTracingScopeTests()
        {
            var settings = new TracerSettings();
            var writerMock = new Mock<IAgentWriter>();
            var samplerMock = new Mock<ISampler>();

            _signalFxTracer = new Tracer(settings, writerMock.Object, samplerMock.Object, scopeManager: null, statsd: null);
        }

        [Fact]
        public void NewScope_FromExistingDDScope_FinishOnDispose()
        {
            var signalFxScope = _signalFxTracer.StartActive("testSpan");
            var openTracingScope = new OpenTracingScope((Scope)signalFxScope);

            var openTracingSpan = (OpenTracingSpan)openTracingScope.Span;
            var signalFxSpan = (Span)openTracingSpan.Span;
            Assert.True(signalFxSpan == signalFxScope.Span);

            openTracingSpan.SetTag("SetInOT", "123");
            signalFxSpan.GetTag("SetInOT").Should().Be("123");

            signalFxSpan.IsFinished.Should().BeFalse();
            openTracingScope.Dispose();
            signalFxSpan.IsFinished.Should().BeTrue();
        }

        [Fact]
        public void NewScope_FromExistingDDScope_WithoutFinishOnDispose()
        {
            var signalFxScope = _signalFxTracer.StartActive("testSpan", finishOnClose: false);
            var openTracingScope = new OpenTracingScope(signalFxScope);

            var openTracingSpan = (OpenTracingSpan)openTracingScope.Span;
            var signalFxSpan = (Span)openTracingSpan.Span;

            signalFxSpan.IsFinished.Should().BeFalse();
            openTracingScope.Dispose();
            signalFxSpan.IsFinished.Should().BeFalse();

            openTracingSpan.Finish();
            signalFxSpan.IsFinished.Should().BeTrue();
        }
    }
}
