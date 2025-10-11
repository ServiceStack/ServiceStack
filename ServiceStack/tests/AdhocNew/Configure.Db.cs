using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ServiceStack.OrmLite;
using MyApp.Data;
using ServiceStack.Data;
using ServiceStack.Text;
using ServiceStack.Web;

[assembly: HostingStartup(typeof(MyApp.ConfigureDb))]

namespace MyApp;

public class ConfigureDb : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                ?? "Server=test;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200";
            
            services.AddOrmLite(options => options.UsePostgres(connectionString, dialect => {
                    // dialect.NamingStrategy = new OrmLiteNamingStrategyBase();
                })
                .ConfigureJson(json => {
                    // json.DefaultSerializer = JsonSerializerType.ServiceStackJson;
                })
            );

            // $ dotnet ef migrations add CreateIdentitySchema
            // $ dotnet ef database update
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString
                    //, b => b.MigrationsAssembly(nameof(MyApp))
                ));
            
            // Enable built-in Database Admin UI at /admin-ui/database
            services.AddPlugin(new AdminDatabaseFeature());
        })
        .ConfigureAppHost(appHost =>
        {
            // var dbFactory = appHost.Resolve<IDbConnectionFactory>();
            // string namedConneciton = "";
            // using var db1 = dbFactory.Open(configure: db => db.WithTag("MyTag"));
            // using var db = dbFactory.Open(namedConneciton, configure: db => db.WithTag("MyTag"));
            // IHttpRequest req = null;
            // using var db3 = HostContext.AppHost.GetDbConnection(req, configure: db => db.WithTag("MyTag"));
            
            // OrmLiteConfig.BeforeExecFilter = cmd => 
            //     cmd.GetTag().Print();
            // OrmLiteConfig.AfterExecFilter = cmd =>
            // {
            //     Console.WriteLine($"[{cmd.GetTag()}] {cmd.GetElapsedTime()}");
            // };
        });
}

// Used by dotnet ef
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("", b => b.MigrationsAssembly(nameof(MyApp)));
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
