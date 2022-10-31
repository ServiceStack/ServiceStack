#nullable enable
#if NET6_0_OR_GREATER
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using System.Net.Http;
using System.Net;
using System;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Collections.Generic;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.Host;
using Microsoft.JSInterop;
using ServiceStack.Auth;
using System.Collections.Specialized;
using ServiceStack.Configuration;

public static class BlazorServerUtils
{
    public static IHttpClientBuilder AddBlazorServerApiClient(this IServiceCollection services, string baseUrl, Action<HttpClient>? configure = null)
    {
        return services
            // Most reliable way to sync AuthenticationState + HttpClient is accessing HttpContext Cookies on server
            .AddHttpContextAccessor() 
            .AddTransient<CookieHandler>()
            .AddHttpClient<JsonApiClient>(client => {
                client.BaseAddress = new Uri(baseUrl);
                configure?.Invoke(client);
            })
            .ConfigureHttpMessageHandlerBuilder(h => new HttpClientHandler
            {
                UseCookies = false, // needed to allow manually adding cookies
                DefaultProxyCredentials = CredentialCache.DefaultCredentials,
            })
            .AddHttpMessageHandler<CookieHandler>();
    }

    //https://jasonwatmore.com/post/2020/08/09/blazor-webassembly-get-query-string-parameters-with-navigation-manager
    public static NameValueCollection QueryString(NavigationManager nav) =>
        System.Web.HttpUtility.ParseQueryString(new Uri(nav.Uri).Query);

    public static string? QueryString(NavigationManager nav, string key) => QueryString(nav)[key];

    public static string GetReturnUrl(NavigationManager nav)
    {
        var returnUrl = QueryString(nav, "return");
        if (returnUrl == null || returnUrl.IsEmpty())
            return "/";
        return returnUrl;
    }

}

public class CookieHandler : DelegatingHandler, IDisposable
{
    IHttpContextAccessor HttpContextAccessor;
    public CookieHandler(IHttpContextAccessor httpContextAccessor) => HttpContextAccessor = httpContextAccessor;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var req = (IHttpRequest)HttpContextAccessor.GetOrCreateRequest();
        var httpCookies = req.Cookies.Values.ToList();

        if (httpCookies?.Count > 0)
        {
            var cookieHeader = string.Join("; ", httpCookies.Select(x => $"{x.Name}={x.Value.UrlEncode()}"));
            request.Headers.Add(HttpHeaders.Cookie, cookieHeader);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

public class BlazorServerAuthenticationStateProvider : AuthenticationStateProvider
{
    protected JsonApiClient Client { get; }
    protected ILogger<BlazorServerAuthenticationStateProvider> Log { get; }
    protected IHttpContextAccessor HttpContextAccessor { get; }
    protected NavigationManager NavigationManager { get; }
    protected IJSRuntime JS { get; }

    public BlazorServerAuthenticationStateProvider(
        JsonApiClient client, 
        ILogger<BlazorServerAuthenticationStateProvider> log, 
        IHttpContextAccessor httpContextAccessor, 
        NavigationManager navigationManager,
        IJSRuntime js)
    {
        Client = client;
        Log = log;
        HttpContextAccessor = httpContextAccessor;
        NavigationManager = navigationManager;
        JS = js;
    }

    protected AuthenticationState UnAuthenticationState => new(new ClaimsPrincipal(new ClaimsIdentity()));

    public const string DefaultProfileUrl = "data:image/svg+xml,%3Csvg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'%3E%3Cstyle%3E .path%7B%7D %3C/style%3E%3Cg id='male-svg'%3E%3Cpath fill='%23556080' d='M1 92.84V84.14C1 84.14 2.38 78.81 8.81 77.16C8.81 77.16 19.16 73.37 27.26 69.85C31.46 68.02 32.36 66.93 36.59 65.06C36.59 65.06 37.03 62.9 36.87 61.6H40.18C40.18 61.6 40.93 62.05 40.18 56.94C40.18 56.94 35.63 55.78 35.45 47.66C35.45 47.66 32.41 48.68 32.22 43.76C32.1 40.42 29.52 37.52 33.23 35.12L31.35 30.02C31.35 30.02 28.08 9.51 38.95 12.54C34.36 7.06 64.93 1.59 66.91 18.96C66.91 18.96 68.33 28.35 66.91 34.77C66.91 34.77 71.38 34.25 68.39 42.84C68.39 42.84 66.75 49.01 64.23 47.62C64.23 47.62 64.65 55.43 60.68 56.76C60.68 56.76 60.96 60.92 60.96 61.2L64.74 61.76C64.74 61.76 64.17 65.16 64.84 65.54C64.84 65.54 69.32 68.61 74.66 69.98C84.96 72.62 97.96 77.16 97.96 81.13C97.96 81.13 99 86.42 99 92.85L1 92.84Z'/%3E%3C/g%3E%3C/svg%3E";
    public const string Picture = "picture";
    public const string PermissionType = "perm";
    public const string AuthenticationType = "Server Authentication";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var req = AppHostBase.GetOrCreateRequest(HttpContextAccessor);
            var session = await req.GetSessionAsync();
            if (!session.IsAuthenticated)
                return UnAuthenticationState;

            List<Claim> claims = new() {
                new Claim(ClaimTypes.NameIdentifier, session.UserAuthId),
                new Claim(ClaimTypes.Name, session.DisplayName),
                new Claim(ClaimTypes.Email, session.UserName),
                new Claim(Picture, session.ProfileUrl ?? DefaultProfileUrl),
            };

            var roles = (session.FromToken
                ? session.Roles
                : await session.GetRolesAsync(HostContext.AppHost.GetAuthRepositoryAsync())).OrEmpty().ToList();
            // Add all App Roles to Admin Users
            if (roles.Contains(RoleNames.Admin))
            {
                roles.AddDistinctRange(HostContext.Metadata.GetAllRoles());
            }
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            await OnAuthenticationStateClaimsAsync(claims);

            var perms = session.FromToken
                ? session.Permissions
                : await session.GetRolesAsync(HostContext.AppHost.GetAuthRepositoryAsync());
            foreach (var permission in perms.OrEmpty())
            {
                claims.Add(new Claim(PermissionType, permission));
            }

            var identity = new ClaimsIdentity(claims, AuthenticationType);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "SignIn failed");
            return UnAuthenticationState;
        }
    }

    public virtual Task OnAuthenticationStateClaimsAsync(List<Claim> claims) => Task.CompletedTask;

    public virtual async Task LogoutIfAuthenticatedAsync()
    {
        var authState = await GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
            await LogoutAsync();
    }

    public virtual Task LogoutAsync(string? redirectTo = null)
    {
        NotifyAuthenticationStateChanged(Task.FromResult(UnAuthenticationState));
        var url = "/auth/logout" + (redirectTo != null ? "?continue=" + redirectTo : "");
        NavigationManager.NavigateTo(url, forceLoad: true);
        return Task.CompletedTask;
    }

    public virtual Task<ApiResult<AuthenticateResponse>> SignInAsync(ApiResult<AuthenticateResponse> api)
    {
        if (api.Succeeded)
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        return Task.FromResult(api);
    }

    public virtual Task<ApiResult<AuthenticateResponse>> SignInAsync(AuthenticateResponse authResponse) =>
        SignInAsync(ApiResult.Create(authResponse));

    // Can SignInAsync with RegisterResponse when Register.AutoLogin = true
    public virtual Task<ApiResult<AuthenticateResponse>> SignInAsync(RegisterResponse registerResponse) =>
        SignInAsync(ApiResult.Create(registerResponse.ToAuthenticateResponse()));

    public virtual async Task<ApiResult<RegisterResponse>> RegisterAsync(Register request)
    {
        var api = await AuthApiAsync(request);
        return api;
    }

    /// <summary>
    /// Execute Request Serializing API or Session Cookies to document.cookie for HttpClient calls to utilize browsers cookies
    /// </summary>
    protected virtual async Task<ApiResult<T>> AuthApiAsync<T>(IReturn<T> request) where T : class
    {
        try
        {
            var apiReq = new BasicHttpRequest();
            object? authResult = null;
            if (request is Authenticate authRequest)
            {
                using var authService = HostContext.ResolveService<AuthenticateService>(apiReq);
                authResult = await authService.PostAsync(authRequest);
            }
            else if (request is Register registerRequest)
            {
                using var registerService = HostContext.ResolveService<RegisterService>(apiReq);
                authResult = await registerService.PostAsync(registerRequest);
            }
            else
            {
                authResult = AppHostBase.Instance.ServiceController.ManagedGatewayExecuteAsync(request, apiReq);
            }

            var req = HttpContextAccessor.GetOrCreateRequest();
            var httpCookies = authResult is IHttpResult httpResult && httpResult.Cookies?.Count > 0
                ? httpResult.Cookies
                : null;

            //Log.LogDebug("CookieCollection: {0}", string.Join("; ", ((BasicHttpResponse)apiReq.Response).CookieCollection.Select(x => $"{x.Name}: {x.Value}")));

            if (httpCookies != null)
            {
                var jsCookies = httpCookies.Map(x => new JsCookie
                {
                    Name = x.Name,
                    Value = x.Value,
                    Path = x.Path,
                    Domain = x.Domain,
                    Expires = x.Expires.ToString("R"),
                });
                if (Log.IsEnabled(LogLevel.Debug)) 
                    Log.LogDebug("API JS.setCookies: {0}", string.Join("; ", jsCookies.Select(x => $"{x.Name}={x.Value}")));
                await JS.InvokeVoidAsync("JS.setCookies", jsCookies);
            }
            else
            {
                var jsCookies = new JsCookie[] {
                    new() {
                        Name = SessionFeature.SessionId,
                        Value = apiReq.GetSessionId(),
                    },
                    new() {
                        Name = SessionFeature.PermanentSessionId,
                        Value = apiReq.GetPermanentSessionId(),
                        Expires = DateTime.UtcNow.AddYears(20).ToString("R"),
                    },
                    new() {
                        Name = SessionFeature.SessionOptionsKey,
                        Value = apiReq.GetSessionParam(SessionFeature.SessionOptionsKey),
                    },
                };
                if (Log.IsEnabled(LogLevel.Debug)) 
                    Log.LogDebug("SESSION JS.setCookies: {0}", string.Join("; ", jsCookies.Select(x => $"{x.Name}={x.Value}")));
                await JS.InvokeVoidAsync("JS.setCookies", jsCookies);
            }

            var authRes = authResult.GetResponseDto<T>();
            return ApiResult.Create(authRes);
        }
        catch (Exception ex)
        {
            return ApiResult.CreateError<T>(ex);
        }
    }

    public virtual async Task<ApiResult<AuthenticateResponse>> LoginAsync(Authenticate request)
    {
        var authRes = await AuthApiAsync(request);
        return await SignInAsync(authRes);
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

public class JsCookie
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Path { get; set; }
    public string Domain { get; set; }
    public string Expires { get; set; }
}

#endif
