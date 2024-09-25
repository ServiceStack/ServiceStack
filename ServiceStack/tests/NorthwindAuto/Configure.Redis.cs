using ServiceStack;
using ServiceStack.Redis;

// [assembly: HostingStartup(typeof(MyApp.ConfigureRedis))]

namespace MyApp;

public class ConfigureRedis : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            services.AddSingleton<IRedisClientsManager>(
                new RedisManagerPool(context.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));
            
            services.AddPlugin(new AdminRedisFeature {
                ModifiableConnection = true
            });
        });
}
