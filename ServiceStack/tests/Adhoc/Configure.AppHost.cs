using ServiceStack;
using ServiceStack.AI;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(MyApp.AppHost))]

namespace MyApp;

public class AppHost() : AppHostBase("MyApp"), IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            // Configure ASP.NET Core IOC Dependencies
            
            services.AddPlugin(new AuthFeature(() => new AuthUserSession(), [
                new BasicAuthProvider() //Sign-in with HTTP Basic Auth
            ]));

            //SqlServer2012Dialect.Provider.GetStringConverter().UseUnicode = true;

            services.AddSingleton<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(AppSettings.GetString("ConnectionString") ?? "App_Data/app.db", SqliteDialect.Provider));

            // services.AddSingleton<IAuthRepository>(c =>
            //     new OrmLiteAuthRepository(c.GetRequiredService<IDbConnectionFactory>()));

            services.AddSingleton<ICacheClient>(c => new OrmLiteCacheClient {
                DbFactory = c.GetRequiredService<IDbConnectionFactory>()
            });
            
            services.AddPlugin(new ApiKeysFeature());
            
            services.AddPlugin(new ChatFeature());
        });

    public override void Configure()
    {
        var services = this.GetApplicationServices();
        var apiKeysFeature = GetPlugin<ApiKeysFeature>();
        using var db = apiKeysFeature.OpenDb();
        apiKeysFeature.InitSchema(db);
            
        // const string AnonKey = "ak-200F62B274954568A70E1BAE38BB983D";
        // const string UserKey = "ak-C56C9065463A4AEEAD4D33C0DCB1FCD9";
        // const string AdminKey = "ak-CCEE28F476C2413191D62F57803297E4";
        // const string RestrictedKey = "ak-4E155BB187734A87BAADB15D69F7604F";
        // var dbFactory = services.GetRequiredService<IDbConnectionFactory>();
        // apiKeysFeature.InsertAll(db, [
        //     new() { Key = AnonKey },
        //     new() { Key = UserKey, UserId = "89C1698D-9FD1-43B1-8C8B-C76EFA65E99B", UserName = "apiuser" },
        //     new() { Key = AdminKey, UserId = "40E566F2-DD08-4432-9D9C-528B3B0CCBEE", UserName = "admin", Scopes = [RoleNames.Admin] },
        //     new() { Key = RestrictedKey, UserId = "9FDC4B8B-04AA-42AD-80AD-803FF8530EFB", UserName = "restricted", RestrictTo = [nameof(RestrictedKey)] },
        // ]);

        // Configure ServiceStack, Run custom logic after ASP.NET Core Startup
        SetConfig(new HostConfig {
        });
        
        // var authRepository = Container.Resolve<IAuthRepository>();
        // authRepository.InitSchema();
        //
        // if (authRepository.GetUserAuthByUserName("user") == null)
        // {
        //     authRepository.CreateUserAuth(new UserAuth
        //     {
        //         UserName = "user"
        //     }, "password");
        // }

        Container.Resolve<ICacheClient>().InitSchema();
    }
}
