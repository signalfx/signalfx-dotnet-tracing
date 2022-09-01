// Modified by Splunk Inc.

using System.Web.Http;
using Samples.AspNetMvc5CustomException.Data;

namespace Samples.AspNetMvc5CustomException.Controllers
{
    public class ConventionsController : ApiController
    {
        [HttpGet]
        public IHttpActionResult ThrowCustomHttpCodeException(int httpStatusCode)
        {
            throw new CustomHttpCodeException();
        }
    }
}
