// Modified by SignalFx
using System;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.TestHelpers;
using Moq;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Util;
using SignalFx.Tracing;
using SignalFx.Tracing.Agent;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.OpenTracing;
using SignalFx.Tracing.Sampling;
using Xunit;

namespace Datadog.Trace.OpenTracing.Tests
{
    public class OpenTracingScopeManagerTests
    {
        private readonly Tracer _datadogTracer;

        public OpenTracingScopeManagerTests()
        {
            var settings = new TracerSettings();
            var writerMock = new Mock<IAgentWriter>();
            var samplerMock = new Mock<ISampler>();

            _datadogTracer = new Tracer(settings, writerMock.Object, samplerMock.Object, scopeManager: null, statsd: null);
        }

        [Fact]
        public void NewScopeManager_FromExistingDDScopeManager()
        {
            var ddScopeManager = ((ISignalFxTracer)_datadogTracer).ScopeManager;
            OpenTracingScopeManager otScopeManager = new OpenTracingScopeManager(ddScopeManager);

            Assert.True(ddScopeManager == otScopeManager.ScopeManager);
        }

        [Fact]
        public void ScopeManager_ActivatesWithFinishOnDispose()
        {
            var ddScopeManager = ((ISignalFxTracer)_datadogTracer).ScopeManager;
            OpenTracingScopeManager otScopeManager = new OpenTracingScopeManager(ddScopeManager);

            var tracer = new OpenTracingTracer(_datadogTracer, otScopeManager);

            Span parentSpan = null;
            Span childSpan = null;

            using (IScope parentScope = tracer.BuildSpan("parent").StartActive(finishSpanOnDispose: true))
            {
                parentSpan = ((OpenTracingSpan)parentScope.Span).Span;

                using (IScope childScope = tracer.BuildSpan("child").StartActive(finishSpanOnDispose: true))
                {
                    childSpan = ((OpenTracingSpan)childScope.Span).Span;
                    Assert.True(childSpan.Context.ParentId == parentSpan.SpanId);

                    Assert.False(childSpan.IsFinished);
                }

                Assert.True(childSpan.IsFinished);
                Assert.False(parentSpan.IsFinished);
            }

            Assert.True(parentSpan.IsFinished);
        }

        [Fact]
        public void ScopeManager_ActivatesWithoutFinishOnDispose()
        {
            var ddScopeManager = ((ISignalFxTracer)_datadogTracer).ScopeManager;
            OpenTracingScopeManager otScopeManager = new OpenTracingScopeManager(ddScopeManager);

            var tracer = new OpenTracingTracer(_datadogTracer, otScopeManager);

            OpenTracingSpan otParentSpan = null;
            OpenTracingSpan otChildSpan = null;
            Span parentSpan = null;
            Span childSpan = null;

            using (IScope parentScope = tracer.BuildSpan("parent").StartActive(finishSpanOnDispose: false))
            {
                otParentSpan = (OpenTracingSpan)parentScope.Span;
                parentSpan = otParentSpan.Span;

                using (IScope childScope = tracer.BuildSpan("child").StartActive(finishSpanOnDispose: false))
                {
                    otChildSpan = (OpenTracingSpan)childScope.Span;
                    childSpan = otChildSpan.Span;

                    Assert.True(childSpan.Context.ParentId == parentSpan.SpanId);

                    Assert.False(childSpan.IsFinished);
                }

                Assert.False(childSpan.IsFinished);
                Assert.False(parentSpan.IsFinished);
            }

            Assert.False(parentSpan.IsFinished);

            otChildSpan.Finish();
            otParentSpan.Finish();

            Assert.True(childSpan.IsFinished);
            Assert.True(parentSpan.IsFinished);
        }
    }
}
