using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Text;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Default IChatAuth using ASP.NET Identity Auth (ClaimsPrincipal from the authentication cookie),
/// with optional API Key support (Authorization: Bearer) when ApiKeysFeature is registered —
/// used both by the stock 'apikey' SignIn UI and programmatic clients.
/// </summary>
public class IdentityChatAuth(ChatFeature feature) : IChatAuth
{
    public bool IsEnabled => feature.RequireAuth;

    public ClaimsPrincipal? GetClaimsPrincipal(IRequest request)
    {
        var user = request.GetClaimsPrincipal();
        return user.IsAuthenticated() ? user : null;
    }

    public string? GetUserName(IRequest request)
    {
        if (!IsEnabled)
            return null;
        // a user without RequiredRole is no more authenticated than an anonymous one, so every
        // caller of GetUserName/CheckAuth (uploads, /v1/chat/completions, extension APIs) rejects
        // them without each needing its own role check
        if (!HasRequiredRole(request))
            return null;
        var user = GetClaimsPrincipal(request);
        if (user != null)
            return user.GetUserName();
        return GetApiKeyUserName(request);
    }

    /// <summary>
    /// Whether the request satisfies ChatFeature.RequiredRole, checked against the same identity
    /// GetUserName resolves (ClaimsPrincipal first, then API Key). Follows ServiceStack's convention
    /// that Admin satisfies any required role. Always true when RequiredRole isn't configured.
    /// </summary>
    public virtual bool HasRequiredRole(IRequest request)
    {
        var requiredRole = feature.RequiredRole;
        if (requiredRole == null)
            return true;

        var user = GetClaimsPrincipal(request);
        if (user != null)
            return user.HasRole(requiredRole) || user.HasRole(RoleNames.Admin);

        var apiKey = request.GetApiKey();
        if (apiKey != null)
            return apiKey.HasScope(requiredRole) || apiKey.HasScope(RoleNames.Admin);

        return false;
    }

    string? GetApiKeyUserName(IRequest request)
    {
        var apiKey = request.GetApiKey();
        return apiKey?.UserAuthId;
    }

    /// <summary>
    /// Resolve a Bearer API Key onto the request. ApiKeysFeature only does this for typed services,
    /// but the Chat UI's routes are dispatched by ChatHttpHandler — without this an API Key user
    /// couldn't sign in (GET /auth) or reach any /ext API.
    /// </summary>
    public static async Task ResolveApiKeyAsync(IRequest request)
    {
        if (request.GetApiKey() != null)
            return;
        var resolver = HostContext.TryResolve<IApiKeyResolver>();
        var source = HostContext.TryResolve<IApiKeySource>();
        if (resolver == null || source == null)
            return;
        var token = resolver.GetApiKeyToken(request);
        if (string.IsNullOrEmpty(token))
            return;
        var apiKey = await source.GetApiKeyAsync(token).ConfigAwait();
        if (apiKey != null)
        {
            request.SetItem(Keywords.ApiKey, apiKey);
        }
    }

    public string? AssertUserName(IRequest request)
    {
        if (!IsEnabled)
            return null;
        if (feature.RequiredRole != null && !HasRequiredRole(request))
            throw new UnauthorizedAccessException($"Authentication required: '{feature.RequiredRole}' Role Required");
        return GetUserName(request)
            ?? throw new UnauthorizedAccessException("Authentication required");
    }

    public (bool IsAuthenticated, JsonObject? Session) CheckAuth(IRequest request)
    {
        if (!IsEnabled)
            return (true, null);
        var userName = GetUserName(request);
        return userName != null
            ? (true, new JsonObject { ["userName"] = userName })
            : (false, null);
    }

    public virtual async Task<JsonObject?> GetAuthInfoAsync(IRequest request)
    {
        if (!IsEnabled)
            return null;
        // no auth info for a user the host authenticated but who lacks RequiredRole, so GET /auth
        // 401s and the UI keeps showing SignIn instead of treating them as signed in
        if (!HasRequiredRole(request))
            return null;

        var user = GetClaimsPrincipal(request);
        string authProvider = "identity";
        if (user == null)
        {
            var apiKey = request.GetApiKey();
            if (apiKey == null)
                return null;
            authProvider = "apikey";
            // resolve the key's user for richer profile info when available
            var userResolver = request.GetService<IUserResolver>();
            if (userResolver != null && apiKey.UserAuthId != null)
            {
                user = await userResolver.CreateClaimsPrincipalAsync(request, apiKey.UserAuthId).ConfigAwait();
            }
            if (user == null || !user.IsAuthenticated())
            {
                return new JsonObject
                {
                    ["userId"] = apiKey.UserAuthId ?? apiKey.Key,
                    ["userName"] = apiKey.UserAuthId ?? "user",
                    ["displayName"] = apiKey.UserAuthId ?? "user",
                    ["profileUrl"] = "/avatar/user",
                    ["roles"] = new JsonArray(apiKey.HasScope(RoleNames.Admin) ? [(JsonNode)RoleNames.Admin] : []),
                    ["authProvider"] = authProvider,
                };
            }
        }

        var roles = new JsonArray();
        foreach (var role in user.GetRoles())
        {
            roles.Add(role);
        }
        return new JsonObject
        {
            ["userId"] = user.GetUserId() ?? user.GetUserName(),
            ["userName"] = user.GetUserName(),
            ["displayName"] = user.GetDisplayName() ?? user.GetUserName(),
            ["profileUrl"] = user.GetPicture() ?? "/avatar/user",
            ["roles"] = roles,
            ["authProvider"] = authProvider,
        };
    }

    public virtual async Task SignOutAsync(IRequest request)
    {
        // Prefer the host's logout provider: Identity's signs out with SignInManager, i.e. the
        // Identity Application scheme. A bare HttpContext.SignOutAsync() uses the host's default
        // sign-out scheme instead, which can be a different cookie (e.g. Identity's External
        // scheme when it's registered as DefaultSignInScheme) leaving the user still signed in.
        if (HostContext.HasPlugin<AuthFeature>())
        {
            await using var authService = HostContext.ResolveService<AuthenticateService>(request);
            await authService.PostAsync(new Authenticate { provider = AuthenticateService.LogoutAction })
                .ConfigAwait();
            return;
        }

        if (request.OriginalRequest is HttpRequest httpReq)
        {
            await httpReq.HttpContext.SignOutAsync().ConfigAwait();
        }
    }

    public bool IsAdmin(IRequest request)
    {
        if (!IsEnabled)
            return true;
        var user = GetClaimsPrincipal(request);
        if (user != null && user.HasRole(RoleNames.Admin))
            return true;
        var apiKey = request.GetApiKey();
        return apiKey?.HasScope(RoleNames.Admin) == true;
    }
}
