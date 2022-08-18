using ServiceStack;
using ServiceStack.Redis;

[assembly: HostingStartup(typeof(NorthwindAuto.ConfigureRedis))]

namespace NorthwindAuto;

public class ConfigureRedis : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            services.AddSingleton<IRedisClientsManager>(
                new RedisManagerPool(context.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));
        })
        .ConfigureAppHost(appHost => {
            appHost.Plugins.Add(new AdminRedisFeature());
        });
}