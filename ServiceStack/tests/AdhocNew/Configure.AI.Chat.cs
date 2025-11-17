using ServiceStack.AI;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(MyApp.ConfigureAiChat))]

namespace MyApp;

public class ConfigureAiChat : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddPlugin(new ChatFeature
            {
                // ValidateRequest = async (req) => req.GetApiKey()?.HasScope(RoleNames.Admin) == true 
                //     ? null 
                //     : HttpResult.Redirect("/admin-ui"),
                EnableProviders = [
                    "servicestack",
                    "groq",
                    "openrouter_free",
                ],
                // Variables = {
                //     ["GOOGLE_API_KEY"] = Environment.GetEnvironmentVariable("GOOGLE_FREE_API_KEY")!
                // }
            });
            // services.AddSingleton<IChatStore,PostgresChatStore>();
            services.AddSingleton<IChatStore,DbChatStore>();
             
            services.ConfigurePlugin<MetadataFeature>(feature => {
                feature.AddPluginLink("/chat", "AI Chat");
            });
       }).ConfigureAppHost(appHost => {
            using var db = appHost.Resolve<IDbConnectionFactory>().Open();
            db.CreateTableIfNotExists<ChatCompletionLog>();
       });
}
