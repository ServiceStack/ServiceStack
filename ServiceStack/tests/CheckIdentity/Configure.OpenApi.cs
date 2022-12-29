using ServiceStack;
using ServiceStack.Api.OpenApi;

[assembly: HostingStartup(typeof(MyApp.ConfigureOpenApi))]

namespace MyApp;

public class ConfigureOpenApi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureAppHost(appHost => {
            appHost.Plugins.Add(new OpenApiFeature());
        });
}
