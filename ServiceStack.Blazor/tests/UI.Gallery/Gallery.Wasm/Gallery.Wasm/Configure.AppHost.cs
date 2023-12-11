using Funq;
using ServiceStack;
using ServiceStack.IO;
using ServiceStack.Configuration;
using MyApp.ServiceModel;
using MyApp.ServiceInterface;

[assembly: HostingStartup(typeof(MyApp.AppHost))]

namespace MyApp;

public class AppHost : AppHostBase, IHostingStartup
{
    public AppHost() : base("Blazor Gallery", typeof(MyServices).Assembly) { }

    public override void Configure(Container container)
    {
        SetConfig(new HostConfig {
        });

        Plugins.Add(new CorsFeature(allowedHeaders: "Content-Type,Authorization",
            allowOriginWhitelist: new[]{
            "http://localhost:5000",
            "https://localhost:5001",
            "https://localhost:5002",
            "http://localhost:3000",
            "http://localhost:5173",
            "http://localhost:8080",
            "https://docs.servicestack.net",
            "https://servicestack.net",
            "https://jamstacks.net",
            "https://razor-ssg.web-templates.io",
            "https://razor-press.web-templates.io",
            "https://" + Environment.GetEnvironmentVariable("DEPLOY_CDN")
        }, allowCredentials: true));

        var wwwrootVfs = GetVirtualFileSource<FileSystemVirtualFiles>();
        var appDataVfs = new FileSystemVirtualFiles(ContentRootDirectory.RealPath.CombineWith("App_Data").AssertDir());
        Plugins.Add(new FilesUploadFeature(
            new UploadLocation("profiles", wwwrootVfs, allowExtensions: FileExt.WebImages,
                resolvePath: ctx => $"/profiles/{ctx.FileName}"),
            new UploadLocation("users", wwwrootVfs, allowExtensions: FileExt.WebImages,
                resolvePath: ctx => $"/profiles/users/{ctx.UserAuthId}.{ctx.FileExtension}"),
            new UploadLocation("applications", appDataVfs, maxFileCount: 3, maxFileBytes: 10_000_000,
                    resolvePath: ctx => ctx.GetLocationPath((ctx.Dto is CreateJobApplication create
                        ? $"jobapp/{create.JobId}/{create.ContactId}/{ctx.FileName}"
                        : $"app/{ctx.Dto.GetId()}") + $"/{ctx.DateSegment}/{ctx.FileName}"),
                    readAccessRole: RoleNames.AllowAnon, writeAccessRole: RoleNames.AllowAnon)
        ));

        ScriptContext.Args["AppData"] = new AppData
        {
            Currencies = NumberCurrency.All,
            AlphaValues = new() {
                "Alpha", "Bravo", "Charlie"
            },
            AlphaDictionary = new()
            {
                ["A"] = "Alpha",
                ["B"] = "Bravo",
                ["C"] = "Charlie",
            },
            AlphaKeyValuePairs = new()
            {
                new("A","Alpha"),
                new("B","Bravo"),
                new("C","Charlie"),
            },
        };
    }

    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => 
            services.ConfigureNonBreakingSameSiteCookies(context.HostingEnvironment));
}
