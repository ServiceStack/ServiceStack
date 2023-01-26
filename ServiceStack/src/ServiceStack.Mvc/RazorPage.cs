#nullable enable
#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Web;
using ActionContext = Microsoft.AspNetCore.Mvc.ActionContext;
using HtmlString = Microsoft.AspNetCore.Html.HtmlString;

namespace ServiceStack.Mvc;

public class RazorPageConfig
{
    public string? ForbiddenRedirect { get; set; } = "/Error?code=403";
    public string? ForbiddenPartial { get; set; }
}

// View Page to support ASP.Net Razor Pages
public abstract class RazorPage : Microsoft.AspNetCore.Mvc.RazorPages.Page, IDisposable
{
    public static RazorPageConfig Config { get; set; } = new(); 
    
    public HttpContext GetHttpContext() => base.HttpContext ?? base.ViewContext.HttpContext;

    public override ViewContext ViewContext
    {
        get => base.ViewContext;
        set
        {
            base.ViewContext = value;
            if (base.PageContext == null) //can be null
            {
                var httpCtx = GetHttpContext();
                var actionContext = new ActionContext(httpCtx,
                    httpCtx.GetRouteData(),
                    ViewContext.ActionDescriptor);
                RazorPagesEngine.PopulateRazorPageContext(httpCtx, this, value.ViewData, actionContext);
            }
        }
    }

    public IHttpRequest HttpRequest
    {
        get
        {
            if (base.ViewContext.ViewData.TryGetValue(Keywords.IRequest, out var oRequest)
                || GetHttpContext()?.Items.TryGetValue(Keywords.IRequest, out oRequest) == true)
                return (IHttpRequest)oRequest!;

            return AppHostBase.GetOrCreateRequest(HttpContext) as IHttpRequest
                ?? new BasicHttpRequest();
        }
    }

    public IHttpResponse HttpResponse => (IHttpResponse)HttpRequest.Response;

    public string GetLayout(string defaultLayout) => ViewContext.ViewData["Layout"] as string ?? defaultLayout;

    public bool IsError => ModelError != null || GetErrorStatus() != null;

    public object? ModelError { get; set; }

    public bool IsPostBack => this.HttpRequest.Verb == HttpMethods.Post;

    public ResponseStatus? GetErrorStatus()
    {
        var errorStatus = this.HttpRequest.GetItem(Keywords.ErrorStatus);
        return errorStatus as ResponseStatus
               ?? GetResponseStatus(ModelError);
    }

    private static ResponseStatus? GetResponseStatus(object? response)
    {
        if (response == null)
            return null;

        if (response is ResponseStatus status)
            return status;

        if (response is IHasResponseStatus hasResponseStatus)
            return hasResponseStatus.ResponseStatus;

        var propertyInfo = response.GetType().GetProperty("ResponseStatus");
        return propertyInfo?.GetProperty(response) as ResponseStatus;
    }

    public HtmlString? GetErrorMessage()
    {
        var errorStatus = GetErrorStatus();
        return errorStatus == null ? null : new HtmlString(errorStatus.Message);
    }

    public HtmlString GetAbsoluteUrl(string virtualPath)
    {
        return new HtmlString(AppHost.ResolveAbsoluteUrl(virtualPath, HttpRequest));
    }

    public void ApplyRequestFilters(object requestDto)
    {
        HostContext.ApplyRequestFiltersAsync(HttpRequest, HttpResponse, requestDto).Wait();
        if (HttpResponse.IsClosed)
            throw new StopExecutionException();
    }

    public HtmlString GetErrorHtml()
    {
        return new HtmlString(RazorViewExtensions.GetErrorHtml(GetErrorStatus()) ?? "");
    }

    public string PathBase => AppHost.Config.PathBase;
    public IVirtualFiles VirtualFiles => HostContext.VirtualFiles;
    public IVirtualPathProvider VirtualFileSources => HostContext.VirtualFileSources;

    public IAppHost AppHost => ServiceStackHost.Instance;

    public bool DebugMode => HostContext.DebugMode;

    public virtual TPlugin GetPlugin<TPlugin>() where TPlugin : class, IPlugin =>
        HostContext.AppHost.GetPlugin<TPlugin>();

    private IServiceStackProvider? provider;
    public virtual IServiceStackProvider ServiceStackProvider => provider ??= new ServiceStackProvider(HttpRequest);

    public virtual IAppSettings AppSettings => ServiceStackProvider.AppSettings;

    public virtual IHttpRequest ServiceStackRequest => ServiceStackProvider.Request;

    public virtual IHttpResponse ServiceStackResponse => ServiceStackProvider.Response;

    public virtual ICacheClient Cache => ServiceStackProvider.Cache;

    public virtual ICacheClientAsync CacheAsync => ServiceStackProvider.CacheAsync;

    public virtual IDbConnection Db => ServiceStackProvider.Db;

    public virtual IRedisClient Redis => ServiceStackProvider.Redis;

    public virtual ValueTask<IRedisClientAsync> GetRedisAsync() => ServiceStackProvider.GetRedisAsync();

    public virtual IMessageProducer MessageProducer => ServiceStackProvider.MessageProducer;

    public virtual IAuthRepository AuthRepository => ServiceStackProvider.AuthRepository;

    public virtual IAuthRepositoryAsync AuthRepositoryAsync => ServiceStackProvider.AuthRepositoryAsync;

    public virtual ISessionFactory SessionFactory => ServiceStackProvider.SessionFactory;

    public virtual Caching.ISession SessionBag => ServiceStackProvider.SessionBag;

    public virtual Caching.ISessionAsync SessionBagAsync => ServiceStackProvider.SessionBagAsync;

    public virtual bool IsAuthenticated => ServiceStackProvider.IsAuthenticated;

    public virtual IAuthSession GetSession(bool reload = false) => ServiceStackProvider.GetSession(reload);

    public virtual Task<IAuthSession> GetSessionAsync(bool reload = false, CancellationToken token = default) =>
        ServiceStackProvider.GetSessionAsync(reload, token);

    public virtual IAuthSession UserSession => GetSession();

    public virtual TUserSession SessionAs<TUserSession>() => ServiceStackProvider.SessionAs<TUserSession>();

    public virtual Task<TUserSession> SessionAsAsync<TUserSession>(CancellationToken token = default) =>
        ServiceStackProvider.SessionAsAsync<TUserSession>(token);

    public virtual Task SaveSessionAsync(IAuthSession session, TimeSpan? expiresIn = null,
        CancellationToken token = default) =>
        ServiceStackProvider.Request.SaveSessionAsync(session, expiresIn, token);

    public virtual void ClearSession() => ServiceStackProvider.ClearSession();

    public virtual Task ClearSessionAsync(CancellationToken token = default) =>
        ServiceStackProvider.ClearSessionAsync(token);

    public virtual TDependency TryResolve<TDependency>() => ServiceStackProvider.TryResolve<TDependency>();

    public virtual TService ResolveService<TService>() => ServiceStackProvider.ResolveService<TService>();

    public virtual object ForwardRequestToServiceStack(IRequest request = null) =>
        ServiceStackProvider.Execute(request ?? ServiceStackProvider.Request);

    public virtual IServiceGateway Gateway => ServiceStackProvider.Gateway;

    public virtual async Task<HtmlString> ApiAsJsonAsync<TResponse>(IReturn<TResponse> request)
    {
        return (await Gateway.ApiAsync(request).ConfigAwait()).Response.AsRawJson();
    }
    
    public void Dispose()
    {
        if (provider == null)
            return;

        provider?.Dispose();
        provider = null;
        EndServiceStackRequest();
    }

    public virtual void EndServiceStackRequest() => HostContext.AppHost.OnEndRequest(HttpRequest);

    public bool RenderErrorIfAny()
    {
        var html = GetErrorHtml(GetErrorStatus());
        if (html == null)
            return false;

        WriteLiteral(html);

        return true;
    }

    private string? GetErrorHtml(ResponseStatus? responseStatus)
    {
        if (responseStatus == null) return null;

        var stackTrace = responseStatus.StackTrace != null
            ? "<pre>" + responseStatus.StackTrace + "</pre>"
            : "";

        var html = @"
            <div id=""error-response"" class=""alert alert-danger"">
                <h4>" +
                   responseStatus.ErrorCode + ": " +
                   responseStatus.Message + @"
                </h4>" +
                   stackTrace +
                   "</div>";
        return html;
    }

    public async Task RedirectIfNotAuthenticatedAsync(string? redirect = null)
    {
        var req = HttpRequest;
        if ((await req.GetSessionAsync()).IsAuthenticated)
            return;

        req.RedirectUnauthenticated(redirect);
    }

    public async Task RedirectToAsync(string path)
    {
        await HttpRequest.RedirectToAsyncInternalAsync(path).ConfigAwait();
    }
}

public static class RazorPageHtmlExtensions
{
    public static string GetReturnUrl(this IHtmlHelper html) => html.GetRequest().GetReturnUrl() ?? "/";
    public static RazorPage? TryGetRazorPage(this IHtmlHelper html) => html.ViewContext?.ViewData?.Model as RazorPage;
    public static RazorPage GetRazorPage(this IHtmlHelper html) => html.ViewContext?.ViewData?.Model as RazorPage
        ?? throw new NotSupportedException("Only available from a ServiceStack.Mvc.RazorPage");

    public static async Task<bool> IsAuthenticatedAsync(this IHtmlHelper html) =>
        (await html.GetRequest().GetSessionAsync().ConfigAwait()).IsAuthenticated;
    
    public static async Task<bool> EnsureAuthenticatedAsync(this IHtmlHelper html, string? redirect = null)
    {
        var req = html.GetRequest();
        var session = await req.GetSessionAsync().ConfigAwait();
        if (!session.IsAuthenticated)
        {
            req.RedirectUnauthenticated(redirect);
            return false;
        }
        return true;
    }

    public static async Task<bool> HasRoleAsync(this IHtmlHelper html, string role)
    {
        var req = html.GetRequest();
        var session = await req.GetSessionAsync().ConfigAwait();
        if (!session.IsAuthenticated)
            return false;
        return await session.HasRoleAsync(role, req.TryResolve<IAuthRepositoryAsync>()).ConfigAwait();
    }
    
    public static async Task<bool> EnsureRoleAsync(this IHtmlHelper html, string role, string? message = null, string? redirect = null)
    {
        var req = html.GetRequest();
        var session = await req.GetSessionAsync().ConfigAwait();
        if (!session.IsAuthenticated)
        {
            req.RedirectUnauthenticated(redirect);
            return false;
        }

        if (!await session.HasRoleAsync(role, req.TryResolve<IAuthRepositoryAsync>()))
        {
            if (redirect != null)
            {
                await req.RedirectToAsyncInternalAsync(redirect).ConfigAwait();
                return false;
            }

            if (RazorPage.Config.ForbiddenPartial != null)
            {
                message ??= $"Missing Role {role}";
                await html.RenderPartialAsync(RazorPage.Config.ForbiddenPartial, message).ConfigAwait();
            }
            else if (RazorPage.Config.ForbiddenRedirect != null)
            {
                var url = RazorPage.Config.ForbiddenRedirect.AddQueryParam("role", role);
                await req.RedirectToAsyncInternalAsync(url).ConfigAwait();
            }
            else
            {
                var error = new HttpError(HttpStatusCode.Forbidden, message ?? ErrorMessages.InvalidRole.Localize(req));
                await req.Response.WriteToResponse(req, error).ConfigAwait();
            }
            return false;
        }
        return true;
    }
    
    public static async Task<bool> HasPermissionAsync(this IHtmlHelper html, string role)
    {
        var req = html.GetRequest();
        var session = await req.GetSessionAsync().ConfigAwait();
        if (!session.IsAuthenticated)
            return false;
        return await session.HasPermissionAsync(role, req.TryResolve<IAuthRepositoryAsync>()).ConfigAwait();
    }
    
    public static async Task<bool> EnsurePermissionAsync(this IHtmlHelper html, string permission, string? message = null, string? redirect = null) 
    {
        var req = html.GetRequest();
        var session = await req.GetSessionAsync().ConfigAwait();
        if (!session.IsAuthenticated)
        {
            await req.RedirectToAsyncInternalAsync(redirect).ConfigAwait();
            return false;
        }

        if (!await session.HasPermissionAsync(permission, req.TryResolve<IAuthRepositoryAsync>()))
        {
            if (redirect != null)
            {
                await req.RedirectToAsyncInternalAsync(redirect).ConfigAwait();
                return false;
            }

            if (RazorPage.Config.ForbiddenPartial != null)
            {
                message ??= $"Missing Permission {permission}";
                await html.RenderPartialAsync(RazorPage.Config.ForbiddenPartial, message).ConfigAwait();
            }
            else if (RazorPage.Config.ForbiddenRedirect != null)
            {
                var url = RazorPage.Config.ForbiddenRedirect.AddQueryParam("permission", permission);
                await req.RedirectToAsyncInternalAsync(url).ConfigAwait();
            }
            else
            {
                var error = new HttpError(HttpStatusCode.Forbidden, message ?? ErrorMessages.InvalidRole.Localize(req));
                await req.Response.WriteToResponse(req, error).ConfigAwait();
            }
            return false;
        }
        return true;
    }

    public static IHtmlContent ImportMap(this IHtmlHelper html, Dictionary<string, (string Dev, string Prod)> importMaps)
    {
        var map = new Dictionary<string, object>();
        var imports = new Dictionary<string, object> { ["imports"] = map };
        var isDev = HostContext.AppHost.IsDevelopmentEnvironment();
        foreach (var importMap in importMaps)
        {
            map[importMap.Key] = isDev ? importMap.Value.Dev : importMap.Value.Prod;
        }
        var script = $"<script type=\"importmap\">\n{imports.ToJson().IndentJson()}\n</script>";
        return html.Raw(script);
    }
    
    public static IHtmlContent StaticImportMap(this IHtmlHelper html, Dictionary<string, string> importMaps)
    {
        var to = new Dictionary<string, (string Dev, string Prod)>();
        foreach (var entry in importMaps)
        {
            to[entry.Key] = (entry.Value, entry.Value);
        }
        return html.ImportMap(to);
    }

    public static string ClassNames(this IHtmlHelper html, params string?[] classes) => CssUtils.ClassNames(classes);
}
#endif
