#if NETCORE

#nullable enable

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

public class IdentityAuthUserResolver(IIdentityAuthContextManager manager, IIdentityAuthContext authCtx, IServiceProvider services) : IUserResolver
{
    public async Task<ClaimsPrincipal?> CreateClaimsPrincipalAsync(IRequest req, string userId, CancellationToken token=default)
    {
        var user = await manager.CreateClaimsPrincipalAsync(userId, req);
        return user;
    }

    public async Task<IAuthSession?> CreateAuthSessionAsync(IRequest req, ClaimsPrincipal user, CancellationToken token=default)
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

    public async Task<List<Dictionary<string, object>>> GetUsersByIdsAsync(IRequest req, List<string> ids, CancellationToken token = default)
    {
        return await manager.GetUsersByIdsAsync(ids, req).ConfigAwait();
    }
}

public class ServiceStackAuthUserResolver(NetCoreIdentityAuthProvider authProvider) : IUserResolver
{
    public async Task<ClaimsPrincipal?> CreateClaimsPrincipalAsync(IRequest req, string userId, CancellationToken token=default)
    {
        var session = await CreateSessionFromUserIdAsync(req, userId, token);
        if (session != null)
        {
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
        var session = await authProvider.ConvertPrincipalToSessionAsync(req, user, token);
        return session;
    }

    public async Task<List<Dictionary<string, object>>> GetUsersByIdsAsync(IRequest req, List<string> ids, CancellationToken token = default)
    {
        var ret = new List<Dictionary<string, object>>();
        var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(req);
        await using (authRepo as IAsyncDisposable)
        {
            if (authRepo is IQueryUserAuthAsync queryUsers)
            {
                var allUsers = await queryUsers.GetUserAuthsAsync(orderBy: nameof(IUserAuth.Id), token: token).ConfigAwait();
                var allUsersMap = new Dictionary<int, IUserAuth>();
                foreach (var user in allUsers)
                {
                    allUsersMap[user.Id] = user;
                }
                foreach (var id in ids)
                {
                    if (int.TryParse(id, out var userId) && allUsersMap.TryGetValue(userId, out var userAuth))
                    {
                        ret.Add(userAuth.ToObjectDictionary());
                    }
                }
            }
        }
        return ret;
    }
}

#endif