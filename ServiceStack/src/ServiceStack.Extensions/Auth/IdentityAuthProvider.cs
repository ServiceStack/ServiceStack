using Microsoft.AspNetCore.Identity;
using ServiceStack.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Auth;

public abstract class IdentityAuthProvider : AuthProvider
{
    protected IdentityAuthProvider() { }
    protected IdentityAuthProvider(IAppSettings appSettings, string authRealm, string authProvider)
        : base(appSettings, authRealm, authProvider) { }
}

public abstract class IdentityAuthProvider<TUser> : IdentityAuthProvider
    where TUser : IdentityUser
{
    protected IdentityAuthProvider() { }
    protected IdentityAuthProvider(IAppSettings appSettings, string authRealm, string authProvider)
        : base(appSettings, authRealm, authProvider) { }

    public override async Task<object> LogoutAsync(IServiceBase service, Authenticate request, CancellationToken token = default)
    {
        var user = service.Request.GetClaimsPrincipal();

        var signInManager = service.Resolve<SignInManager<TUser>>();
        if (signInManager.IsSignedIn(user))
        {
            await signInManager.SignOutAsync();
        }

        return await base.LogoutAsync(service, request, token);
    }
}
