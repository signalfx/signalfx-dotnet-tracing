// <copyright file="HttpHeaderCodecTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Globalization;
using Datadog.Trace.Propagation;
using Xunit;

namespace Datadog.Trace.OpenTracing.Tests
{
    public class HttpHeaderCodecTests
    {
        private readonly HttpHeadersCodec _codec = new();

        [Fact]
        public void Extract_ValidParentAndTraceId_ProperSpanContext()
        {
            var traceId = TraceId.CreateRandom();
            const ulong spanId = 1200000000000000;

            var headers = new MockTextMap();
            headers.Set(B3HttpHeaderNames.B3TraceId, traceId.ToString());
            headers.Set(B3HttpHeaderNames.B3SpanId, spanId.ToString());

            var spanContext = _codec.Extract(headers) as OpenTracingSpanContext;

            Assert.NotNull(spanContext);
            Assert.Equal(traceId, spanContext.Context.TraceId);
            Assert.Equal(spanId.ToString(), spanContext.Context.SpanId.ToString("x16"));
        }

        [Fact]
        public void Extract_WrongHeaderCase_ExtractionStillWorks()
        {
            TraceId traceId = TraceId.CreateRandom();
            const ulong spanId = 1200000000000000;

            var headers = new MockTextMap();
            headers.Set(B3HttpHeaderNames.B3TraceId, traceId.ToString());
            headers.Set(B3HttpHeaderNames.B3SpanId, spanId.ToString());
            // headers.Set(HttpHeaderSamplingPriority.ToUpper(), samplingPriority.ToString());

            var spanContext = _codec.Extract(headers) as OpenTracingSpanContext;

            Assert.NotNull(spanContext);
            Assert.Equal(traceId, spanContext.Context.TraceId);
            Assert.Equal(spanId.ToString(), spanContext.Context.SpanId.ToString("x16"));
        }

        [Fact]
        public void Inject_SpanContext_HeadersWithCorrectInfo()
        {
            const ulong spanId = 1200000000000000;
            TraceId traceId = TraceId.CreateRandom();
            const SamplingPriority samplingPriority = SamplingPriority.UserKeep;

            var ddSpanContext = new SpanContext(traceId, spanId, samplingPriority);
            var spanContext = new OpenTracingSpanContext(ddSpanContext);
            var headers = new MockTextMap();

            _codec.Inject(spanContext, headers);

            Assert.Equal(spanId, ulong.Parse(headers.Get(B3HttpHeaderNames.B3SpanId), NumberStyles.HexNumber));
            Assert.Equal(traceId.ToString(), headers.Get(B3HttpHeaderNames.B3TraceId));
        }
    }
}
