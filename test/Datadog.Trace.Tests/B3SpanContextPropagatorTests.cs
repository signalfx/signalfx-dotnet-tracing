// Modified by SignalFx
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using SignalFx.Tracing;
using SignalFx.Tracing.ExtensionMethods;
using SignalFx.Tracing.Headers;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class B3SpanContextPropagatorTests
    {
        [Fact]
        public void HttpRequestMessage_InjectExtract_Identity()
        {
            const ulong traceId = 18446744073709551615;
            const ulong spanId = 18446744073709551614;
            const SamplingPriority samplingPriority = SamplingPriority.AutoKeep;

            IHeadersCollection headers = new HttpRequestMessage().Headers.Wrap();
            var context = new SpanContext(traceId, spanId, samplingPriority);

            B3SpanContextPropagator.Instance.Inject(context, headers);

            AssertExpected(headers, HttpHeaderNames.B3TraceId, "ffffffffffffffff");
            AssertExpected(headers, HttpHeaderNames.B3SpanId, "fffffffffffffffe");
            AssertExpected(headers, HttpHeaderNames.B3Sampled, "1");
            AssertMissing(headers, HttpHeaderNames.B3ParentId);
            AssertMissing(headers, HttpHeaderNames.B3Flags);

            var resultContext = B3SpanContextPropagator.Instance.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(context.SpanId, resultContext.SpanId);
            Assert.Equal(context.TraceId, resultContext.TraceId);
            Assert.Equal(context.SamplingPriority, resultContext.SamplingPriority);
        }

        [Fact]
        public void HttpRequestMessage_InjectExtract_Identity_WithParent()
        {
            const ulong traceId = 18446744073709551615;
            const ulong spanId = 18446744073709551614;
            const SamplingPriority samplingPriority = SamplingPriority.UserKeep;

            IHeadersCollection headers = new HttpRequestMessage().Headers.Wrap();
            var parentContext = new SpanContext(traceId, spanId, samplingPriority);
            var traceContext = new TraceContext(null)
            {
                SamplingPriority = samplingPriority
            };

            var context = new SpanContext(parentContext, traceContext, null);

            B3SpanContextPropagator.Instance.Inject(context, headers);

            AssertExpected(headers, HttpHeaderNames.B3TraceId, "ffffffffffffffff");
            AssertExpected(headers, HttpHeaderNames.B3SpanId, context.SpanId.ToString("x16"));
            AssertExpected(headers, HttpHeaderNames.B3ParentId, "fffffffffffffffe");
            AssertExpected(headers, HttpHeaderNames.B3Flags, "1");
            AssertMissing(headers, HttpHeaderNames.B3Sampled);

            var resultContext = B3SpanContextPropagator.Instance.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(context.SpanId, resultContext.SpanId);
            Assert.Equal(context.TraceId, resultContext.TraceId);
            Assert.Equal(samplingPriority, resultContext.SamplingPriority);
        }

        [Fact]
        public void WebRequest_InjectExtract_Identity()
        {
            const int traceId = 2147483647;
            const int spanId = 2147483646;
            const SamplingPriority samplingPriority = SamplingPriority.AutoReject;

            IHeadersCollection headers = WebRequest.CreateHttp("http://localhost").Headers.Wrap();
            var context = new SpanContext(traceId, spanId, samplingPriority);

            B3SpanContextPropagator.Instance.Inject(context, headers);

            AssertExpected(headers, HttpHeaderNames.B3TraceId, "000000007fffffff");
            AssertExpected(headers, HttpHeaderNames.B3SpanId, "000000007ffffffe");
            AssertExpected(headers, HttpHeaderNames.B3Sampled, "0");
            AssertMissing(headers, HttpHeaderNames.B3ParentId);
            AssertMissing(headers, HttpHeaderNames.B3Flags);

            var resultContext = B3SpanContextPropagator.Instance.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(context.SpanId, resultContext.SpanId);
            Assert.Equal(context.TraceId, resultContext.TraceId);
            Assert.Equal(context.SamplingPriority, resultContext.SamplingPriority);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("trace.id")]
        public void Extract_InvalidTraceId(string traceId)
        {
            const string spanId = "7";
            const string samplingPriority = "2";

            var headers = InjectContext(traceId, spanId, samplingPriority);
            var resultContext = B3SpanContextPropagator.Instance.Extract(headers);

            // invalid traceId should return a null context even if other values are set
            Assert.Null(resultContext);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("span.id")]
        public void Extract_InvalidSpanId(string spanId)
        {
            const ulong traceId = 12345678;
            const SamplingPriority samplingPriority = SamplingPriority.UserKeep;

            var headers = InjectContext(
                traceId.ToString("x16", CultureInfo.InvariantCulture),
                spanId,
                ((int)samplingPriority).ToString(CultureInfo.InvariantCulture));
            var resultContext = B3SpanContextPropagator.Instance.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(traceId, resultContext.TraceId);
            Assert.Equal(default(ulong), resultContext.SpanId);
            Assert.Equal(samplingPriority, resultContext.SamplingPriority);
        }

        [Theory]
        [InlineData("-2")]
        [InlineData("3")]
        [InlineData("sampling.priority")]
        public void Extract_InvalidSamplingPriority(string samplingPriority)
        {
            const ulong traceId = 12345678;
            const ulong spanId = 23456789;

            var headers = InjectContext(
                traceId.ToString("x16", CultureInfo.InvariantCulture),
                spanId.ToString("x16", CultureInfo.InvariantCulture),
                samplingPriority);
            var resultContext = B3SpanContextPropagator.Instance.Extract(headers);

            Assert.NotNull(resultContext);
            Assert.Equal(traceId, resultContext.TraceId);
            Assert.Equal(spanId, resultContext.SpanId);
            Assert.Null(resultContext.SamplingPriority);
        }

        private static IHeadersCollection InjectContext(string traceId, string spanId, string samplingPriority)
        {
            IHeadersCollection headers = new HttpRequestMessage().Headers.Wrap();
            headers.Add(HttpHeaderNames.B3TraceId, traceId);
            headers.Add(HttpHeaderNames.B3SpanId, spanId);

            // Mimick the B3 injection mapping of samplingPriority
            switch (samplingPriority)
            {
                case "-1":
                // SamplingPriority.UserReject
                case "0":
                    // SamplingPriority.AutoReject
                    headers.Add(HttpHeaderNames.B3Flags, "0");
                    break;
                case "1":
                    // SamplingPriority.AutoKeep
                    headers.Add(HttpHeaderNames.B3Sampled, "1");
                    break;
                case "2":
                    // SamplingPriority.UserKeep
                    headers.Add(HttpHeaderNames.B3Flags, "1");
                    break;
                default:
                    // Invalid samplingPriority
                    break;
            }

            return headers;
        }

        private static void AssertExpected(IHeadersCollection headers, string key, string expected)
        {
            var matches = headers.GetValues(key);
            Assert.Single(matches);
            matches.ToList().ForEach(x => Assert.Equal(expected, x));
        }

        private static void AssertMissing(IHeadersCollection headers, string key)
        {
            var matches = headers.GetValues(key);
            Assert.Empty(matches);
        }
    }
}
