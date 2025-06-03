using System;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.OrmLite;

namespace ServiceStack;

public static class OrmLiteServiceCollectionExtensions
{
    public static OrmLiteConfigurationBuilder AddOrmLite(this IServiceCollection services, Action<OrmLiteConfigOptions> configure)
    {
        var config = new OrmLiteConfigOptions(); 
        configure(config);

        if (config.DbFactory == null)
            throw new Exception("Primary Database connection was not configured");
        
        services.AddSingleton(config.DbFactory);
        
        return new OrmLiteConfigurationBuilder(config.DbFactory);
    }
}