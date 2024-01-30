using Funq;
using MyApp.ServiceInterface;

[assembly: HostingStartup(typeof(MyApp.AppHost))]

namespace MyApp;

public class AppHost() : AppHostBase("MyApp", typeof(MyServices).Assembly), IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            // Configure ASP.NET Core IOC Dependencies
            
            // For TodosService
            services.AddPlugin(new AutoQueryDataFeature());
        });

    public override void Configure(Container container)
    {
        SetConfig(new HostConfig {
        });
        
        ScriptContext.Args[nameof(AppData)] = AppData.Instance;
    }
}
