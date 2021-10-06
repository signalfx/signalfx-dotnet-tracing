// Modified by Splunk Inc.

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
    }
}
