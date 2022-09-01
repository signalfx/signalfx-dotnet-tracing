// Modified by Splunk Inc.

using System.Web.Http;
using Samples.AspNetMvc5CustomException.Handlers;

namespace Samples.AspNetMvc5CustomException
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();


            // Add global message handler
            config.MessageHandlers.Add(new CustomHttpCodeExceptionMessageHandler());

            config.Routes.MapHttpRoute(
                name: "ApiConventions",
                routeTemplate: "api2/{action}/{value}",
                defaults: new
                {
                    controller = "Conventions",
                    value = RouteParameter.Optional
                });
        }
    }
}
