// Modified by Splunk Inc.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Samples.AspNetMvc5CustomException.Handlers
{
    public class CustomNofFoundExceptionMessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            response.StatusCode = HttpStatusCode.NotFound;
            return response;
        }
    }
}
