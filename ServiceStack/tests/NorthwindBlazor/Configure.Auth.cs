using ServiceStack.Auth;
using MyApp.Data;

[assembly: HostingStartup(typeof(MyApp.ConfigureAuth))]

namespace MyApp;

public class ConfigureAuth : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            Console.WriteLine("ConfigureAuth.ConfigureServices()");
            services.AddPlugin(new AuthFeature(IdentityAuth.For<ApplicationUser>(options => {
                options.EnableCredentialsAuth = true;
                options.EnableJwtAuth = true;
                options.AuthJwt!.ExtendRefreshTokenExpiryAfterUsage = TimeSpan.FromDays(100);
                options.SessionFactory = () => new CustomUserSession();
            })));
        });
}
