using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;

namespace CheckWebCore
{
    /// <summary>
    /// Run before AppHost.Configure()
    /// </summary>
    public class ConfigureAuth : IConfigureAppHost, IConfigureServices
    {
        private IConfiguration configuration;
        public ConfigureAuth(IConfiguration configuration) => this.configuration = configuration;

        public void Configure(IServiceCollection services)
        {
            services.AddSingleton<IAuthRepository>(new InMemoryAuthRepository());
        }

        public void Configure(IAppHost appHost)
        {
            var AppSettings = appHost.AppSettings;
            appHost.Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[] {
                    //new BasicAuthProvider(), //Sign-in with HTTP Basic Auth
                    new JwtAuthProvider(AppSettings)
                    {
                        AuthKey = AesUtils.CreateKey(),
                        RequireSecureConnection = false,
                    }, 
                    new CredentialsAuthProvider(), //HTML Form post of UserName/Password credentials
                    new AppleAuthProvider(AppSettings), 
                    new TwitterAuthProvider(AppSettings),
                    new GithubAuthProvider(AppSettings), 
                    new GoogleAuthProvider(AppSettings),
                    new FacebookAuthProvider(AppSettings),
                    new MicrosoftGraphAuthProvider(AppSettings),
//                    new LinkedInAuthProvider(AppSettings), 
                }));

            appHost.Plugins.Add(new RegistrationFeature());

            //override the default registration validation with your own custom implementation
            appHost.RegisterAs<CustomRegistrationValidator, IValidator<Register>>();

            var authRepo = appHost.TryResolve<IAuthRepository>();

            var newAdmin = new UserAuth {Email = "admin@email.com", DisplayName = "Admin User"};
            var user = authRepo.CreateUserAuth(newAdmin, "p@55wOrd");
            authRepo.AssignRoles(user, new List<string> {"Admin"});
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

}