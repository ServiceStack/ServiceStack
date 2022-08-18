using MyApp;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite.Tests.Migrations;

[assembly: HostingStartup(typeof(NorthwindAuto.ConfigureDbMigrations))]

namespace NorthwindAuto;

public class ConfigureDbMigrations : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddSingleton(c => new Migrator(c.Resolve<IDbConnectionFactory>(), typeof(Migration1000).Assembly));
        })
        .ConfigureAppHost(appHost => {
            appHost.RegisterAppTask("migrate", args => 
                appHost.Resolve<Migrator>().Run());
            appHost.RegisterAppTask("migrate.revert", args => 
                appHost.Resolve<Migrator>().Revert(args[0]));
        });
}
