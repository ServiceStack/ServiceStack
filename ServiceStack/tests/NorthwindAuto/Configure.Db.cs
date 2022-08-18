using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.VirtualPath;
using TalentBlazor;
using TalentBlazor.ServiceModel;

[assembly: HostingStartup(typeof(MyApp.ConfigureDb))]

namespace MyApp;

public class ConfigureDb : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) =>
        {
            var dbFactory = new OrmLiteConnectionFactory(
                context.Configuration.GetConnectionString("DefaultConnection") ?? ":memory:",
                SqliteDialect.Provider);
            services.AddSingleton<IDbConnectionFactory>(dbFactory);
            dbFactory.RegisterConnection("chinook", 
                context.Configuration.GetConnectionString("ChinookConnection"), SqliteDialect.Provider);
            
            // Add support for dynamically generated db rules
            services.AddSingleton<IValidationSource>(c => 
                new OrmLiteValidationSource(c.Resolve<IDbConnectionFactory>(), HostContext.LocalCache));            
        })
        .ConfigureAppHost(appHost =>
        {
            // Create non-existing Table and add Seed Data Example
            using var db = appHost.Resolve<IDbConnectionFactory>().Open();

            appHost.AddVirtualFileSources.Add(new FileSystemMapping("profiles", AppHost.ProfilesDir));
            db.DropTable<Contact>();
            db.DropTable<Job>();
            db.DropTable<JobApplication>();
            db.DropTable<JobApplicationEvent>();
            db.DropTable<PhoneScreen>();
            db.DropTable<Interview>();
            db.DropTable<JobApplicationAttachment>();
            db.DropTable<JobApplicationComment>();
            db.SeedTalent(profilesDir:AppHost.ProfilesDir);
            db.SeedAttachments(appHost, sourceDir:AppHost.TalentBlazorAppDataDir);
            
            db.DropAndCreateTable<FileSystemItem>();
            db.DropAndCreateTable<FileSystemFile>();
            
            appHost.Resolve<IValidationSource>().InitSchema();
            
            appHost.Plugins.Add(new AdminDatabaseFeature {
                DatabasesFilter = dbs => {
                    dbs.Each(db => {
                        if (db.Name == "main")
                        {
                            db.Alias = "Northwind";
                            db.Schemas[0].Alias = "Traders";
                        }
                        else if (db.Name == "chinook")
                        {
                            db.Alias = "Chinook";
                            db.Schemas[0].Alias = "Music";
                        }
                    });
                },
                // SchemasFilter = schemas => {
                //     schemas.Add(new SchemaInfo {
                //         Name = "test",
                //         Tables = new() { "Test" },
                //     });
                // },
            });
        });
}

public static class ConfigureDbUtils
{
    public static T WithAudit<T>(this T row, string by, DateTime? date = null) where T : AuditBase
    {
        var useDate = date ?? DateTime.Now;
        row.CreatedBy = by;
        row.CreatedDate = useDate;
        row.ModifiedBy = by;
        row.ModifiedDate = useDate;
        return row;
    }
}