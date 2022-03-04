using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Http;
using System;

namespace Samples.AspNetMvc5
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var httpContext = HttpContext.Current;
            var transferRequested = httpContext.Request.QueryString["TransferRequest"].Equals("true", StringComparison.OrdinalIgnoreCase);

            if (transferRequested)
            {
                var errorRoute = "~/Error/Index";
                var errorId = "8DBA5152-2B3E-47C0-942B-0484A9FA2F84";

                var exception = httpContext.Server.GetLastError();
                System.Diagnostics.Debug.WriteLine(exception);

                httpContext.Server.ClearError();
                string queryString = $"?errorId={errorId}";
                if (httpContext.Items["ErrorStatusCode"] is int statusCode)
                {
                    queryString += $"&ErrorStatusCode={statusCode}";
                }

                if (HttpRuntime.UsingIntegratedPipeline)
                {
                    httpContext.Server.TransferRequest(errorRoute + queryString, false, "GET", null);
                }
                else
                {
                    httpContext.Response.StatusCode = 500;
                }
            }
        }
    }
}
