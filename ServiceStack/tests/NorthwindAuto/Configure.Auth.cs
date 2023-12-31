using ServiceStack;
using ServiceStack.Admin;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.FluentValidation;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(MyApp.ConfigureAuth))]

namespace MyApp;

// Add any additional metadata properties you want to store in the Users Typed Session
public class CustomUserSession : AuthUserSession
{
}

// Custom Validator to add custom validators to built-in /register Service requiring DisplayName and ConfirmPassword
public class CustomRegistrationValidator : RegistrationValidator
{
    public CustomRegistrationValidator()
    {
        RuleSet(ApplyTo.Post, () =>
        {
            RuleFor(x => x.DisplayName).NotEmpty();
            RuleFor(x => x.ConfirmPassword).NotEmpty();
        });
    }
}

/**/
public class ConfigureAuth : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        //.ConfigureServices(services => services.AddSingleton<ICacheClient>(new MemoryCacheClient()))
        .ConfigureAppHost(appHost =>
        {
            var appSettings = appHost.AppSettings;
            appHost.Plugins.Add(new AuthFeature(() => new CustomUserSession(), [
                new CredentialsAuthProvider(appSettings),
                    new JwtAuthProvider(appSettings) {
                        AuthKeyBase64 = appSettings.GetString("AuthKeyBase64") ?? "cARl12kvS/Ra4moVBIaVsrWwTpXYuZ0mZf/gNLUhDW5=",
                    },
                    new BasicAuthProvider(appSettings),
                    new ApiKeyAuthProvider(appSettings),
                    //new CustomCredentialsProvider(appSettings),
                    
                    new FacebookAuthProvider(appSettings),
                    new GoogleAuthProvider(appSettings),
                    new MicrosoftGraphAuthProvider(appSettings)
            ])
            {
                // IncludeDefaultLogin = false
                ProfileImages = new PersistentImagesHandler("/auth-profiles", Svg.GetStaticContent(Svg.Icons.Female),
                    appHost.VirtualFiles, "/App_Data/auth-profiles"),
                // OnAfterInit = {
                //     feature => appHost.AddToAppMetadata(meta => {
                //         meta.Plugins.Auth.AuthProviders.RemoveAll(x => x.Name == "credentials");
                //     })
                // }
            });
            
            // appHost.GetPlugin<MetadataFeature>().AfterAppMetadataFilters.Add((req,meta) =>
            //     meta.Plugins.Auth.AuthProviders.RemoveAll(x => x.Name == "credentials"));

            appHost.Plugins.Add(new RegistrationFeature()); //Enable /register Service

            //override the default registration validation with your own custom implementation
            appHost.RegisterAs<CustomRegistrationValidator, IValidator<Register>>();
        });
}

public class CustomCredentialsProvider : CredentialsAuthProvider
{
    public CustomCredentialsProvider(IAppSettings appSettings)
        : base(appSettings, "custom")
    {
        Label = "Alt Auth";
    }
}

/*
// Call QueryApiKeys to view API Keys
public class ConfigureAuth : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureAppHost(appHost =>
        {
            appHost.Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                new IAuthProvider[] {
                    new ApiKeyAuthProvider(appHost.AppSettings)
                }));
        }, afterAppHostInit: appHost => {
            var authProvider = (ApiKeyAuthProvider)
                AuthenticateService.GetAuthProvider(ApiKeyAuthProvider.Name);
            
            using var db = appHost.TryResolve<IDbConnectionFactory>().Open();
            var userWithKeysIds = db.Column<string>(db.From<ApiKey>()
                .SelectDistinct(x => x.UserAuthId)).Map(int.Parse);

            var userIdsMissingKeys = db.Column<string>(db.From<AppUser>()
                .Where(x => userWithKeysIds.Count == 0 || !userWithKeysIds.Contains(x.Id))
                .Select(x => x.Id));

            var authRepo = (IManageApiKeys)appHost.TryResolve<IAuthRepository>();
            foreach (var userId in userIdsMissingKeys)
            {
                var apiKeys = authProvider.GenerateNewApiKeys(userId);
                authRepo.StoreAll(apiKeys);
            }
        });
}
*/