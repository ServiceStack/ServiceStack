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
                options.SessionFactory = () => new CustomUserSession();
                options.CredentialsAuth();
                options.JwtAuth(x => {
                    x.ExtendRefreshTokenExpiryAfterUsage = TimeSpan.FromDays(90);
                });
            })));
        });
}
