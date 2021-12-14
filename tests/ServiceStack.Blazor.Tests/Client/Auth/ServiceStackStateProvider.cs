using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using ServiceStack;

namespace MyApp.Client;

/// <summary>
/// Typed Wrapper around populated claims
/// </summary>
public static class ClaimUtils
{
    public const string AuthenticationType = "Server Authentication";
    public const string PermissionType = "perm";

    public static bool IsAuthenticated(this AuthenticationState? state) => state?.User?.AuthenticatedUser() != null;
    public static ClaimsPrincipal? AuthenticatedUser(this AuthenticationState? state) => state?.User?.AuthenticatedUser();
    public static ClaimsPrincipal? AuthenticatedUser(this ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true ? principal : null;

    public static string? GetUserId(this ClaimsPrincipal principal) => principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public static string? GetDisplayName(this ClaimsPrincipal principal) => principal.FindFirst(ClaimTypes.Name)?.Value;
    public static string? GetEmail(this ClaimsPrincipal principal) => principal.FindFirst(ClaimTypes.Email)?.Value;
    public static string[] GetRoles(this ClaimsPrincipal principal) => principal.Claims.Where(x => x.Type == ClaimTypes.Role)
        .Select(x => x.Value).ToArray();
    public static string[] GetPermissions(this ClaimsPrincipal principal) => principal.Claims.Where(x => x.Type == PermissionType)
        .Select(x => x.Value).ToArray();

    public static bool HasRole(this ClaimsPrincipal? principal, string roleName) => principal?.GetRoles()
        .Contains(roleName) == true;
    public static bool HasAllRoles(this ClaimsPrincipal? principal, params string[] roleNames) => principal?.GetRoles()
        .All(roleNames.Contains) == true;
}

public class ServiceStackStateProvider : AuthenticationStateProvider
{
    private ApiResult<AuthenticateResponse> authResult = new();
    private readonly JsonApiClient client;

    ILogger<ServiceStackStateProvider> Log { get; }

    public ServiceStackStateProvider(JsonApiClient client, ILogger<ServiceStackStateProvider> log)
    {
        this.client = client;
        this.Log = log;
    }

    public AuthenticateResponse? AuthUser => authResult.Response;
    public bool IsAuthenticated => authResult.Response != null;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var authResponse = authResult.Response;
            if (authResponse == null)
            {
                Log.LogInformation("Checking server /auth for authentication");
                var apiResult = await client.ApiAsync(new Authenticate());
                authResponse = apiResult.Response;
            }
            
            if (authResponse is null)
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, authResponse.UserId),
                new Claim(ClaimTypes.Name, authResponse.DisplayName),
                new Claim(ClaimTypes.Email, authResponse.UserName)
            };
            foreach (var role in authResponse.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            foreach (var permission in authResponse.Permissions)
            {
                claims.Add(new Claim(ClaimUtils.PermissionType, permission));
            }

            var identity = new ClaimsIdentity(claims, ClaimUtils.AuthenticationType);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "SignIn failed");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task<ApiResult<AuthenticateResponse>> LogoutAsync()
    {
        var logoutResult = await client.ApiAsync(new Authenticate { provider = "logout" });
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        authResult.Reset();
        return logoutResult;
    }

    public Task<ApiResult<AuthenticateResponse>> SignInAsync(ApiResult<AuthenticateResponse> apiResult)
    {
        authResult = apiResult;
        if (authResult.IsSuccess)
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        return Task.FromResult(authResult);
    }

    public Task<ApiResult<AuthenticateResponse>> SignInAsync(AuthenticateResponse authResponse) =>
        SignInAsync(ApiResult.Create(authResponse));

    // Can SignInAsync with RegisterResponse when Register.AutoLogin = true
    public Task<ApiResult<AuthenticateResponse>> SignInAsync(RegisterResponse registerResponse) =>
        SignInAsync(ApiResult.Create(registerResponse.ToAuthenticateResponse()));

    public async Task<ApiResult<AuthenticateResponse>> LoginAsync(string email, string password)
    {
        return await SignInAsync(await client.ApiAsync(new Authenticate
        {
            provider = "credentials",
            Password = password,
            UserName = email,
            UseTokenCookie = true
        }));
    }
}
