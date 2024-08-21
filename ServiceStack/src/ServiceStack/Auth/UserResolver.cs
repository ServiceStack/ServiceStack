#nullable enable

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public class UserResolver(IServiceProvider services, 
    IIdentityAuthContextManager manager, IIdentityAuthContext authCtx) : IUserResolver
{
    public async Task<ClaimsPrincipal?> CreateClaimsPrincipal(IRequest req, string userId)
    {
        var user = await manager.CreateClaimsPrincipalAsync(userId, req);
        return user;
    }

    public async Task<IAuthSession?> CreateAuthSession(IRequest req, ClaimsPrincipal user)
    {
        var authProvider = req.GetService<IIdentityApplicationAuthProvider>()
                           ?? services.GetService<IIdentityApplicationAuthProvider>();
        if (authProvider != null)
        {
            var session = authCtx.SessionFactory();
            await authProvider.PopulateSessionAsync(req, session, user);
            return session;
        }
        return null;
    }
}
