using Funq;
using ServiceStack;
using ServiceStack.Mvc;
using MyApp.ServiceInterface;

[assembly: HostingStartup(typeof(MyApp.AppHost))]

namespace MyApp;

public class AppHost : AppHostBase, IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            // Configure ASP.NET Core IOC Dependencies
        });

    public AppHost() : base("MyApp", typeof(MyServices).Assembly) {}

    public override void Configure(Container container)
    {
        SetConfig(new HostConfig {
        });

        Plugins.Add(new StaticFilePrettyUrlsFeature());
        
        Plugins.Add(new RazorFormat {
            RazorPages = true,
            ForbiddenRedirect = "/forbidden",
            //ForbiddenPartial = "~/Pages/Shared/Forbidden.cshtml", //alternative: render partial instead 
        });
        
        // For TodosService
        Plugins.Add(new AutoQueryDataFeature());
    }
}
