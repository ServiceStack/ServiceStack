using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Data;

namespace ServiceStack.Jobs;

public static class SqliteDataExtensions
{
    // Admin UI requires AutoQuery functionality
    public static void RegisterAutoQueryDbIfNotExists(this AutoQueryFeature feature)
    {
        ServiceStackHost.GlobalAfterConfigureServices.Add(c =>
        {
            if (!c.Exists<IAutoQueryDb>())
            {
                c.AddSingleton<IAutoQueryDb>(c => 
                    feature.CreateAutoQueryDb(c.GetService<IDbConnectionFactory>()));
            }
        });
    }
}
