// Modified by SignalFx
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Voyager;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;

namespace Samples.AdditionalDiagnosticListeners
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddInMemorySubscriptionProvider();

            services.AddControllers();

            services.AddGraphQL(sp => SchemaBuilder.New()
                .SetOptions(new SchemaOptions(){ StrictValidation = false })
                .AddServices(sp)
                .AddAuthorizeDirectiveType()
                .Create());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseGraphQL("/graphql");
        }
    }
}
