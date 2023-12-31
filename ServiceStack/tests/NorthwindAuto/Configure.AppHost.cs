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

namespace MyApp;

public class AppHost : AppHostBase
{
    public static string TalentBlazorDir = "../../../../../netcore/TalentBlazor/TalentBlazor";
    public static string TalentBlazorSeedDataDir = TalentBlazorDir + "/Migrations/seed";
    public static string TalentBlazorWwwRootDir = TalentBlazorDir + "/wwwroot";
    public static string ProfilesDir = $"{TalentBlazorWwwRootDir}/profiles";
        
    public AppHost() : base("My App", typeof(MyServices).Assembly) { }

    // Configure your AppHost with the necessary configuration and dependencies your App needs
    public override void Configure(Container container)
    {
        // JsConfig.Init(new ServiceStack.Text.Config {
        //     IncludeNullValues = true,
        //     TextCase = TextCase.PascalCase
        // });
        SetConfig(new HostConfig
        {
            DebugMode = true,
            AdminAuthSecret = "secret",
            // UseCamelCase = false,
        });
        
        Plugins.Add(new CorsFeature(new[] {
            "http://localhost:5173", //vite dev
            "https://docs.servicestack.net"
        }, allowCredentials:true));

        var memFs = GetVirtualFileSource<MemoryVirtualFiles>();

        memFs.WriteFile("locode/custom.html", VirtualFiles.GetFile("custom/locode/custom.html"));
        var files = VirtualFiles.GetDirectory("custom/locode/components").GetAllFiles();
        files.Each(file => memFs.WriteFile($"locode/components/{file.Name}", file));
        
        GlobalRequestFilters.Add((req, res, dto) => {
            memFs.WriteFile("locode/custom.html", VirtualFiles.GetFile("custom/locode/custom.html"));
            files.Each(file => memFs.WriteFile($"locode/components/{file.Name}", file));
        });

        ConfigurePlugin<UiFeature>(feature => {
            Console.WriteLine(@"ConfigurePlugin<UiFeature>...");
            
            feature.Module.Configure((appHost, module) =>
            {
                module.VirtualFiles = appHost.VirtualFiles;
                module.DirPath = module.DirPath.Replace("/modules", "");
            });
            feature.Handlers.Cast<SharedFolder>().Each(x => {
                if (x.Header == FilesTransformer.ModuleHeader)
                {
                    x.SharedDir = "/wwwroot" + x.SharedDir;
                }
                else
                {
                    x.SharedDir = x.SharedDir.Replace("/modules", "");
                }
            });
        });
            
        // Not needed in `dotnet watch` and in /wwwroot/modules/ui which can use _framework/aspnetcore-browser-refresh.js"
        Plugins.AddIfDebug(new HotReloadFeature
        {
            VirtualFiles = VirtualFiles,
            DefaultPattern = "*.html;*.js;*.mjs;*.css"
        });
        //Plugins.Add(new PostmanFeature());

        var uploadVfs = new FileSystemVirtualFiles(TalentBlazorWwwRootDir);
        var appDataVfs = new FileSystemVirtualFiles(TalentBlazorSeedDataDir);
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

        Metadata.ForceInclude = [typeof(GetAccessToken)];
        Plugins.Add(new ServiceStack.Api.OpenApi.OpenApiFeature());

        ScriptContext.Args[nameof(AppData)] = new AppData
        {
            AlphaValues = ["Alpha", "Bravo", "Charlie"],
            AlphaDictionary = new() {
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

    // public override string ResolveLocalizedString(string text, IRequest request = null) => 
    //     text == null ? null : $"({text})";

    public override string? GetCompressionType(IRequest request)
    {
        if (request.RequestPreferences.AcceptsDeflate && StreamCompressors.SupportsEncoding(CompressionTypes.Deflate))
            return CompressionTypes.Deflate;

        if (request.RequestPreferences.AcceptsGzip && StreamCompressors.SupportsEncoding(CompressionTypes.GZip))
            return CompressionTypes.GZip;

        return null;
    }
}