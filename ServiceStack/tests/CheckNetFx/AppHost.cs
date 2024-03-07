using System.Collections.Generic;
using Funq;
using ServiceStack;
using MyApp.ServiceInterface;
using ServiceStack.Auth;
using ServiceStack.FluentValidation;

namespace MyApp;

//VS.NET Template Info: https://servicestack.net/vs-templates/EmptyAspNet
public class AppHost : AppHostBase
{
    /// <summary>
    /// Base constructor requires a Name and Assembly where web service implementation is located
    /// </summary>
    public AppHost()
        : base("MyApp", typeof(MyServices).Assembly) {}

    /// <summary>
    /// Application specific configuration
    /// This method should initialize any IoC resources utilized by your web service classes.
    /// </summary>
    public override void Configure(Container container)
    {
        container.AddSingleton<IAuthRepository>(new InMemoryAuthRepository());

        var appSettings = AppSettings;
        Plugins.Add(new AuthFeature(() => new CustomUserSession(),
        [
            //new BasicAuthProvider(), //Sign-in with HTTP Basic Auth
            new JwtAuthProvider(appSettings)
            {
                AuthKey = AesUtils.CreateKey(),
                RequireSecureConnection = false,
            },
            new CredentialsAuthProvider(), //HTML Form post of UserName/Password credentials
            // new TwitterAuthProvider(appSettings),
            // new GithubAuthProvider(appSettings),
            // new GoogleAuthProvider(appSettings),
            // new FacebookAuthProvider(appSettings),
            // new MicrosoftGraphAuthProvider(appSettings),
        ]));

        Plugins.Add(new RegistrationFeature());

        Plugins.Add(new AdminUsersFeature());
        ConfigurePlugin<PredefinedRoutesFeature>(feature => feature.JsonApiRoute = null);

        //override the default registration validation with your own custom implementation
        RegisterAs<CustomRegistrationValidator, IValidator<Register>>();

        var authRepo = TryResolve<IAuthRepository>();

        var newAdmin = new UserAuth { Email = "admin@email.com", DisplayName = "Admin User" };
        var user = authRepo.CreateUserAuth(newAdmin, "p@55wOrd");
        authRepo.AssignRoles(user, new List<string> { "Admin" });
    }
}

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
