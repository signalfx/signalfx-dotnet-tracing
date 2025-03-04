using System.Globalization;
using Datadog.Trace.Propagation;
using Datadog.Trace.TestHelpers;
using Xunit;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.Helpers
{
    public static class PropagationTestHelpers
    {
        public static void AssertPropagationEnabled(MockSpan expectedSpan, ProcessResult processResult)
        {
            // Verify B3 headers
            var b3TraceId = StringUtil.GetHeader(processResult.StandardOutput, B3HttpHeaderNames.B3TraceId);
            var b3SpanId = StringUtil.GetHeader(processResult.StandardOutput, B3HttpHeaderNames.B3SpanId);

            Assert.Equal(expectedSpan.TraceId.ToString(), b3TraceId); // TODO: With DD Trace ID Convention, it will be in DD Trace ID format
            Assert.Equal(expectedSpan.SpanId.ToString("x16", CultureInfo.InvariantCulture), b3SpanId);
        }

        public static void AssertPropagationDisabled(ProcessResult processResult)
        {
            // Verify common headers
            var tracingEnabled = StringUtil.GetHeader(processResult.StandardOutput, CommonHttpHeaderNames.TracingEnabled);

            Assert.Equal("false", tracingEnabled);

            // Verify DD headers
            var ddTraceId = StringUtil.GetHeader(processResult.StandardOutput, B3HttpHeaderNames.B3TraceId);
            var ddParentSpanId = StringUtil.GetHeader(processResult.StandardOutput, B3HttpHeaderNames.B3SpanId);

            Assert.Null(ddTraceId);
            Assert.Null(ddParentSpanId);

            // Verify B3 headers
            var b3TraceId = StringUtil.GetHeader(processResult.StandardOutput, B3HttpHeaderNames.B3TraceId);
            var b3SpanId = StringUtil.GetHeader(processResult.StandardOutput, B3HttpHeaderNames.B3SpanId);
            var b3ParentSpanId = StringUtil.GetHeader(processResult.StandardOutput, B3HttpHeaderNames.B3ParentId);

            Assert.Null(b3TraceId);
            Assert.Null(b3SpanId);
            Assert.Null(b3ParentSpanId);
        }
    }
}
