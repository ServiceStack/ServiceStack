using ServiceStack;
using ServiceStack.Api.OpenApi;

namespace CheckWebCore
{
    /// <summary>
    /// Run after AppHost.Configure()
    /// </summary>
    public class ConfigureOpenApi : IPostConfigureAppHost
    {
        public void Configure(IAppHost appHost)
        {
            appHost.Plugins.Add(new OpenApiFeature
            {
                UseBearerSecurity = true,
            });
        }
    }
}