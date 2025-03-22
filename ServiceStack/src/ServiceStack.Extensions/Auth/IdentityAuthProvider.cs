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

public abstract class IdentityAuthProvider<TUser,TRole,TKey> : IdentityAuthProvider
    where TUser : IdentityUser<TKey>, new()
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
{
    protected IdentityAuthProvider() { }
    protected IdentityAuthProvider(IAppSettings? appSettings, string authRealm, string authProvider)
        : base(appSettings, authRealm, authProvider) { }
    
    public IdentityAuthContext<TUser, TRole, TKey> Context => IdentityAuth.Instance<TUser,TRole,TKey>()
        ?? throw new NotSupportedException("IdentityAuth is not configured");

    public IdentityAuthContextManager<TUser, TRole, TKey> Manager => IdentityAuth.Manager as IdentityAuthContextManager<TUser, TRole, TKey>
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
