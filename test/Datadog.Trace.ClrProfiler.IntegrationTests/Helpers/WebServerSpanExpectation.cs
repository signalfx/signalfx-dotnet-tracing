// Modified by SignalFx
using SignalFx.Tracing;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class WebServerSpanExpectation : SpanExpectation
    {
        public WebServerSpanExpectation(
            string serviceName,
            string operationName,
            string resourceName,
            string type = SpanTypes.Web,
            string statusCode = null,
            string httpMethod = null,
            bool addClientIpExpectation = true)
        : base(serviceName, operationName, resourceName, type)
        {
            StatusCode = statusCode;
            HttpMethod = httpMethod;

            // Expectations for all spans of a web server variety should go here
            RegisterTagExpectation(Tags.HttpStatusCode, expected: StatusCode);
            RegisterTagExpectation(Tags.HttpMethod, expected: HttpMethod);

            if (addClientIpExpectation)
            {
                TagShouldExist(Tags.PeerIpV4, when: s => !s.Tags.ContainsKey(Tags.PeerIpV6));
                TagShouldExist(Tags.PeerIpV6, when: s => !s.Tags.ContainsKey(Tags.PeerIpV4));
            }
            else
            {
                TagShouldNotExist(Tags.PeerIpV4);
                TagShouldNotExist(Tags.PeerIpV4);
            }
        }

        public string OriginalUri { get; set; }

        public string StatusCode { get; set; }

        public string HttpMethod { get; set; }
    }
}
