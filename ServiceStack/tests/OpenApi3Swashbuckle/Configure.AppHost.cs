using ServiceStack;
using MyApp.ServiceInterface;

[assembly: HostingStartup(typeof(MyApp.AppHost))]

namespace MyApp;

public class AppHost() : AppHostBase("OpenApi3Swashbuckle", typeof(MyServices).Assembly), IHostingStartup
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

