// Modified by SignalFx

using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.Sampling;
using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.OpenTracing.Tests
{
    public class OpenTracingScopeManagerTests
    {
        private readonly Tracer _signalFxTracer;

        public OpenTracingScopeManagerTests()
        {
            var settings = new TracerSettings();
            var writerMock = new Mock<IAgentWriter>();
            var samplerMock = new Mock<ISampler>();

            _signalFxTracer = new Tracer(settings, writerMock.Object, samplerMock.Object, scopeManager: null, statsd: null);
        }

        [Fact]
        public void NewScopeManager_FromExistingDDScopeManager()
        {
            var signalFxScopeManager = _signalFxTracer.ScopeManager;
            var openTracingScopeManager = new OpenTracingScopeManager(signalFxScopeManager);

            Assert.True(signalFxScopeManager == openTracingScopeManager.ScopeManager);
        }

        [Fact]
        public void ScopeManager_ActivatesWithFinishOnDispose()
        {
            var ddScopeManager = _signalFxTracer.ScopeManager;
            var openTracingScopeManager = new OpenTracingScopeManager(ddScopeManager);

            var tracer = new OpenTracingTracer(_signalFxTracer, openTracingScopeManager);

            Span parentSpan;

            using (var parentScope = tracer.BuildSpan("parent").StartActive(finishSpanOnDispose: true))
            {
                parentSpan = (Span)((OpenTracingSpan)parentScope.Span).Span;

                Span childSpan;
                using (var childScope = tracer.BuildSpan("child").StartActive(finishSpanOnDispose: true))
                {
                    childSpan = (Span)((OpenTracingSpan)childScope.Span).Span;
                    childSpan.Context.Parent.SpanId.Should().Be(parentSpan.SpanId);

                    childSpan.IsFinished.Should().BeFalse();
                }

                childSpan.IsFinished.Should().BeTrue();
                parentSpan.IsFinished.Should().BeFalse();
            }

            parentSpan.IsFinished.Should().BeTrue();
        }

        [Fact]
        public void ScopeManager_ActivatesWithoutFinishOnDispose()
        {
            var ddScopeManager = _signalFxTracer.ScopeManager;
            var openTracingScopeManager = new OpenTracingScopeManager(ddScopeManager);

            var tracer = new OpenTracingTracer(_signalFxTracer, openTracingScopeManager);

            OpenTracingSpan openTracingParentSpan;
            OpenTracingSpan openTracingChildSpan;
            Span parentSpan;
            Span childSpan;

            using (var parentScope = tracer.BuildSpan("parent").StartActive(finishSpanOnDispose: false))
            {
                openTracingParentSpan = (OpenTracingSpan)parentScope.Span;
                parentSpan = (Span)openTracingParentSpan.Span;

                using (var childScope = tracer.BuildSpan("child").StartActive(finishSpanOnDispose: false))
                {
                    openTracingChildSpan = (OpenTracingSpan)childScope.Span;
                    childSpan = (Span)openTracingChildSpan.Span;

                    childSpan.Context.Parent.SpanId.Should().Be(parentSpan.SpanId);

                    childSpan.IsFinished.Should().BeFalse();
                }

                childSpan.IsFinished.Should().BeFalse();
                parentSpan.IsFinished.Should().BeFalse();
            }

            parentSpan.IsFinished.Should().BeFalse();

            openTracingParentSpan.Finish();
            openTracingChildSpan.Finish();

            childSpan.IsFinished.Should().BeTrue();
            parentSpan.IsFinished.Should().BeTrue();
        }
    }
}
