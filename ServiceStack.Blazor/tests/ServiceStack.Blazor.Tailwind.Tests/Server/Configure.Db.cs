using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(MyApp.ConfigureDb))]

namespace MyApp;

public class ConfigureDb : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context,services) => services.AddSingleton<IDbConnectionFactory>(new OrmLiteConnectionFactory(
            context.Configuration.GetConnectionString("DefaultConnection") ?? "App_Data/db.sqlite",
            SqliteDialect.Provider)))
        .ConfigureAppHost(appHost => {
            // Create non-existing Table and add Seed Data Example
            using var db = appHost.Resolve<IDbConnectionFactory>().Open();                
        });
}
