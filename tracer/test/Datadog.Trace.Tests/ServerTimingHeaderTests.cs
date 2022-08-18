// Modified by Splunk Inc.

using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Headers;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class ServerTimingHeaderTests : HeadersCollectionTestBase
    {
        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void SetHeaders_InjectsTheHeadersCorrectly(IHeadersCollection headers)
        {
            var traceId = TraceId.CreateRandom();
            var spanContext = new SpanContext(traceId, 123, SamplingPriority.AutoKeep);

            ServerTimingHeader.SetHeaders(spanContext, headers, (h, name, value) => h.Add(name, value));

            using (new AssertionScope())
            {
                headers.GetValues("Server-Timing").Should().HaveCount(1);
                headers.GetValues("Server-Timing").Should().Equal($"traceparent;desc=\"00-{traceId}-000000000000007b-01\"");
                headers.GetValues("Access-Control-Expose-Headers").Should().HaveCount(1);
                headers.GetValues("Access-Control-Expose-Headers").Should().Equal("Server-Timing");
            }
        }

        [Theory]
        [InlineData(null, false, "01")]
        [InlineData(SamplingPriority.AutoReject, false, "00")]
        [InlineData(SamplingPriority.UserKeep, false, "01")]
        [InlineData(null, true, "01")]
        [InlineData(SamplingPriority.UserReject, true, "00")]
        [InlineData(SamplingPriority.AutoKeep, true, "01")]
        internal void HandleContextCorrectly(SamplingPriority? samplingPriority, bool useTraceContext, string expectedSampled)
        {
            var traceId = TraceId.CreateRandom();
            var spanContext = useTraceContext
                ? new SpanContext(traceId, 123, samplingPriority)
                : new SpanContext(null, BuildTraceContext(samplingPriority), "someService", traceId, 123);

            var headers = GetHeaderCollectionImplementations().First()[0] as IHeadersCollection;
            ServerTimingHeader.SetHeaders(spanContext, headers, (h, name, value) => h.Add(name, value));

            using (new AssertionScope())
            {
                headers.GetValues("Server-Timing").Should().HaveCount(1);
                headers.GetValues("Server-Timing").Should().Equal($"traceparent;desc=\"00-{traceId}-000000000000007b-{expectedSampled}\"");
                headers.GetValues("Access-Control-Expose-Headers").Should().HaveCount(1);
                headers.GetValues("Access-Control-Expose-Headers").Should().Equal("Server-Timing");
            }

            TraceContext BuildTraceContext(SamplingPriority? samplingPriority)
            {
                var tc = new TraceContext(null, null);
                tc.SetSamplingPriority((int?)samplingPriority);
                return tc;
            }
        }
    }
}
