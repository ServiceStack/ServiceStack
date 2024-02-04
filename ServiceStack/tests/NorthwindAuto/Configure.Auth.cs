using MyApp.Data;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Html;

[assembly: HostingStartup(typeof(MyApp.ConfigureAuth))]

namespace MyApp;

public class ConfigureAuth : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            services.AddPlugin(new AuthFeature(IdentityAuth.For<ApplicationUser>(options => {
                options.SessionFactory = () => new CustomUserSession();
                options.CredentialsAuth();
                options.JwtAuth();
                options.BasicAuth();
                
                options.AdminUsersFeature(feature =>
                {
                    feature.QueryIdentityUserProperties =
                    [
                        nameof(ApplicationUser.Id),
                        nameof(ApplicationUser.DisplayName),
                        nameof(ApplicationUser.Email),
                        nameof(ApplicationUser.UserName),
                        nameof(ApplicationUser.LockoutEnd),
                    ];
                    feature.DefaultOrderBy = nameof(ApplicationUser.DisplayName);
                    feature.SearchUsersFilter = (q, query) =>
                    {
                        var queryUpper = query.ToUpper();
                        return q.Where(x =>
                            x.DisplayName!.Contains(query) ||
                            x.Id.Contains(queryUpper) ||
                            x.NormalizedUserName!.Contains(queryUpper) ||
                            x.NormalizedEmail!.Contains(queryUpper));
                    };
                    feature.FormLayout =
                    [
                        Input.For<ApplicationUser>(x => x.UserName, c => c.FieldsPerRow(2)),
                        Input.For<ApplicationUser>(x => x.Email, c => { 
                            c.Type = Input.Types.Email;
                            c.FieldsPerRow(2); 
                        }),
                        Input.For<ApplicationUser>(x => x.FirstName, c => c.FieldsPerRow(2)),
                        Input.For<ApplicationUser>(x => x.LastName, c => c.FieldsPerRow(2)),
                        Input.For<ApplicationUser>(x => x.DisplayName, c => c.FieldsPerRow(2)),
                        Input.For<ApplicationUser>(x => x.PhoneNumber, c =>
                        {
                            c.Type = Input.Types.Tel;
                            c.FieldsPerRow(2); 
                        }),
                    ];
                });
            })));
        });
}
