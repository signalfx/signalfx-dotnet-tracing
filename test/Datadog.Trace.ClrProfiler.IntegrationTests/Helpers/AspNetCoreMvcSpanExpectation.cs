// Modified by SignalFx
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class AspNetCoreMvcSpanExpectation : WebServerSpanExpectation
    {
        public AspNetCoreMvcSpanExpectation(string serviceName, string operationName, string resourceName, string statusCode, string httpMethod, bool addClientIpExpectation = false)
            : base(serviceName, operationName, resourceName, SpanTypes.Web, statusCode, httpMethod, addClientIpExpectation)
        {
            RegisterTagExpectation(Tags.SpanKind, expected: SpanKinds.Server);
        }

        public override bool Matches(IMockSpan span)
        {
            var spanUri = GetTag(span, Tags.HttpUrl);
            if (spanUri == null || !spanUri.Contains(OriginalUri))
            {
                return false;
            }

            return base.Matches(span);
        }
    }
}
