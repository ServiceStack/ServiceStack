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

[assembly: HostingStartup(typeof(MyApp.AppHost))]

namespace MyApp;

public class AppHost : AppHostBase, IHostingStartup
{
    public static string TalentBlazorDir = "C:\\src\\netcore\\TalentBlazor\\TalentBlazor\\wwwroot";
    public static string ProfilesDir = $"{TalentBlazorDir}\\profiles";

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
        // JsConfig.Init(new Config { TextCase = TextCase.PascalCase });
            
        SetConfig(new HostConfig
        {
            //DebugMode = false,
            DebugMode = true,
            AdminAuthSecret = "secret",
        });

        // Register Database Connection, see: https://github.com/ServiceStack/ServiceStack.OrmLite#usage
        // container.AddSingleton<IDbConnectionFactory>(c =>
        //     new OrmLiteConnectionFactory(MapProjectPath("~/northwind.sqlite"), SqliteDialect.Provider));

        ConfigurePlugin<UiFeature>(feature => {
            Console.WriteLine(@"ConfigurePlugin<UiFeature>...");
            //feature.Module.EnableHttpCaching = true;
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

        var uploadVfs = new FileSystemVirtualFiles(TalentBlazorDir);
        var appDataVfs = new FileSystemVirtualFiles(ContentRootDirectory.RealPath.CombineWith("App_Data"));
        Plugins.Add(new FilesUploadFeature(
            new UploadLocation("profiles", uploadVfs, allowExtensions:FileExt.WebImages),
            new UploadLocation("users", uploadVfs, allowExtensions:FileExt.WebImages,
                resolvePath:(req,fileName) => $"/profiles/users/{req.GetSession().UserAuthId}.{fileName.LastRightPart('.')}"),
            new UploadLocation("game_items", appDataVfs, allowExtensions:FileExt.WebImages)
        ));
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
