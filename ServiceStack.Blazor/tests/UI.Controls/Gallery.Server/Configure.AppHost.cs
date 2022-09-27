using Funq;
using ServiceStack;
using MyApp.ServiceInterface;
using ServiceStack.Admin;

[assembly: HostingStartup(typeof(MyApp.AppHost))]

namespace MyApp;

public class AppHost : AppHostBase, IHostingStartup
{
    public AppHost() : base("MyApp", typeof(MyServices).Assembly) { }

    public override void Configure(Container container)
    {
        SetConfig(new HostConfig {
        });

        Plugins.Add(new CorsFeature(allowedHeaders: "Content-Type,Authorization",
            allowOriginWhitelist: new[]{
            "http://localhost:5000",
            "https://localhost:5001",
            "https://localhost:7142",
            "https://" + Environment.GetEnvironmentVariable("DEPLOY_CDN")
        }, allowCredentials: true));

        //Plugins.Add(new RunAsAdminFeature());
        Plugins.Add(new RequestLogsFeature
        {
            EnableResponseTracking = true,
            EnableRequestBodyTracking = true,
        }); ;
        Plugins.Add(new ProfilingFeature());
    }

    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => 
            services.ConfigureNonBreakingSameSiteCookies(context.HostingEnvironment));
}
