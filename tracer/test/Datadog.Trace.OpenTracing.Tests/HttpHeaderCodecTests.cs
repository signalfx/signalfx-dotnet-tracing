// <copyright file="HttpHeaderCodecTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using Xunit;

namespace Datadog.Trace.OpenTracing.Tests
{
    public class HttpHeaderCodecTests
    {
        // The values are duplicated here to make sure that if they are changed it will break tests
        private const string HttpHeaderTraceId = "trace-id";
        private const string HttpHeaderSpanId = "parent-id";

        private readonly HttpHeadersCodec _codec = new();

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
            Assert.Equal(spanId.ToString(), spanContext.Context.SpanId.ToString());
        }

        [Fact]
        public void Extract_WrongHeaderCase_ExtractionStillWorks()
        {
            TraceId traceId = TraceId.CreateFromInt(10);
            const ulong spanId = 120;

            var headers = new MockTextMap();
            headers.Set(HttpHeaderTraceId.ToUpper(), traceId.ToString());
            headers.Set(HttpHeaderSpanId.ToUpper(), spanId.ToString());
            // headers.Set(HttpHeaderSamplingPriority.ToUpper(), samplingPriority.ToString());

            var spanContext = _codec.Extract(headers) as OpenTracingSpanContext;

            Assert.NotNull(spanContext);
            Assert.Equal(traceId, spanContext.Context.TraceId);
            Assert.Equal(spanId.ToString(), spanContext.Context.SpanId.ToString());
        }

        [Fact]
        public void Inject_SpanContext_HeadersWithCorrectInfo()
        {
            const ulong spanId = 10;
            TraceId traceId = TraceId.CreateFromInt(7);
            const SamplingPriority samplingPriority = SamplingPriority.UserKeep;

            var ddSpanContext = new SpanContext(traceId, spanId, samplingPriority);
            var spanContext = new OpenTracingSpanContext(ddSpanContext);
            var headers = new MockTextMap();

            _codec.Inject(spanContext, headers);

            Assert.Equal(spanId, ulong.Parse(headers.Get(HttpHeaderSpanId)));
            Assert.Equal(traceId.ToString(), headers.Get(HttpHeaderTraceId));
        }
    }
}
