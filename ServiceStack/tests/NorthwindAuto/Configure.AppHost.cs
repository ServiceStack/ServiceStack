using Funq;
using Microsoft.OpenApi.Models;
using ServiceStack;
using ServiceStack.Admin;
using ServiceStack.AspNetCore.OpenApi;
using ServiceStack.Configuration;
using ServiceStack.HtmlModules;
using ServiceStack.IO;
using Swashbuckle.AspNetCore.SwaggerGen;
using TalentBlazor.ServiceModel;
using GetAccessTokenResponse = ServiceStack.GetAccessTokenResponse;

[assembly: HostingStartup(typeof(MyApp.AppHost))]

namespace MyApp;

public class AppHost() : AppHostBase("My App"), IHostingStartup
{
    public static string TalentBlazorDir = "../../../../../netcore/TalentBlazor/TalentBlazor";
    public static string TalentBlazorSeedDataDir = TalentBlazorDir + "/Migrations/seed";
    public static string TalentBlazorWwwRootDir = TalentBlazorDir + "/wwwroot";
    public static string ProfilesDir = $"{TalentBlazorWwwRootDir}/profiles";

    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            
            var vfs = new FileSystemVirtualFiles(context.HostingEnvironment.ContentRootPath);
            var uploadVfs = new FileSystemVirtualFiles(TalentBlazorWwwRootDir);
            var appDataVfs = new FileSystemVirtualFiles(TalentBlazorSeedDataDir);
            services.AddPlugin(new FilesUploadFeature(
                new UploadLocation("profiles", uploadVfs, allowExtensions:FileExt.WebImages,
                    resolvePath:ctx => $"/profiles/{ctx.FileName}"),
            
                new UploadLocation("game_items", appDataVfs, allowExtensions:FileExt.WebImages),
            
                new UploadLocation("files", vfs,
                    resolvePath:ctx => $"/files/{ctx.FileName}"),
            
                new UploadLocation("users", uploadVfs, allowExtensions:FileExt.WebImages,
                    resolvePath:ctx => $"/profiles/users/{ctx.UserAuthId}.{ctx.FileExtension}"),

                new UploadLocation("applications", appDataVfs, maxFileCount: 3, maxFileBytes: 10_000_000,
                    resolvePath: ctx => ctx.GetLocationPath((ctx.Dto is CreateJobApplication create
                            ? $"job/{create.JobId}"
                            : $"app/{ctx.Dto.GetId()}") + $"/{ctx.DateSegment}/{ctx.FileName}"),
                    readAccessRole:RoleNames.AllowAnon, writeAccessRole:RoleNames.AllowAnon)
            ));

        });

    // Configure your AppHost with the necessary configuration and dependencies your App needs
    public override void Configure()
    {
        SetConfig(new HostConfig
        {
            DebugMode = true,
            AdminAuthSecret = "p@55wOrd",
        });
        
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
        // Plugins.AddIfDebug(new HotReloadFeature
        // {
        //     VirtualFiles = VirtualFiles,
        //     DefaultPattern = "*.html;*.js;*.mjs;*.css"
        // });

        Metadata.ForceInclude = [
            typeof(Authenticate),typeof(AuthenticateResponse),
            typeof(GetAccessToken),typeof(GetAccessTokenResponse),
            typeof(AdminDashboard),typeof(AdminDashboardResponse),
            typeof(AdminDatabase),typeof(AdminDatabaseResponse),
            typeof(AdminCreateUser),typeof(AdminDeleteUser),typeof(AdminDeleteUserResponse),typeof(AdminGetUser),typeof(AdminUserResponse),
            typeof(AdminQueryUsers),typeof(AdminUpdateUser),typeof(AdminUserBase),typeof(AdminUsersResponse),
            typeof(RequestLogs),typeof(RequestLogsResponse),
            typeof(AdminProfiling),typeof(AdminProfilingResponse),
            typeof(AdminRedis),typeof(AdminRedisResponse),
            typeof(GetValidationRules),typeof(ModifyValidationRules),typeof(ValidationRule),typeof(ValidateRule),
            typeof(GetValidationRulesResponse),
        ];

        ScriptContext.Args[nameof(AppData)] = new AppData
        {
            AlphaValues = ["Alpha", "Bravo", "Charlie"],
            AlphaDictionary = new() {
                ["A"] = "Alpha",
                ["B"] = "Bravo",
                ["C"] = "Charlie",
            },
            AlphaKeyValuePairs =
            [
                new("A", "Alpha"),
                new("B", "Bravo"),
                new("C", "Charlie")
            ], 
        };
    }
}
