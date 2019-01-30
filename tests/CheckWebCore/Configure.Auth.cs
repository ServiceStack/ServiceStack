using System.Collections.Generic;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;

namespace CheckWebCore
{
    /// <summary>
    /// Run before AppHost.Configure()
    /// </summary>
    public class ConfigureAuth : IConfigureAppHost
    {
        public void Configure(IAppHost appHost)
        {
            var AppSettings = appHost.AppSettings;
            appHost.Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                new IAuthProvider[] {
                    //new BasicAuthProvider(), //Sign-in with HTTP Basic Auth
                    new JwtAuthProvider(AppSettings)
                    {
                        AuthKey = AesUtils.CreateKey(),
                        RequireSecureConnection = false,
                    }, 
                    new CredentialsAuthProvider(), //HTML Form post of UserName/Password credentials
                    new FacebookAuthProvider(AppSettings),
                    new TwitterAuthProvider(AppSettings),
                }));

            appHost.Plugins.Add(new RegistrationFeature());

            var userRep = new InMemoryAuthRepository();
            appHost.Register<IAuthRepository>(userRep);

            var authRepo = userRep;

            var newAdmin = new UserAuth {Email = "admin@email.com", DisplayName = "Admin User"};
            var user = authRepo.CreateUserAuth(newAdmin, "p@55wOrd");
            authRepo.AssignRoles(user, new List<string> {"Admin"});
        }
    }
}