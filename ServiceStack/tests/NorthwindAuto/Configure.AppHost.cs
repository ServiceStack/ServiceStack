using Funq;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using MyApp.ServiceInterface;
using ServiceStack.Configuration;
using ServiceStack.HtmlModules;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;
using TalentBlazor.ServiceModel;

[assembly: HostingStartup(typeof(MyApp.AppHost))]

namespace MyApp;

public class AppHost : AppHostBase, IHostingStartup
{
    public static string TalentBlazorDir = "C:\\src\\netcore\\TalentBlazor\\TalentBlazor";
    public static string TalentBlazorAppDataDir = TalentBlazorDir + "\\App_Data";
    public static string TalentBlazorWwwRootDir = TalentBlazorDir + "\\wwwroot";
    public static string ProfilesDir = $"{TalentBlazorWwwRootDir}\\profiles";

    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => services.AddHttpUtilsClient())
        .Configure(app => {
            if (!HasInit) 
                app.UseServiceStack(new AppHost());
        });
        
    public AppHost() : base("Northwind Auto", typeof(MyServices).Assembly) { }

    // Configure your AppHost with the necessary configuration and dependencies your App needs
    public override void Configure(Container container)
    {
        // JsConfig.Init(new ServiceStack.Text.Config {
        //     IncludeNullValues = true,
        //     TextCase = TextCase.PascalCase
        // });
        SetConfig(new HostConfig
        {
            //DebugMode = false,
            DebugMode = true,
            AdminAuthSecret = "secret",
        });

        var memFs = GetVirtualFileSource<MemoryVirtualFiles>();
        var files = VirtualFiles.GetDirectory("custom").GetAllFiles();
        files.Each(file => memFs.WriteFile($"locode/{file.Name}", file));
        GlobalRequestFilters.Add((req, res, dto) => {
            files.Each(file => memFs.WriteFile($"locode/{file.Name}", file));
        });

        ConfigurePlugin<UiFeature>(feature => {
            Console.WriteLine(@"ConfigurePlugin<UiFeature>...");
            feature.Module.Configure((appHost, module) =>
            {
                module.VirtualFiles = appHost.VirtualFiles;
                module.DirPath = module.DirPath.Replace("/modules", "");
            });
            feature.Handlers.Cast<SharedFolder>().Each(x => 
                x.SharedDir = x.SharedDir.Replace("/modules", ""));
        });
            
        // Not needed in `dotnet watch` and in /wwwroot/modules/ui which can use _framework/aspnetcore-browser-refresh.js"
        Plugins.AddIfDebug(new HotReloadFeature
        {
            VirtualFiles = VirtualFiles,
            DefaultPattern = "*.html;*.js;*.css"
        });
        //Plugins.Add(new PostmanFeature());

        var uploadVfs = new FileSystemVirtualFiles(TalentBlazorWwwRootDir);
        var appDataVfs = new FileSystemVirtualFiles(TalentBlazorAppDataDir);
        Plugins.Add(new FilesUploadFeature(
            new UploadLocation("profiles", uploadVfs, allowExtensions:FileExt.WebImages,
                resolvePath:ctx => $"/profiles/{ctx.FileName}"),
            new UploadLocation("game_items", appDataVfs, allowExtensions:FileExt.WebImages),
            new UploadLocation("files", GetVirtualFileSource<FileSystemVirtualFiles>(),
                resolvePath:ctx => $"/files/{ctx.FileName}"),
            new UploadLocation("users", uploadVfs, allowExtensions:FileExt.WebImages,
                resolvePath:ctx => $"/profiles/users/{ctx.UserAuthId}.{ctx.FileExtension}"),
            new UploadLocation("applications", appDataVfs, maxFileCount: 3, maxFileBytes: 10_000_000,
                resolvePath: ctx => ctx.GetLocationPath((ctx.Dto is CreateJobApplication create
                    ? $"job/{create.JobId}"
                    : $"app/{ctx.Dto.GetId()}") + $"/{ctx.DateSegment}/{ctx.FileName}"),
                readAccessRole:RoleNames.AllowAnon, writeAccessRole:RoleNames.AllowAnon)
        ));

        Metadata.ForceInclude = new() { 
            typeof(GetAccessToken)
        };
        Plugins.Add(new ServiceStack.Api.OpenApi.OpenApiFeature());
    }

    public override string? GetCompressionType(IRequest request)
    {
        if (request.RequestPreferences.AcceptsDeflate && StreamCompressors.SupportsEncoding(CompressionTypes.Deflate))
            return CompressionTypes.Deflate;

        if (request.RequestPreferences.AcceptsGzip && StreamCompressors.SupportsEncoding(CompressionTypes.GZip))
            return CompressionTypes.GZip;

        return null;
    }
}
