using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.VirtualPath;
using TalentBlazor;
using TalentBlazor.ServiceModel;
using MyApp.ServiceModel;

[assembly: HostingStartup(typeof(MyApp.ConfigureDb))]

namespace MyApp;

// Database can be created with "dotnet run --AppTasks=migrate"   
public class ConfigureDb : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context,services) =>
        {
            var dbFactory = new OrmLiteConnectionFactory(
                    context.Configuration.GetConnectionString("DefaultConnection") ?? "App_Data/db.sqlite",
                    SqliteDialect.Provider);
            services.AddSingleton<IDbConnectionFactory>(dbFactory);

            dbFactory.RegisterConnection("chinook",
                context.Configuration.GetConnectionString("ChinookConnection") ?? "App_Data/chinook.sqlite", SqliteDialect.Provider);
            dbFactory.RegisterConnection("northwind",
                context.Configuration.GetConnectionString("NorthwindConnection") ?? "App_Data/northwind.sqlite", SqliteDialect.Provider);

            // Add support for dynamically generated db rules
            services.AddSingleton<IValidationSource>(c =>
                new OrmLiteValidationSource(c.Resolve<IDbConnectionFactory>(), HostContext.LocalCache));

        })
        .ConfigureAppHost(appHost =>
        {
            // Create non-existing Table and add Seed Data Example
            using var db = appHost.Resolve<IDbConnectionFactory>().Open();

            //appHost.AddVirtualFileSources.Add(new FileSystemMapping("profiles", AppHost.ProfilesDir));
            appHost.Resolve<IValidationSource>().InitSchema();
        });
}
