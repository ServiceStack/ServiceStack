using Funq;
using ServiceStack;
using MyApp.ServiceInterface;
using ServiceStack.Configuration;
using ServiceStack.IO;

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
            "https://" + Environment.GetEnvironmentVariable("DEPLOY_CDN")
        }, allowCredentials: true));

        var fsVfs = new FileSystemVirtualFiles(Path.Join(this.ContentRootDirectory.RealPath, "files_tmp").AssertDir());
        
        Plugins.Add(new FilesUploadFeature(
            new UploadLocation("fs",fsVfs, writeAccessRole: RoleNames.AllowAnon)));
    }

    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => 
            services.ConfigureNonBreakingSameSiteCookies(context.HostingEnvironment));
}
