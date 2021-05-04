// Modified by SignalFx

using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using SignalFx.Tracing;
using SignalFx.Tracing.Headers;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class ServerTimingHeaderTests : HeadersCollectionTestBase
    {
        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        public void SetHeaders_InjectsTheHeadersCorrectly(IHeadersCollection headers)
        {
            var traceId = TraceId.CreateRandom();
            var spanContext = new SpanContext(traceId, 123, SamplingPriority.AutoKeep);

            ServerTimingHeader.SetHeaders(spanContext, headers, (h, name, value) => h.Add(name, value));

            using (new AssertionScope())
            {
                headers.GetValues("Server-Timing").Should().HaveCount(1);
                headers.GetValues("Server-Timing").Should().Equal($"traceparent;desc=\"00-{traceId.ToString()}-000000000000007b-01\"");
                headers.GetValues("Access-Control-Expose-Headers").Should().HaveCount(1);
                headers.GetValues("Access-Control-Expose-Headers").Should().Equal("Server-Timing");
            }
        }
    }
}
