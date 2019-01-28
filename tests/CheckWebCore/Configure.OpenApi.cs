using ServiceStack;
using ServiceStack.Api.OpenApi;

namespace CheckWebCore
{
    /// <summary>
    /// Run before AppHost.Configure()
    /// </summary>
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