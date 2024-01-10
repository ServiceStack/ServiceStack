using Microsoft.AspNetCore.Identity;
using ServiceStack.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Auth;

public abstract class IdentityAuthProvider : AuthProvider
{
    protected IdentityAuthProvider() { }
    protected IdentityAuthProvider(IAppSettings? appSettings, string authRealm, string authProvider)
        : base(appSettings, authRealm, authProvider) { }
}

public abstract class IdentityAuthProvider<TUser,TKey> : IdentityAuthProvider
    where TKey : IEquatable<TKey>
    where TUser : IdentityUser<TKey>, new()
{
    protected IdentityAuthProvider() { }
    protected IdentityAuthProvider(IAppSettings? appSettings, string authRealm, string authProvider)
        : base(appSettings, authRealm, authProvider) { }
    
    public IdentityAuthContext<TUser, TKey> Context => IdentityAuth.Instance<TUser,TKey>()
        ?? throw new NotSupportedException("IdentityAuth is not configured");

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
