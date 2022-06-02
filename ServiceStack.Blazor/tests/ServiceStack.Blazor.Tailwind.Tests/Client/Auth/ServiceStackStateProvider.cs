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

    public static string? GetUserId(this ClaimsPrincipal? principal) => principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public static string? GetDisplayName(this ClaimsPrincipal? principal) => principal?.FindFirst(ClaimTypes.Name)?.Value;
    public static string? GetEmail(this ClaimsPrincipal? principal) => principal?.FindFirst(ClaimTypes.Email)?.Value;
    public static string[] GetRoles(this ClaimsPrincipal? principal) => principal?.Claims.Where(x => x.Type == ClaimTypes.Role)
        .Select(x => x.Value).ToArray() ?? Array.Empty<string>();
    public static string[] GetPermissions(this ClaimsPrincipal? principal) => principal?.Claims.Where(x => x.Type == PermissionType)
        .Select(x => x.Value).ToArray() ?? Array.Empty<string>();

    public static bool HasRole(this ClaimsPrincipal? principal, string roleName) => principal?.GetRoles()
        .Contains(roleName) == true;
    public static bool HasAllRoles(this ClaimsPrincipal? principal, params string[] roleNames) => principal?.GetRoles()
        .All(roleNames.Contains) == true;

    public static string GetProfileUrl(this ClaimsPrincipal? principal) => principal?.FindFirst(ClaimTypesExt.Picture)?.Value
        ?? "data:image/svg+xml,%3Csvg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'%3E %3Cstyle%3E .path%7B%7D %3C/style%3E %3Cg id='male-svg'%3E%3Cpath fill='%23556080' d='M1 92.84V84.14C1 84.14 2.38 78.81 8.81 77.16C8.81 77.16 19.16 73.37 27.26 69.85C31.46 68.02 32.36 66.93 36.59 65.06C36.59 65.06 37.03 62.9 36.87 61.6H40.18C40.18 61.6 40.93 62.05 40.18 56.94C40.18 56.94 35.63 55.78 35.45 47.66C35.45 47.66 32.41 48.68 32.22 43.76C32.1 40.42 29.52 37.52 33.23 35.12L31.35 30.02C31.35 30.02 28.08 9.51 38.95 12.54C34.36 7.06 64.93 1.59 66.91 18.96C66.91 18.96 68.33 28.35 66.91 34.77C66.91 34.77 71.38 34.25 68.39 42.84C68.39 42.84 66.75 49.01 64.23 47.62C64.23 47.62 64.65 55.43 60.68 56.76C60.68 56.76 60.96 60.92 60.96 61.2L64.74 61.76C64.74 61.76 64.17 65.16 64.84 65.54C64.84 65.54 69.32 68.61 74.66 69.98C84.96 72.62 97.96 77.16 97.96 81.13C97.96 81.13 99 86.42 99 92.85L1 92.84Z'/%3E%3C/g%3E%3C/svg%3E";
}

public static class ClaimTypesExt
{
    public const string Picture = "picture";
}

public class ServiceStackStateProvider : AuthenticationStateProvider
{
    private ApiResult<AuthenticateResponse> authApi = new();
    private readonly JsonApiClient client;

    ILogger<ServiceStackStateProvider> Log { get; }

    public ServiceStackStateProvider(JsonApiClient client, ILogger<ServiceStackStateProvider> log)
    {
        this.client = client;
        this.Log = log;
    }

    public AuthenticateResponse? AuthUser => authApi.Response;
    public bool IsAuthenticated => authApi.Response != null;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var authResponse = authApi.Response;
            if (authResponse == null)
            {
                Log.LogInformation("Checking server /auth for authentication");
                var authApi = await client.ApiAsync(new Authenticate());
                authResponse = authApi.Response;
            }
            
            if (authResponse is null)
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, authResponse.UserId),
                new Claim(ClaimTypes.Name, authResponse.DisplayName),
                new Claim(ClaimTypes.Email, authResponse.UserName),
                new Claim(ClaimTypesExt.Picture, authResponse.ProfileUrl),
            };

            // Add all App Roles to Admin Users to use [Authorize(Roles)]
            var isAdmin = authResponse.Roles.FirstOrDefault(x => x == AppRoles.Admin);
            var roles = isAdmin != null
                ? authResponse.Roles.Union(AppRoles.All).Distinct()
                : authResponse.Roles;
            foreach (var role in roles)
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
    public async Task LogoutIfAuthenticatedAsync()
    {
        var authState = await GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
            await LogoutAsync();
    }

    public async Task<ApiResult<AuthenticateResponse>> LogoutAsync()
    {
        var logoutResult = await client.ApiAsync(new Authenticate { provider = "logout" });
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        authApi.ClearErrors();
        return logoutResult;
    }

    public Task<ApiResult<AuthenticateResponse>> SignInAsync(ApiResult<AuthenticateResponse> api)
    {
        authApi = api;
        if (authApi.Succeeded)
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        return Task.FromResult(authApi);
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
        }));
    }
}
