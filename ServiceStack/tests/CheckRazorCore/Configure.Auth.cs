using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.FluentValidation;

[assembly: HostingStartup(typeof(CheckWebCore.ConfigureAuth))]

namespace CheckWebCore;

public class CustomUserSession : AuthUserSession {}
    
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

/// <summary>
/// Run before AppHost.Configure()
/// </summary>
public class ConfigureAuth : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            services.AddSingleton<IAuthRepository>(new InMemoryAuthRepository());
        })
        .ConfigureAppHost(appHost =>
        {
            var appSettings = appHost.AppSettings;
            appHost.Plugins.Add(new AuthFeature(() => new CustomUserSession(),
            [
                //new BasicAuthProvider(), //Sign-in with HTTP Basic Auth
                new JwtAuthProvider(appSettings)
                {
                    AuthKey = AesUtils.CreateKey(),
                    RequireSecureConnection = false,
                }, 
                new CredentialsAuthProvider(), //HTML Form post of UserName/Password credentials
                new AppleAuthProvider(appSettings), 
                new TwitterAuthProvider(appSettings),
                new GithubAuthProvider(appSettings), 
                new GoogleAuthProvider(appSettings),
                new FacebookAuthProvider(appSettings),
                new MicrosoftGraphAuthProvider(appSettings),
                // new LinkedInAuthProvider(AppSettings), 
            ]));

            appHost.Plugins.Add(new RegistrationFeature());

            //override the default registration validation with your own custom implementation
            appHost.RegisterAs<CustomRegistrationValidator, IValidator<Register>>();

            var authRepo = appHost.TryResolve<IAuthRepository>();

            var newAdmin = new UserAuth {Email = "admin@email.com", DisplayName = "Admin User"};
            var user = authRepo.CreateUserAuth(newAdmin, "p@55wOrd");
            authRepo.AssignRoles(user, new List<string> {"Admin"});
        });
}
