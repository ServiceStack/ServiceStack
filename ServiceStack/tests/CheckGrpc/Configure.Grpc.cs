using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(CheckGrpc.ConfigureGrpc))]

namespace CheckGrpc;

public class ConfigureGrpc : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => services.AddServiceStackGrpc())
        .ConfigureAppHost(appHost => {
            appHost.Plugins.Add(new GrpcFeature(appHost.GetApp()));
        });
}
