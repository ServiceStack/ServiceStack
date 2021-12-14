using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyApp;
using ServiceStack;
using ServiceStack.Admin;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;

namespace CheckWebCore
{
    /// <summary>
    /// Run before AppHost.Configure()
    /// </summary>
    public class ConfigureAuth : IConfigureAppHost
    {
        private IConfiguration configuration;
        public ConfigureAuth(IConfiguration configuration) => this.configuration = configuration;

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
                    new CredentialsAuthProvider {
                        SkipPasswordVerificationForInProcessRequests = true,
                    }, //HTML Form post of UserName/Password credentials
                    new ApiKeyAuthProvider(AppSettings),
                    new TwitterAuthProvider(AppSettings),
                    new GithubAuthProvider(AppSettings), 
                    new GoogleAuthProvider(AppSettings),
                    new FacebookAuthProvider(AppSettings),
                    new MicrosoftGraphAuthProvider(AppSettings), 
//                    new LinkedInAuthProvider(AppSettings), 
                }) {
                HtmlRedirect = "/validation/server/login",
                HtmlRedirectAccessDenied = "/forbidden",
            });

            appHost.Plugins.Add(new RegistrationFeature());
            
            appHost.Plugins.Add(new AdminUsersFeature());

            //override the default registration validation with your own custom implementation
            appHost.RegisterAs<CustomRegistrationValidator, IValidator<Register>>();

            var authRepo = appHost.TryResolve<IAuthRepository>();

            var newAdmin = new AppUser {Email = "admin@email.com", DisplayName = "Admin User"};
            var user = authRepo.CreateUserAuth(newAdmin, "p@55wOrd");
            authRepo.AssignRoles(user, new List<string> {"Admin"});

            authRepo.CreateUserAuth(new AppUser {UserName = "test", DisplayName = "Test"}, "test");
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
    
    [Route("/apikeyonly")]
    public class ApiKeyOnly : IReturn<string> {}

    [Authenticate(AuthenticateService.ApiKeyProvider)]
    public class ApiKeyAuthServices : Service
    {
        public object Any(ApiKeyOnly request) => "OK";
    }
    
    [Route("/jwtonly")]
    public class JwtOnly : IReturn<string> {}

    [Authenticate(AuthenticateService.JwtProvider)]
    public class JwtAuthServices : Service
    {
        public object Any(JwtOnly request) => "OK";
    }

}