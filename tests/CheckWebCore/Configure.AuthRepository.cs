using System.Collections.Generic;
using ServiceStack;
using ServiceStack.Auth;

namespace CheckWebCore
{
    /// <summary>
    /// Run after AppHost.Configure()
    /// </summary>
    public class ConfigureAuthRepository : IPostConfigureAppHost
    {
        public void Configure(IAppHost appHost)
        {
            var userRep = new InMemoryAuthRepository();
            appHost.Register<IAuthRepository>(userRep);

            var authRepo = userRep;

            var newAdmin = new UserAuth {Email = "test@test.com"};
            var user = authRepo.CreateUserAuth(newAdmin, "test");
            authRepo.AssignRoles(user, new List<string> {"Admin"});
        }
    }
}