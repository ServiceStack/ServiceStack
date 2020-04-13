using ServiceStack;
using ServiceStack.Api.OpenApi;

namespace CheckWebCore
{
    /// <summary>
    /// Run after AppHost.Configure()
    /// </summary>
    [Priority(1)]
    public class ConfigureOpenApi : IConfigureAppHost
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