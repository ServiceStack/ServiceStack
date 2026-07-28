using System.Text.Json.Nodes;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// The auth seam of the Chat UI, replacing llms-py's pluggable AuthProvider (credentials/github_auth
/// extensions) with ServiceStack Identity Auth. The username partitions all per-user data
/// (db 'user' columns + App_Data/chat/user/&lt;user&gt;/ paths).
/// </summary>
public interface IChatAuth
{
    /// <summary>Whether auth is required. When false everything runs as the "default" user (Python parity).</summary>
    bool IsEnabled { get; }

    /// <summary>Authenticated username or null (null also when auth is disabled, matching Python).</summary>
    string? GetUserName(IRequest request);

    /// <summary>Throws when auth is enabled and the request is unauthenticated.</summary>
    string? AssertUserName(IRequest request);

    /// <summary>Returns (isAuthenticated, session). Always (true, null) when auth is disabled.</summary>
    (bool IsAuthenticated, JsonObject? Session) CheckAuth(IRequest request);

    /// <summary>The GET /auth payload: {userId, userName, displayName, profileUrl, roles, authProvider}, or null =&gt; 401</summary>
    Task<JsonObject?> GetAuthInfoAsync(IRequest request);

    /// <summary>POST /auth/logout</summary>
    Task SignOutAsync(IRequest request);

    bool IsAdmin(IRequest request);
}
