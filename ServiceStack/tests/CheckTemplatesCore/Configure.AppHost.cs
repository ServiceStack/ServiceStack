using System.Net;

[assembly: HostingStartup(typeof(CheckTemplatesCore.AppHost))]

namespace CheckTemplatesCore;

public class AppHost() : AppHostBase("MyApp", typeof(MyServices).Assembly), IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            // Configure ASP.NET Core IOC Dependencies
        });

    // Configure your AppHost with the necessary configuration and dependencies your App needs
    public override void Configure()
    {
        base.SetConfig(new HostConfig
        {
            DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), true)
        });

        Plugins.Add(new SharpPagesFeature());
            
        this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = 
            new SharpPageHandler("/notfound");
            
        this.GlobalHtmlErrorHttpHandler = new SharpPageHandler("/error");
    }
}
