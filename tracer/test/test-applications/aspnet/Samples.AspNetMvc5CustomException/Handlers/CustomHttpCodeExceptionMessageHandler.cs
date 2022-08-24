// Modified by Splunk Inc.

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Samples.AspNetMvc5CustomException.Handlers
{
    public class CustomHttpCodeExceptionMessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            var codeAsString = request.RequestUri.PathAndQuery.Substring(request.RequestUri.PathAndQuery.Length - 3);

            response.StatusCode = (HttpStatusCode) int.Parse(codeAsString);
            return response;
        }
    }
}
