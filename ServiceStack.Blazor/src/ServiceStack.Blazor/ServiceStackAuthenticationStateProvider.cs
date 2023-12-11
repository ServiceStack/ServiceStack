using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ServiceStack.Blazor;

[Obsolete("Renamed to BlazorWasmAuthenticationStateProvider")]
public class ServiceStackAuthenticationStateProvider : BlazorWasmAuthenticationStateProvider
{
    public ServiceStackAuthenticationStateProvider(JsonApiClient client, ILogger<ServiceStackAuthenticationStateProvider> log, NavigationManager navigationManager)
        : base(new BlazorWasmAuthContext(client, navigationManager), log) { }
}

public class BlazorWasmAuthContext
{
    public JsonApiClient Client { get; }
    public NavigationManager NavigationManager { get; }
    public BlazorWasmAuthContext(
        JsonApiClient client,
        NavigationManager navigationManager)
    {
        Client = client;
        NavigationManager = navigationManager;
    }
}

public class BlazorWasmAuthenticationStateProvider : AuthenticationStateProvider
{
    protected ApiResult<AuthenticateResponse> authApi = new();
    protected ILogger<BlazorWasmAuthenticationStateProvider> Log { get; }
    protected JsonApiClient Client { get; }
    protected NavigationManager NavigationManager { get; }
    public BlazorWasmAuthenticationStateProvider(BlazorWasmAuthContext context, ILogger<BlazorWasmAuthenticationStateProvider> log)
    {
        Log = log;
        Client = context.Client;
        NavigationManager = context.NavigationManager;
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
                var authApi = await Client.ApiAsync(new Authenticate());
                authResponse = authApi.Response;
            }
            
            if (authResponse is null)
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            List<Claim> claims = new() {
                new Claim(ClaimTypes.NameIdentifier, authResponse.UserId),
                new Claim(ClaimTypes.Name, authResponse.DisplayName),
                new Claim(ClaimTypes.Email, authResponse.UserName),
                new Claim(BlazorClaimUtils.Picture, authResponse.ProfileUrl),
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

            var identity = new ClaimsIdentity(claims, BlazorClaimUtils.AuthenticationType);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "SignIn failed");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public virtual async Task LogoutIfAuthenticatedAsync(string? redirectTo = null)
    {
        var authState = await GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
            await LogoutAsync(redirectTo);
        else if (redirectTo != null)
            NavigationManager.NavigateTo(redirectTo);
    }

    public virtual async Task<ApiResult<AuthenticateResponse>> LogoutAsync(string? redirectTo = null)
    {
        var logoutResult = await Client.ApiAsync(new Authenticate { 
            provider = "logout",            
        });
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        authApi.ClearErrors();
        
        if (redirectTo != null)
            NavigationManager.NavigateTo(redirectTo);

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

    public virtual async Task<ApiResult<RegisterResponse>> RegisterAsync(Register request)
    {
        var api = await Client.ApiAsync(request);
        if (api.Succeeded)
        {
            await SignInAsync(api.Response!);
        }
        return api;
    }

    public virtual async Task<ApiResult<AuthenticateResponse>> LoginAsync(Authenticate request)
    {
        return await SignInAsync(await Client.ApiAsync(request));
    }

    public virtual async Task<ApiResult<AuthenticateResponse>> LoginAsync(string email, string password, bool? rememberMe = null)
    {
        return await LoginAsync(new Authenticate {
            provider = "credentials",
            Password = password,
            UserName = email,
            RememberMe = rememberMe,
        });
    }
}
