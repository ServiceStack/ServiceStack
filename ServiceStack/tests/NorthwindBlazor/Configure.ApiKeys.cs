using MyApp.Data;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(MyApp.ConfigureApiKeys))]

namespace MyApp;

public class ConfigureApiKeys : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            services.AddPlugin(new ApiKeysFeature
            {
                // Optional: Limit scope of API Key access
                Scopes = [
                ],
                // Optional: Tag API Keys with additional features
                Features = [
                ],
            });
        })
        .ConfigureAppHost(appHost =>
        {
            appHost.Metadata.ForceInclude =
            [
                typeof(QueryUserApiKeys),
                typeof(CreateUserApiKey),
                typeof(UpdateUserApiKey),
                typeof(DeleteUserApiKey),
            ];
            var apiKeysFeature = appHost.GetPlugin<ApiKeysFeature>();

            using var db = appHost.Resolve<IDbConnectionFactory>().Open();
            apiKeysFeature.InitSchema(db);
            // Optional, create API Key for specified Users
            if (apiKeysFeature.ApiKeyCount(db) == 0)
            {
                var createApiKeysFor = new [] { "admin@email.com", "manager@email.com" };
                var users = db.Select<ApplicationUser>(x => createApiKeysFor.Contains(x.UserName));
                // Example using EF
                // var scopeFactory = appHost.GetApplicationServices().GetRequiredService<IServiceScopeFactory>();
                // using var scope = scopeFactory.CreateScope();
                // using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                // var users = dbContext.Users.Where(x => createApiKeysFor.Contains(x.UserName));
                
                foreach (var user in users)
                {
                    List<string> scopes = user.UserName == "admin@email.com"
                        ? [RoleNames.Admin] 
                        : [];
                    apiKeysFeature.Insert(db, 
                        new() { Name = "Seed API Key", UserId = user.Id, UserName = user.UserName, Scopes = scopes });
                }
            }
        });
}