using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace ServiceStack.Blazor;

public class ServiceStackAuthenticationStateProvider : AuthenticationStateProvider
{
    protected ApiResult<AuthenticateResponse> authApi = new();
    protected readonly JsonApiClient client;

    protected ILogger<ServiceStackAuthenticationStateProvider> Log { get; }

    public ServiceStackAuthenticationStateProvider(JsonApiClient client, ILogger<ServiceStackAuthenticationStateProvider> log)
    {
        this.client = client;
        this.Log = log;
    }

    public AuthenticateResponse? AuthUser => authApi.Response;
    public virtual bool IsAuthenticated => authApi.Response != null;
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

            List<Claim> claims = new() {
                new Claim(ClaimTypes.NameIdentifier, authResponse.UserId),
                new Claim(ClaimTypes.Name, authResponse.DisplayName),
                new Claim(ClaimTypes.Email, authResponse.UserName),
                new Claim(ClaimUtils.Picture, authResponse.ProfileUrl),
            };

            var roles = authResponse.Roles;
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

    public virtual async Task LogoutIfAuthenticatedAsync()
    {
        var authState = await GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
            await LogoutAsync();
    }

    public virtual async Task<ApiResult<AuthenticateResponse>> LogoutAsync()
    {
        var logoutResult = await client.ApiAsync(new Authenticate { provider = "logout" });
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        authApi.ClearErrors();
        return logoutResult;
    }

    public virtual Task<ApiResult<AuthenticateResponse>> SignInAsync(ApiResult<AuthenticateResponse> api)
    {
        authApi = api;
        if (authApi.Succeeded)
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        return Task.FromResult(authApi);
    }

    public virtual Task<ApiResult<AuthenticateResponse>> SignInAsync(AuthenticateResponse authResponse) =>
        SignInAsync(ApiResult.Create(authResponse));

    // Can SignInAsync with RegisterResponse when Register.AutoLogin = true
    public virtual Task<ApiResult<AuthenticateResponse>> SignInAsync(RegisterResponse registerResponse) =>
        SignInAsync(ApiResult.Create(registerResponse.ToAuthenticateResponse()));

    public virtual async Task<ApiResult<AuthenticateResponse>> LoginAsync(string email, string password)
    {
        return await SignInAsync(await client.ApiAsync(new Authenticate {
            provider = "credentials",
            Password = password,
            UserName = email,
        }));
    }
}
