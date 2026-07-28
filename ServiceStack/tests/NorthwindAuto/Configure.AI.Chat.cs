using ServiceStack;
using ServiceStack.AI;

[assembly: HostingStartup(typeof(MyApp.ConfigureAiChat))]

namespace MyApp;

/// <summary>
/// AI Chat using this host's ASP.NET Identity users, signed in with the Chat UI's own
/// username/password form (AuthType=Credentials installs the 'credentials' extension, which
/// authenticates with the Authenticate API). Set RequireAuth = false for open access, where all
/// chat data is stored under the "default" user, matching llms-py's behaviour with no auth
/// extension installed.
/// </summary>
public class ConfigureAiChat : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {

            services.AddPlugin(new ChatFeature {
                // RequireAuth = false, // open access, runs as the "default" user
                RequireAuth = true,
                ToolsConfig =
                {
                    EnableCodeExecution = true,
                    EnableFilesystemTools = true,
                    AllowedDirectories =
                    {
                        Path.Combine(context.HostingEnvironment.ContentRootPath, "App_Data", "chat")
                    },
                }
            });

            services.ConfigurePlugin<MetadataFeature>(feature => {
                feature.AddPluginLink("/chat", "AI Chat");
            });
       });
}
