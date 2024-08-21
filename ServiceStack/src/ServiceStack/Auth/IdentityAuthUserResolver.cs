#if NETCORE

#nullable enable

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public class IdentityAuthUserResolver(IIdentityAuthContextManager manager, IIdentityAuthContext authCtx) : IUserResolver
{
    public async Task<ClaimsPrincipal?> CreateClaimsPrincipalAsync(IRequest req, string userId, CancellationToken token=default)
    {
        var user = await manager.CreateClaimsPrincipalAsync(userId, req);
        return user;
    }

    public async Task<IAuthSession?> CreateAuthSessionAsync(IRequest req, ClaimsPrincipal user, CancellationToken token=default)
    {
        var authProvider = req.GetService<IIdentityApplicationAuthProvider>()
            ?? HostContext.AppHost.GetApplicationServices().GetService<IIdentityApplicationAuthProvider>();
        if (authProvider != null)
        {
            var session = authCtx.SessionFactory();
            await authProvider.PopulateSessionAsync(req, session, user);
            return session;
        }
        return null;
    }
}

public class ServiceStackAuthUserResolver(AuthFeature feature) : IUserResolver
{
    private NetCoreIdentityAuthProvider GetAuthProvider()
    {
        return feature.AuthProviders
               .FirstOrDefault(x => x is NetCoreIdentityAuthProvider) as NetCoreIdentityAuthProvider
           ?? new NetCoreIdentityAuthProvider(HostContext.AppSettings);
    }

    public async Task<ClaimsPrincipal?> CreateClaimsPrincipalAsync(IRequest req, string userId, CancellationToken token=default)
    {
        var session = await CreateSessionFromUserIdAsync(req, userId, token);
        if (session != null)
        {
            var authProvider = GetAuthProvider();
            var principal = await authProvider.ConvertSessionToPrincipalAsync(req, session, token).ConfigAwait();
            return principal;
        }
        return null;
    }

    private static async Task<IAuthSession?> CreateSessionFromUserIdAsync(IRequest req, string userId, CancellationToken token)
    {
        var sessionId = HostContext.AppHost.CreateSessionId();
        var newSession = SessionFeature.CreateNewSession(req, sessionId);
        var session = HostContext.AppHost.OnSessionFilter(req, newSession, sessionId) ?? newSession;

        var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(req);
        await using (authRepo as IAsyncDisposable)
        {
            var userAuth = await authRepo.GetUserAuthAsync(userId, token: token);
            if (userAuth == null)
                return null;
            await session.PopulateSessionAsync(userAuth, authRepo, token: token);
            return session;
        }
    }

    public async Task<IAuthSession?> CreateAuthSessionAsync(IRequest req, ClaimsPrincipal user, CancellationToken token=default)
    {
        var authProvider = GetAuthProvider();
        var session = await authProvider.ConvertPrincipalToSessionAsync(req, user, token);
        return session;
    }
}

#endif