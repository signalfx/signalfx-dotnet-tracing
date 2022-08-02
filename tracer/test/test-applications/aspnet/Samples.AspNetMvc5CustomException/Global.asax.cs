// Modified by Splunk Inc.

using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Http;
using System;

namespace Samples.AspNetMvc5CustomException
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
