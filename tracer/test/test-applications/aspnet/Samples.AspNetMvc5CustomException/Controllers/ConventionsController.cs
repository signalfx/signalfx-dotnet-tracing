using System.Web.Http;
using Samples.AspNetMvc5CustomException.Data;

namespace Samples.AspNetMvc5CustomException.Controllers
{
    public class ConventionsController : ApiController
    {
        [HttpGet]
        public IHttpActionResult ThrowCustomNotFoundException()
        {
            throw new CustomNotFoundException();
        }
    }
}
