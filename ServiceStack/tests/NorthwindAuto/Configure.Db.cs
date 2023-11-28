using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Auth;
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
            // var dbFactory = new OrmLiteConnectionFactory(
            //     "Server=localhost;Database=ServiceStackDb;UID=root;Password=test;SslMode=none",
            //     MySqlDialect.Provider);
            
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
            db.DropTable<JobApplicationComment>();
            db.DropTable<JobApplicationAttachment>();
            db.DropTable<JobOffer>();
            db.DropTable<Interview>();
            db.DropTable<PhoneScreen>();
            db.DropTable<JobApplicationEvent>();
            db.DropTable<JobApplication>();
            db.DropTable<Job>();
            db.DropTable<Contact>();

            ConfigureAuthRepository.RecreateUsers(appHost.Resolve<IAuthRepository>(), db);
            
            db.SeedTalent(profilesDir:AppHost.ProfilesDir);
            db.SeedAttachments(appHost, sourceDir:AppHost.TalentBlazorSeedDataDir);
            
            db.DropTable<FileSystemFile>();
            db.DropTable<FileSystemItem>();
            db.CreateTable<FileSystemItem>();
            db.CreateTable<FileSystemFile>();
            
            db.DropTable<Todo>();
            
            if (db.CreateTableIfNotExists<Todo>()) 
            {
                db.Insert(new Todo { Text = "Learn" });
                db.Insert(new Todo { Text = "AutoQuery" });
            }
            
            appHost.Resolve<IValidationSource>().InitSchema();
            
            appHost.Plugins.Add(new AdminDatabaseFeature {
                QueryLimit = 100,
                DatabasesFilter = dbs => {
                    foreach (var db in dbs) 
                    {
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
                    }
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