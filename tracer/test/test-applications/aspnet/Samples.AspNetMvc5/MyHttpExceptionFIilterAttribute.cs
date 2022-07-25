using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace Samples.AspNetMvc5
{
    public class MyHttpExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is MyHttpException myHttpException)
            {
                var response = new HttpResponseMessage((HttpStatusCode)myHttpException.HttpStatusCode)
                {
                    Content = new StringContent(myHttpException.Message)
                };
                context.Response = response;
            }
        }
    }
}
