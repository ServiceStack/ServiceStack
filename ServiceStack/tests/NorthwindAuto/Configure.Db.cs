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
        .ConfigureServices((context, services) => services.AddSingleton<IDbConnectionFactory>(
            new OrmLiteConnectionFactory(
                context.Configuration.GetConnectionString("DefaultConnection") ?? ":memory:",
                SqliteDialect.Provider)))
        .ConfigureAppHost(appHost =>
        {
            // Create non-existing Table and add Seed Data Example
            using var db = appHost.Resolve<IDbConnectionFactory>().Open();
            db.SeedBookings();
            db.SeedPlayers();

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