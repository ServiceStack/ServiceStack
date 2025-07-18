using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Caching;
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

            services.AddSingleton<IAuthRepository>(c =>
                new OrmLiteAuthRepository(c.GetRequiredService<IDbConnectionFactory>()));

            services.AddSingleton<ICacheClient>(c => new OrmLiteCacheClient {
                DbFactory = c.GetRequiredService<IDbConnectionFactory>()
            });
            
        });

    public override void Configure()
    {
        // Configure ServiceStack, Run custom logic after ASP.NET Core Startup
        SetConfig(new HostConfig {
        });
        
        var authRepository = Container.Resolve<IAuthRepository>();

        authRepository.InitSchema();

        if (authRepository.GetUserAuthByUserName("user") == null)
        {
            authRepository.CreateUserAuth(new UserAuth
            {
                UserName = "user"
            }, "password");
        }

        Container.Resolve<ICacheClient>().InitSchema();
    }
}
