using System.Data;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.IO;
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
            services.AddOrmLite(options => 
                options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection"), dialect => {
                    dialect.EnableWal = true;
                })
            )
            .AddSqlite("chinook", 
                context.Configuration.GetConnectionString("ChinookConnection"));
            
            // Add support for dynamically generated db rules
            services.AddSingleton<IValidationSource>(c => 
                new OrmLiteValidationSource(c.GetRequiredService<IDbConnectionFactory>(), HostContext.LocalCache));            
            
            services.AddPlugin(new AdminDatabaseFeature {
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
        })
        .ConfigureAppHost(appHost =>
        {
            // Create non-existing Table and add Seed Data Example
            appHost.AddVirtualFileSources.Add(new FileSystemMapping("profiles", AppHost.ProfilesDir));
            
            appHost.Resolve<IValidationSource>().InitSchema();
        });

    public static void SeedData(IDbConnection db)
    {
        //using var db = appHost.Resolve<IDbConnectionFactory>().Open();
        db.DropTable<JobApplicationComment>();
        db.DropTable<JobApplicationAttachment>();
        db.DropTable<JobOffer>();
        db.DropTable<Interview>();
        db.DropTable<PhoneScreen>();
        db.DropTable<JobApplicationEvent>();
        db.DropTable<JobApplication>();
        db.DropTable<Job>();
        db.DropTable<Contact>();
            
        db.SeedTalent(profilesDir:AppHost.ProfilesDir);
        db.SeedAttachments(sourceDir:AppHost.TalentBlazorSeedDataDir);
            
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
    }
    
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