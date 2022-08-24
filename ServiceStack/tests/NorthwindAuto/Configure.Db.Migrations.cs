using MyApp.Migrations;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(NorthwindAuto.ConfigureDbMigrations))]

namespace NorthwindAuto;

public class ConfigureDbMigrations : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddSingleton(c => new Migrator(c.Resolve<IDbConnectionFactory>(), typeof(Migration1000).Assembly));
        })
        .ConfigureAppHost(afterAppHostInit:appHost => {
            AppTasks.Register("migrate", _ => appHost.Resolve<Migrator>().Run());
            AppTasks.Register("migrate.revert", args => appHost.Resolve<Migrator>().Revert(args[0]));
            AppTasks.Run();
        });
}
