using ServiceStack;
using ServiceStack.AI;

[assembly: HostingStartup(typeof(MyApp.ConfigureAiChat))]

namespace MyApp;

/// <summary>
/// AI Chat using ASP.NET Identity Auth: the signed-in username partitions all chat data and
/// /v1/chat/completions additionally accepts Bearer API Keys (this host registers ApiKeysFeature).
/// Chat history is persisted with OrmLite to the host's registered IDbConnectionFactory.
/// </summary>
public class ConfigureAiChat : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddPlugin(new ChatFeature
            {
                RequireAuth = true,
                AuthType = ChatAuthType.OAuth,   // sign in with Identity Auth
                SignInUrl = "/Account/Login",

                // only enable providers we have API Keys for (default: all enabled in llms.json)
                EnableProviders = [
                    "groq",
                    "google",
                    "anthropic",
                    "openai",
                    "openrouter",
                ],

                // Server-side code execution + filesystem tools are opt-in, e.g:
                // ToolsConfig = new() {
                //     EnableCodeExecution = true,
                //     EnableFilesystemTools = true,
                //     AllowedDirectories = [Path.Combine(Path.GetTempPath(), "chat-workspace")],
                // },
            });

            services.ConfigurePlugin<MetadataFeature>(feature => {
                feature.AddPluginLink("/chat", "AI Chat");
            });
       });
}
