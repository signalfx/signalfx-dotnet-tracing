// Modified by SignalFx

using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using SignalFx.Tracing;
using SignalFx.Tracing.Headers;
using SignalFx.Tracing.Propagation;
using Xunit;

namespace Datadog.Trace.Tests.Propagators
{
    public class W3CSpanContextPropagatorTests : HeadersCollectionTestBase
    {
        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Inject_CratesCorrectTraceParentHeader(IHeadersCollection headers)
        {
            var traceId = TraceId.Parse("0af7651916cd43dd8448eb211c80319c");
            const ulong spanId = 67667974448284343;
            var spanContext = new SpanContext(traceId, spanId, samplingPriority: null);
            var propagator = W3CSpanContextPropagator.Instance;

            propagator.Inject(spanContext, headers);

            headers.GetValues(W3CHeaderNames.TraceParent).Should().Equal("00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01");
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Inject_CreateCorrectTraceStateHeaderIfPresent(IHeadersCollection headers)
        {
            var traceId = TraceId.Parse("0af7651916cd43dd8448eb211c80319c");
            const ulong spanId = 67667974448284343;
            var spanContext = new SpanContext(traceId, spanId, samplingPriority: null, serviceName: null, "state");
            var propagator = W3CSpanContextPropagator.Instance;

            propagator.Inject(spanContext, headers);

            headers.GetValues(W3CHeaderNames.TraceState).Should().Equal("state");
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Inject_DoNotCreateCorrectTraceStateHeaderIfNotPresent(IHeadersCollection headers)
        {
            var traceId = TraceId.Parse("0af7651916cd43dd8448eb211c80319c");
            const ulong spanId = 67667974448284343;
            var spanContext = new SpanContext(traceId, spanId, samplingPriority: null);
            var propagator = W3CSpanContextPropagator.Instance;

            propagator.Inject(spanContext, headers);

            headers.GetValues(W3CHeaderNames.TraceState).Should().HaveCount(0);
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Extract_ReturnCorrectTraceAndSpanIdInContext(IHeadersCollection headers)
        {
            var propagator = W3CSpanContextPropagator.Instance;
            headers.Set(W3CHeaderNames.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01");

            var spanContext = propagator.Extract(headers);

            using (new AssertionScope())
            {
                spanContext.SpanId.Should().Be(67667974448284343);
                spanContext.TraceId.Should().Be(TraceId.Parse("0af7651916cd43dd8448eb211c80319c"));
            }
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Extract_ReturnCorrectTraceStateInContextIfPresent(IHeadersCollection headers)
        {
            var propagator = W3CSpanContextPropagator.Instance;
            headers.Set(W3CHeaderNames.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01");
            headers.Set(W3CHeaderNames.TraceState, "state=2,am=dsa");

            var spanContext = propagator.Extract(headers);

            spanContext.TraceState.Should().Be("state=2,am=dsa");
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Extract_DoNotReturnTraceStateInContextIfNotPresent(IHeadersCollection headers)
        {
            var propagator = W3CSpanContextPropagator.Instance;
            headers.Set(W3CHeaderNames.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01");

            var spanContext = propagator.Extract(headers);

            spanContext.TraceState.Should().BeNullOrEmpty();
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Extract_OmitsTraceStateWithIncorrectValue(IHeadersCollection headers)
        {
            var propagator = W3CSpanContextPropagator.Instance;
            headers.Set(W3CHeaderNames.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01");
            headers.Set(W3CHeaderNames.TraceState, "state=,arn=2");

            var spanContext = propagator.Extract(headers);

            spanContext.TraceState.Should().BeNullOrEmpty();
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Extract_OmitsTraceStateWithIncorrectKey(IHeadersCollection headers)
        {
            var propagator = W3CSpanContextPropagator.Instance;
            headers.Set(W3CHeaderNames.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01");
            headers.Set(W3CHeaderNames.TraceState, "statDSAe=3,arn=2");

            var spanContext = propagator.Extract(headers);

            spanContext.TraceState.Should().BeNullOrEmpty();
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void Extract_OmitsEmptyTraceState(IHeadersCollection headers)
        {
            var propagator = W3CSpanContextPropagator.Instance;
            headers.Set(W3CHeaderNames.TraceParent, "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01");
            headers.Set(W3CHeaderNames.TraceState, string.Empty);

            var spanContext = propagator.Extract(headers);

            spanContext.TraceState.Should().BeNullOrEmpty();
        }
    }
}
