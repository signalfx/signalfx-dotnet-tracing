// <copyright file="HttpHeaderCodecTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Globalization;
using Datadog.Trace.Conventions;
using Datadog.Trace.Propagation;
using Xunit;

namespace Datadog.Trace.OpenTracing.Tests
{
    public class HttpHeaderCodecTests
    {
        // The values are duplicated here to make sure that if they are changed it will break tests
        private const string HttpHeaderTraceId = B3HttpHeaderNames.B3TraceId;
        private const string HttpHeaderSpanId = B3HttpHeaderNames.B3SpanId;

        private readonly HttpHeadersCodec _codec = new(new B3SpanContextPropagator(new DatadogTraceIdConvention()));

        [Fact]
        public void Extract_ValidParentAndTraceId_ProperSpanContext()
        {
            var traceId = TraceId.CreateFromInt(10);
            const ulong spanId = 120;

            var headers = new MockTextMap();
            headers.Set(HttpHeaderTraceId, traceId.ToString());
            headers.Set(HttpHeaderSpanId, spanId.ToString());

            var spanContext = _codec.Extract(headers) as OpenTracingSpanContext;

            Assert.NotNull(spanContext);
            Assert.Equal(traceId, spanContext.Context.TraceId);
            Assert.Equal(spanId.ToString(), spanContext.Context.SpanId.ToString("X"));
        }

        [Fact]
        public void Extract_WrongHeaderCase_ExtractionStillWorks()
        {
            TraceId traceId = TraceId.CreateFromInt(10);
            const ulong parentId = 120;
            const int samplingPriority = SamplingPriorityValues.UserKeep;

            var headers = new MockTextMap();
            headers.Set(HttpHeaderTraceId.ToUpper(), traceId.ToString());
            headers.Set(HttpHeaderParentId.ToUpper(), parentId.ToString());
            headers.Set(HttpHeaderSamplingPriority.ToUpper(), samplingPriority.ToString());

            var spanContext = _codec.Extract(headers) as OpenTracingSpanContext;

            Assert.NotNull(spanContext);
            Assert.Equal(traceId, spanContext.Context.TraceId);
            Assert.Equal(spanId.ToString(), spanContext.Context.SpanId.ToString("X"));
        }

        [Fact]
        public void Inject_SpanContext_HeadersWithCorrectInfo()
        {
            const ulong spanId = 10;
            TraceId traceId = TraceId.CreateFromInt(7);
            const int samplingPriority = SamplingPriorityValues.UserKeep;

            var ddSpanContext = new SpanContext(traceId, spanId, (SamplingPriority)samplingPriority);
            var spanContext = new OpenTracingSpanContext(ddSpanContext);
            var headers = new MockTextMap();

            _codec.Inject(spanContext, headers);

            Assert.Equal(spanId, ulong.Parse(headers.Get(HttpHeaderSpanId), NumberStyles.HexNumber));
            Assert.Equal(traceId.ToString(), headers.Get(HttpHeaderTraceId));
<<<<<<< HEAD
=======
            Assert.Equal(samplingPriority.ToString(), headers.Get(HttpHeaderSamplingPriority));
>>>>>>> 41e924c48 (use `int` internally instead of `enum` for sampling priority values (#2372))
        }
    }
}
