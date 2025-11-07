using System.Data;
using ServiceStack;
using ServiceStack.AI;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Web;

[assembly: HostingStartup(typeof(MyApp.ConfigureAiChat))]

namespace MyApp;

public class ConfigureAiChat : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            
            var vfs = new FileSystemVirtualFiles(context.HostingEnvironment.ContentRootPath);
            services.AddPlugin(new ChatFeature {
                ConfigJson = vfs.GetFile("wwwroot/chat/llms.json").ReadAllText(),
                ValidateRequest = async req => null,
                EnableProviders = [
                    "servicestack"
                ]
            });
            
            // Persist Chat History
            // services.AddSingleton<IChatStore, DbChatStore>();
            services.AddSingleton<IChatStore>(c => new PostgresChatStore(
                c.GetRequiredService<ILogger<PostgresChatStore>>(), 
                c.GetRequiredService<IDbConnectionFactory>())
            {
                NamedConnection = "northwind"
            });
             
            services.ConfigurePlugin<MetadataFeature>(feature => {
                feature.AddPluginLink("/chat", "AI Chat");
            });
       });
}
