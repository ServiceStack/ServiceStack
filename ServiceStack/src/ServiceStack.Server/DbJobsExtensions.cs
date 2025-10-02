#if NET8_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Data;

namespace ServiceStack;

public static class DbJobsExtensions
{
    // Admin UI requires AutoQuery functionality
    public static void RegisterAutoQueryDbIfNotExists(this AutoQueryFeature feature)
    {
        ServiceStackHost.GlobalAfterConfigureServices.Add(services =>
        {
            if (!services.Exists<IAutoQueryDb>())
            {
                services.AddSingleton<IAutoQueryDb>(c => 
                    feature.CreateAutoQueryDb(c.GetService<IDbConnectionFactory>()));
            }
        });
    }    
}
#endif