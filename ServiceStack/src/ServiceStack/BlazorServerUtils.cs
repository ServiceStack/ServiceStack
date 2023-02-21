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
using ServiceStack.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.IO;
using System.Text.Encodings.Web;

namespace ServiceStack;

public static class BlazorServerUtils
{
    public static IHttpClientBuilder AddBlazorServerApiClient(this IServiceCollection services, string baseUrl, Action<HttpClient>? configure = null)
    {
        return services
            .AddScoped<HostState>()
            .AddScoped<IClientFactory,BlazorServerClientFactory>()
            .AddTransient<BlazorServerAuthContext>()
            .AddSingleton<IGatewayRequestFactory, GatewayRequestFactory>()
            .AddTransient<IServiceGateway>(c => new InProcessServiceGateway(
                c.GetRequiredService<IGatewayRequestFactory>().Create(c.GetRequiredService<HostState>().ToGatewayRequest())))
            .AddHttpClient<JsonApiClient>(client => {
                client.BaseAddress = new Uri(baseUrl);
                configure?.Invoke(client);
            })
            .ConfigureHttpMessageHandlerBuilder(h => new HttpClientHandler {
                UseCookies = false, // needed to allow manually adding cookies
                DefaultProxyCredentials = CredentialCache.DefaultCredentials,
            });
    }

    public static JsonApiClient ConfigureClient(this HostState hostState, JsonApiClient Client)
    {
        Client.ClearHeaders();
        if (hostState.CookiesHeader != null)
            Client.AddHeader(HttpHeaders.Cookie, hostState.CookiesHeader);
        return Client;
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

    public static InitialHostState? GetInitialHostState(this HttpRequest httpReq)
    {
        if (httpReq.HttpContext.GetOrCreateRequest() is IHttpRequest req)
        {
            var to = new InitialHostState
            {
                Session = req.GetSession().ToAuthUserSession(),
                AbsoluteUri = req.AbsoluteUri,
                Headers = req.Headers.ToDictionary(),
                Cookies = req.Cookies.Values.Map(x => x.ToJsCookie()) ?? new()
            };

            var log = LogManager.GetLogger(typeof(BlazorServerUtils));
            if (log.IsDebugEnabled)
                log.DebugFormat("InitialHostState {0} Cookies: {1}", to.Cookies.Count, string.Join(',', to.Cookies.Select(x => x.Name)));

            return to;
        }
        return null;
    }

    public static JsCookie ToJsCookie(this Cookie cookie) => new() {
        Name = cookie.Name,
        Value = cookie.Value,
        Path = cookie.Path,
        Domain = cookie.Domain,
        Expires = cookie.Expires.ToString("R"),
    };

    public static Cookie ToCookie(this JsCookie cookie) => new() {
        Name = cookie.Name,
        Value = cookie.Value,
        Path = cookie.Path,
        Domain = cookie.Domain,
    };

    public static GatewayRequest ToGatewayRequest(this HostState hostState)
    {
        var to = GatewayRequest.Create(new BasicHttpRequest());
        to.Items[Keywords.Session] = hostState.Session.FromAuthUserSession();
        foreach (var cookie in hostState.Cookies)
        {
            to.Cookies.Add(new(cookie.Name, cookie.ToCookie()));
        }
        return to;
    }
}

public class BlazorServerClientFactory : IClientFactory
{
    public HostState HostState { get; }
    public JsonApiClient Client { get; }
    public IServiceGateway Gateway { get; }
    public BlazorServerClientFactory(HostState hostState, JsonApiClient client, IServiceGateway gateway)
    {
        HostState = hostState;
        Client = client;
        Gateway = gateway;
    }
    public JsonApiClient GetClient() => HostState.ConfigureClient(Client);
    public IServiceGateway GetGateway()
    {
        if (Gateway is InProcessServiceGateway inProcGateway && inProcGateway.Request is GatewayRequest gatewayReq)
        {
            gatewayReq.Clear();
        }

        return Gateway;
    }
}

public interface IGatewayRequestFactory
{
    IRequest Create(IRequest request);
}

public class GatewayRequestFactory : IGatewayRequestFactory
{
    public IRequest Create(IRequest request)
    {
        var to = GatewayRequest.Create(request);
        return to;
    }
}

public class InitialHostState
{
    public string AbsoluteUri { get; init; }
    // needs to be serializable
    public AuthUserSession Session { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public List<JsCookie> Cookies { get; set; } = new();
}

public class HostState
{
    // needs to be serializable
    public AuthUserSession? Session { get; set; }
    public List<JsCookie> Cookies { get; protected set; } = new();
    public string? CookiesHeader { get; protected set; }
    public virtual void Load(InitialHostState? hostState)
    {
        if (hostState != null)
        {
            Session = hostState.Session;
            Load(hostState.Cookies);
        }
    }

    public virtual void Load(List<JsCookie> cookies)
    {
        Cookies = cookies;
        CookiesHeader = cookies.Count > 0
            ? string.Join("; ", cookies.Select(x => $"{x.Name}={x.Value.UrlEncode()}"))
            : null;
    }


}

public class BlazorServerClient : JsonApiClient
{
    HostState HostState { get; }
    public BlazorServerClient(HttpClient httpClient, HostState hostState) : base(httpClient)
    {
        HostState = hostState;
        if (HostState.CookiesHeader != null)
        {
            this.AddHeader(HttpHeaders.Cookie, HostState.CookiesHeader);
        }
    }
}


// Scopes services doesn't work for Delegating Handler's
// https://andrewlock.net/understanding-scopes-with-ihttpclientfactory-message-handlers/
// Uses IHttpContextAccessor to fetch token https://auth0.com/blog/call-protected-api-in-aspnet-core/
public class CookieHandler : DelegatingHandler, IDisposable
{
    HostState HostState;
    ILogger<CookieHandler> Log;
    
    public static Func<DelegatingHandler, HttpRequestMessage, CancellationToken, Task>? Filter { get; set; }

    public CookieHandler(HostState hostState, ILogger<CookieHandler> log)
    {
        HostState = hostState;
        Log = log;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (HostState.CookiesHeader != null)
        {
            request.Headers.Add(HttpHeaders.Cookie, HostState.CookiesHeader);
            if (Log.IsEnabled(LogLevel.Debug))
            {
                Log.LogDebug("Added {0} Cookies to HttpClient request: {1}", HostState.Cookies.Count, string.Join(',', HostState.Cookies.Select(x => x.Name)));
            }
            if (Filter != null)
            {
                await Filter(this, request, cancellationToken);
            }
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

// Prevent breaking sub classes when adding new dependencies with single dependency
public class BlazorServerAuthContext
{
    public JsonApiClient Client { get; }
    public IServiceGateway Gateway { get; }
    public HostState HostState { get; }
    public NavigationManager NavigationManager { get; }
    public IJSRuntime JS { get; }
    public BlazorServerAuthContext(
        JsonApiClient client, 
        IServiceGateway gateway,
        HostState hostState,
        NavigationManager navigationManager, 
        IJSRuntime js)
    {
        Client = client;
        Gateway = gateway;
        HostState = hostState;
        NavigationManager = navigationManager;
        JS = js;
    }
}

public class BlazorServerAuthenticationStateProvider : AuthenticationStateProvider
{
    protected ILogger<BlazorServerAuthenticationStateProvider> Log { get; }
    protected JsonApiClient Client { get; }
    protected IServiceGateway Gateway { get; }
    public HostState HostState { get; }
    protected NavigationManager NavigationManager { get; }
    protected IJSRuntime JS { get; }

    public BlazorServerAuthenticationStateProvider(BlazorServerAuthContext context, ILogger<BlazorServerAuthenticationStateProvider> log)
    {
        Log = log;
        Client = context.Client;
        Gateway = context.Gateway;
        HostState = context.HostState;
        NavigationManager = context.NavigationManager;
        JS = context.JS;
    }

    protected AuthenticationState UnAuthenticationState => new(new ClaimsPrincipal(new ClaimsIdentity()));

    public const string AuthenticationType = "Server Authentication";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            if (HostState.Session?.IsAuthenticated != true)
                return UnAuthenticationState;

            var session = HostState.Session!;
            if (session.UserAuthId == null || session.DisplayName == null || session.UserName == null)
            {
                Log.LogWarning("User #{0} {1}, {2} is incomplete", session.UserAuthId, session.UserName, session.DisplayName);
                return UnAuthenticationState;
            }

            var authRepo = HostContext.AppHost.GetAuthRepositoryAsync();
            var claims = await session.AsClaimsAsync(authRepo);
            using (authRepo as IDisposable) {}

            await OnAuthenticationStateClaimsAsync(claims);

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

    public virtual async Task LogoutIfAuthenticatedAsync(string? redirectTo = null)
    {
        var authState = await GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
            await LogoutAsync(redirectTo: redirectTo);
        else if (redirectTo != null)
            NavigationManager.NavigateTo(redirectTo);
    }

    public virtual Task LogoutAsync(string? redirectTo = null)
    {
        HostState.Session = HostContext.GetPlugin<AuthFeature>()?.SessionFactory() as AuthUserSession;
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
            var apiReq = ((IRequiresRequest)Gateway).Request;
            var authResult = await Gateway.SendAsync(request);
            var httpCookies = ((IHttpResponse)apiReq.Response).Cookies.Collection;

            if (httpCookies != null)
            {
                var jsCookies = httpCookies.Map(x => x.ToJsCookie());
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

public interface IComponentRenderer
{
    Task<string> RenderComponentAsync<T>(HttpContext httpContext, Dictionary<string, object>? args = null);
    Task<string> RenderComponentAsync(Type type, HttpContext httpContext, Dictionary<string, object>? args = null);
}

public class ComponentRenderer : IComponentRenderer
{
    public Task<string> RenderComponentAsync<T>(HttpContext httpContext, Dictionary<string, object>? args = null) =>
        RenderComponentAsync(typeof(T), httpContext, args);

    public async Task<string> RenderComponentAsync(Type componentType, HttpContext httpContext, Dictionary<string, object>? args = null)
    {
        var componentArgs = new Dictionary<string, object>();
        if (args != null)
        {
            var accessors = TypeProperties.Get(componentType);
            foreach (var entry in args)
            {
                var prop = accessors.GetPublicProperty(entry.Key);
                if (prop == null)
                    continue;

                var value = entry.Value.ConvertTo(prop.PropertyType);
                componentArgs[prop.Name] = value;
            }
        }

        var componentTagHelper = new ComponentTagHelper
        {
            ComponentType = componentType,
            RenderMode = RenderMode.Static,
            Parameters = componentArgs,
            ViewContext = new ViewContext { HttpContext = httpContext },
        };

        var objArgs = new Dictionary<object, object>();
        var tagHelperContext = new TagHelperContext(
            new TagHelperAttributeList(),
            objArgs,
            "uniqueid");

        var tagHelperOutput = new TagHelperOutput(
            "tagName",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        await componentTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

        using var stringWriter = new StringWriter();

        tagHelperOutput.Content.WriteTo(stringWriter, HtmlEncoder.Default);

        return stringWriter.ToString();
    }
}

#endif
