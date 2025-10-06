using Microsoft.OpenApi.Models;
using MyApp.Data;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Html;
using ServiceStack.OrmLite;
using Swashbuckle.AspNetCore.SwaggerGen;

[assembly: HostingStartup(typeof(MyApp.ConfigureAuth))]

namespace MyApp;

public class ConfigureAuth : IHostingStartup
{
    public static List<ApiKeysFeature.ApiKey> ApiKeys = [
        new() { Key = "ak-4357089af5a446cab0fdc44830e03617", UserId = "CB923F42-AE84-4B77-B2A8-5C6E71F29DF4", UserName = "Admin", Scopes = [RoleNames.Admin] },
        new() { Key = "ak-1359a079e98841a2a0c52419433d207f", UserId = "A8BBBFDB-1DA6-44E6-96D9-93995A7CBCEF", UserName = "System" },
    ];

    public void ConfigureApiKeys(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddPlugin(new AuthFeature([
                new ApiKeyCredentialsProvider(),
                new AuthSecretAuthProvider(),
            ]));
            services.AddPlugin(new SessionFeature());
            services.AddPlugin(new ApiKeysFeature
            {
                // Hide = [
                //     nameof(ApiKeysFeature.ApiKey.RestrictTo),
                //     nameof(ApiKeysFeature.ApiKey.Notes),
                // ],
            });
        })
        .ConfigureAppHost(appHost =>
        {
            var apiKeysFeature = appHost.GetPlugin<ApiKeysFeature>();
            using var db = apiKeysFeature.OpenDb();
            apiKeysFeature.InitSchema(db);
            if (db.Count<ApiKeysFeature.ApiKey>() == 0)
            {
                apiKeysFeature.InsertAll(db, ApiKeys);
            }
        });
    
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            services.AddPlugin(new AuthFeature(IdentityAuth.For<ApplicationUser>(options => {
                options.SessionFactory = () => new CustomUserSession();
                options.CredentialsAuth();
                // options.JwtAuth();
                options.BasicAuth();
                // options.ApplicationAuth(feature => 
                //     feature.PriorityMapClaimsToSession.Clear());
                
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

            services.AddPlugin(new ApiKeysFeature
            {
                // UseDb = () => HostContext.AppHost.GetDbConnection("namedConnection"),
                Scopes = [
                    RoleNames.Admin,
                    "todo:read",
                    "todo:write",
                    "bookings:read",
                    "bookings:write",
                ],
                Features = [
                    "Tracking",
                ],
                // Hide = [
                //     nameof(ApiKeysFeature.ApiKey.RestrictTo),
                //     nameof(ApiKeysFeature.ApiKey.Notes),
                // ],
            });
        })
        .ConfigureAppHost(appHost =>
        {
            var apiKeysFeature = appHost.GetPlugin<ApiKeysFeature>();
            using var db = apiKeysFeature.OpenDb();
            apiKeysFeature.InitSchema(db);
            if (db.Count<ApiKeysFeature.ApiKey>() == 0)
            {
                apiKeysFeature.InsertAll(db, ApiKeys);
            }
        });
}
