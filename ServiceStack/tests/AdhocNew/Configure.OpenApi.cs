using MyApp.Data;
using Scalar.AspNetCore;

[assembly: HostingStartup(typeof(MyApp.ConfigureOpenApi))]

namespace MyApp;

public class ConfigureOpenApi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            if (context.HostingEnvironment.IsDevelopment())
            {
                services.AddOpenApi();
                services.AddServiceStackOpenApi(configure: metadata =>
                {
                    metadata.AddBasicAuth();
                    //metadata.AddJwtBearer();
                });
            }
        })
        .Configure((context, app) => {
            if (context.HostingEnvironment.IsDevelopment())
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapOpenApi();
                    endpoints.MapScalarApiReference();
                });
            }
        });
}