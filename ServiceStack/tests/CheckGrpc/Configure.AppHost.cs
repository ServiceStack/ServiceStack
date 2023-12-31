using Microsoft.AspNetCore.Hosting;
using CheckGrpc.ServiceInterface;

[assembly: HostingStartup(typeof(CheckGrpc.AppHost))]

namespace CheckGrpc;

public class AppHost() : AppHostBase("MyApp", typeof(MyServices).Assembly), IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            // Configure ASP.NET Core IOC Dependencies
            services.RegisterService<GetFileService>();
        });

    // Configure your AppHost with the necessary configuration and dependencies your App needs
    public override void Configure()
    {
        SetConfig(new HostConfig
        {
            // DefaultRedirectPath = "/metadata",
            DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), false)
        });
        
        Plugins.Add(new SharpPagesFeature());
    }
}
