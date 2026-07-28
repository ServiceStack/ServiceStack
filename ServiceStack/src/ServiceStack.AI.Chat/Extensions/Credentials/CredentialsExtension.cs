using Microsoft.Extensions.Logging;
using ServiceStack.Auth;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Username/password SignIn rendered inside the Chat UI — the Identity Auth counterpart of llms-py's
/// 'credentials' extension. Instead of its own users.json + session files, POST /auth/login
/// authenticates with ServiceStack's Authenticate API using the host's 'credentials' auth provider
/// (IdentityCredentialsAuthProvider → SignInManager.PasswordSignInAsync), so the Chat UI signs in
/// against the host's ASP.NET Identity users and shares its auth cookie.
///
/// Installed when RequireAuth is enabled and AuthType is Credentials; user management is the host's
/// (Identity's AdminUsersFeature) rather than the --adduser CLI of the Python extension.
/// </summary>
public class CredentialsExtension : IChatExtension
{
    public string Name => ChatExtension.Credentials;

    ChatFeature feature = null!;

    public void Install(ExtensionContext ctx)
    {
        feature = ctx.Feature;
        if (!ctx.IsAuthEnabled || feature.AuthType != ChatAuthType.Credentials)
        {
            ctx.Log.LogInformation("AuthType is {AuthType}, skipping credentials SignIn", feature.AuthType);
            ctx.Disabled = true;
            return;
        }

        // Python parity: /auth/login sits alongside the core /auth + /auth/logout routes,
        // outside this extension's /ext/credentials prefix (a leading '/' escapes it)
        ctx.AddPost("/auth/login", LoginAsync);
    }

    async Task<object?> LoginAsync(ChatRequestContext ctx)
    {
        var body = await ctx.GetJsonBodyAsync().ConfigAwait();
        // llms-py's SignIn posts 'username', ServiceStack DTOs use 'userName'
        var userName = (body.GetString("userName") ?? body.GetString("username"))?.Trim();
        var password = body.GetString("password");

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            return ChatResult.Json(ChatJson.CreateErrorResponse(
                "Username and password are required", "Validation"), 400);
        }

        using var authService = HostContext.ResolveService<AuthenticateService>(ctx.Request);
        object response;
        try
        {
            response = await authService.PostAsync(new Authenticate
            {
                provider = AuthenticateService.CredentialsProvider,
                UserName = userName,
                Password = password,
                RememberMe = body.GetBool("rememberMe", defaultValue: true),
            }).ConfigAwait();
        }
        catch (Exception e)
        {
            feature.Log.LogInformation("Credentials sign in failed for '{UserName}': {Message}", userName, e.Message);
            return ChatResult.Json(ChatJson.ToErrorResponse(e), e.ToStatusCode());
        }

        // signing in populates the request's ClaimsPrincipal, so /auth's payload can be reused
        var authInfo = await feature.ChatAuth.GetAuthInfoAsync(ctx.Request).ConfigAwait();
        if (authInfo != null)
            return authInfo;

        var authResponse = response as AuthenticateResponse
            ?? (response as IHttpResult)?.Response as AuthenticateResponse;
        if (authResponse == null)
        {
            return ChatResult.Json(ChatJson.CreateErrorResponse(
                "Sign in did not return an authenticated session", "Unauthorized"), 401);
        }

        return new JsonObject
        {
            ["userId"] = authResponse.UserId ?? authResponse.UserName ?? userName,
            ["userName"] = authResponse.UserName ?? userName,
            ["displayName"] = authResponse.DisplayName ?? authResponse.UserName ?? userName,
            ["profileUrl"] = authResponse.ProfileUrl ?? "/avatar/user",
            ["roles"] = new JsonArray((authResponse.Roles ?? []).Select(x => (JsonNode)x).ToArray()),
            ["authProvider"] = AuthenticateService.CredentialsProvider,
        };
    }
}
