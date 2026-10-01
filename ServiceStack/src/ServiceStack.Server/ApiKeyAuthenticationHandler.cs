#nullable enable
#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.Configuration;

namespace ServiceStack;

/// <summary>
/// Authenticates requests sent with a User API Key as the user the API Key belongs to, so APIs protected with
/// [ValidateIsAuthenticated] can be called with either the user's session or one of their API Keys:
/// <code>services.AddAuthentication(...).AddIdentityCookies(...).AddApiKeyAuth();</code>
/// The user is authenticated without their roles. The scopes of the API Key are added as scope claims
/// (session.Scopes) and its Admin scope as the Admin role. API Keys that don't belong to a user aren't
/// authenticated, they continue to only be able to call [ValidateApiKey] APIs.
/// </summary>
public static class ApiKeyAuthenticationHandler
{
    public const string Scheme = "ApiKey";

    public static AuthenticationBuilder AddApiKeyAuth(this AuthenticationBuilder builder,
        Action<ApiKeyAuthenticationOptions>? configure = null)
    {
        ServiceStackHost.InitOptions.AllowedAuthenticationSchemes.AddIfNotExists(Scheme);
        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthHandler>(Scheme, configure);
    }

    /// <summary>
    /// Whether the request was authenticated with a User API Key
    /// </summary>
    public static bool IsApiKeyUser(this ClaimsPrincipal? user) =>
        user?.Identities.Any(x => x.IsAuthenticated && x.AuthenticationType == Scheme) == true;
}

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Customize the claims of the user authenticated with the API Key
    /// </summary>
    public Action<IApiKey, List<Claim>>? ClaimsFilter { get; set; }
}

public class ApiKeyAuthHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeySource apiKeySource)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    /// <summary>
    /// The API Key the request was sent with, from the configured HTTP Header or Bearer Token
    /// </summary>
    protected virtual string? GetApiKeyToken()
    {
        var feature = HostContext.AppHost?.GetPlugin<ApiKeysFeature>();
        if (feature == null)
            return null;

        var token = feature.HttpHeader != null ? Request.Headers[feature.HttpHeader].FirstOrDefault() : null;
        if (string.IsNullOrEmpty(token))
        {
            var auth = Request.Headers.Authorization.FirstOrDefault();
            if (auth?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                token = auth["Bearer ".Length..].Trim();
        }
        if (string.IsNullOrEmpty(token))
            return null;
        // Other Bearer Tokens like JWTs are left for their own schemes 
        if (feature.ApiKeyPrefix != null && !token.StartsWith(feature.ApiKeyPrefix))
            return null;
        return token;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = GetApiKeyToken();
        if (token == null)
            return AuthenticateResult.NoResult();

        IApiKey? apiKey;
        try
        {
            apiKey = await apiKeySource.GetApiKeyAsync(token);
        }
        catch (Exception e)
        {
            // Cancelled or expired
            return AuthenticateResult.Fail(e.Message);
        }
        if (apiKey == null)
            return AuthenticateResult.Fail(ErrorMessages.ApiKeyInvalid);
        if (string.IsNullOrEmpty(apiKey.UserAuthId))
            return AuthenticateResult.NoResult();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.UserAuthId),
            new(ClaimTypes.AuthenticationMethod, Keywords.ApiKeyParam),
        };
        if (apiKey is ApiKeysFeature.ApiKey key)
        {
            if (!string.IsNullOrEmpty(key.UserName))
                claims.Add(new(ClaimTypes.Name, key.UserName));
            foreach (var scope in key.Scopes ?? [])
            {
                claims.Add(new(JwtClaimTypes.Scope, scope));
            }
        }
        if (apiKey.HasScope(RoleNames.Admin))
            claims.Add(new(ClaimTypes.Role, RoleNames.Admin));
        Options.ClaimsFilter?.Invoke(apiKey, claims);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    // Only requests sent with an API Key are answered, so others can still be e.g. redirected to Sign In
    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        RespondWith(StatusCodes.Status401Unauthorized);

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        RespondWith(StatusCodes.Status403Forbidden);

    private Task RespondWith(int statusCode)
    {
        if (GetApiKeyToken() == null)
            return Task.CompletedTask;

        void Apply()
        {
            if (Response.HasStarted) return;
            Response.StatusCode = statusCode;
            Response.Headers.Remove(HttpHeaders.Location);
            if (statusCode == StatusCodes.Status401Unauthorized)
                Response.Headers.WWWAuthenticate = Scheme.Name;
        }
        Apply();
        // Other schemes of the endpoint are challenged as well, in any order: a cookie scheme would redirect to Sign In
        Response.OnStarting(() =>
        {
            Apply();
            return Task.CompletedTask;
        });
        return Task.CompletedTask;
    }
}

#endif
