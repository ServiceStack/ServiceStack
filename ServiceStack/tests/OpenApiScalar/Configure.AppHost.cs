using ServiceStack;
using OpenApiScalar.ServiceInterface;

[assembly: HostingStartup(typeof(OpenApiScalar.AppHost))]

namespace OpenApiScalar;

public class AppHost() : AppHostBase("OpenApiScalar", typeof(MyServices).Assembly), IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            // Additional ASP.NET Core services for tests can go here
        });

    public override void Configure()
    {
        // ServiceStack configuration for tests can go here
        SetConfig(new HostConfig
        {
            DebugMode = true
        });
    }
}
