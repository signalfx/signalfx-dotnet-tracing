using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Samples.AspNetCoreMvc.Shared
{
    public class MyHttpException : Exception
    {
        public MyHttpException(int statusCode)
            : base($"Status code has been set to {statusCode}")
        {
            HttpStatusCode = statusCode;
        }

        public int HttpStatusCode { get; }

        public static void Handler(IApplicationBuilder c)
        {
            c.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerPathFeature>().Error;
                if (exception is MyHttpException myHttpException)
                {
                    context.Response.StatusCode = myHttpException.HttpStatusCode;
                }
                else
                {
                    context.Response.StatusCode = 500;
                }
                await context.Response.WriteAsync(exception.Message);
            });
        }
    }
}
