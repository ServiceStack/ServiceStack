#if NETFRAMEWORK

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host.AspNet;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Web;

namespace ServiceStack.AspNet;

public class ServiceStackPage : Page, IHasServiceStackProvider
{
    public ServiceStackPage()
    {
        this.PreLoad += ServiceStack_PreLoad;
    }

    /// <summary>
    /// Default redirect URL if [Authenticate] attribute doesn't permit access.
    /// </summary>
    public virtual string UnauthorizedRedirectUrl => HostContext.GetPlugin<AuthFeature>().GetHtmlRedirect();

    /// <summary>
    /// Default redirect URL if Required Role or Permission attributes doesn't permit access.
    /// </summary>
    public virtual string ForbiddenRedirectUrl => HostContext.GetPlugin<AuthFeature>().GetHtmlRedirect();

    protected virtual void ServiceStack_PreLoad(object sender, EventArgs e)
    {
        var page = GetType();

        var authAttr = page.FirstAttribute<AuthenticateAttribute>();
        if (!this.IsAuthorized(authAttr))
        {
            var authError = authAttr?.HtmlRedirect != null
                ? authAttr.HtmlRedirect.AddQueryParam(Keywords.Redirect, Request.Url.PathAndQuery)
                : UnauthorizedRedirectUrl != null 
                    ? $"{UnauthorizedRedirectUrl}?{Keywords.Redirect}={Request.Url.PathAndQuery.UrlEncode()}#f=Unauthorized"
                    : null;

            if (authError != null)
            {
                base.Response.Redirect(authError);
            }
            else
            {
                base.Response.StatusCode = 401;
                base.Response.StatusDescription = "Unauthorized";
            }
            return;
        }

        if (!this.HasAccess(
            page.AllAttributes<RequiredRoleAttribute>(),
            page.AllAttributes<RequiresAnyRoleAttribute>(),
            page.AllAttributes<RequiredPermissionAttribute>(),
            page.AllAttributes<RequiresAnyPermissionAttribute>()))
        {
            var authError = authAttr?.HtmlRedirect != null
                ? authAttr.HtmlRedirect.AddQueryParam(Keywords.Redirect, Request.Url.PathAndQuery)
                : ForbiddenRedirectUrl != null 
                    ? $"{ForbiddenRedirectUrl}?{Keywords.Redirect}={Request.Url.PathAndQuery.UrlEncode()}#f=Forbidden"
                    : null;

            if (authError != null)
            {
                base.Response.Redirect(authError);
            }
            else
            {
                base.Response.StatusCode = 403;
                base.Response.StatusDescription = "Forbidden";
            }
        }
    }

    private IServiceStackProvider serviceStackProvider;
    public virtual IServiceStackProvider ServiceStackProvider => 
        serviceStackProvider ??= new ServiceStackProvider(
            new AspNetRequest(new HttpContextWrapper(base.Context), GetType().Name));

    public virtual IAppSettings AppSettings => ServiceStackProvider.AppSettings;

    public virtual IHttpRequest ServiceStackRequest => ServiceStackProvider.Request;

    public virtual IHttpResponse ServiceStackResponse => ServiceStackProvider.Response;

    public new virtual ICacheClient Cache => ServiceStackProvider.Cache;

    public virtual ICacheClientAsync CacheAsync => ServiceStackProvider.CacheAsync;

    public virtual IDbConnection Db => ServiceStackProvider.Db;

    public virtual IRedisClient Redis => ServiceStackProvider.Redis;
    
    public virtual ValueTask<IRedisClientAsync> GetRedisAsync() => ServiceStackProvider.GetRedisAsync();

    public virtual IMessageProducer MessageProducer => ServiceStackProvider.MessageProducer;

    public virtual IAuthRepository AuthRepository => ServiceStackProvider.AuthRepository;
    
    public virtual IAuthRepositoryAsync AuthRepositoryAsync => ServiceStackProvider.AuthRepositoryAsync;

    public virtual ISessionFactory SessionFactory => ServiceStackProvider.SessionFactory;

    public virtual ISession SessionBag => ServiceStackProvider.SessionBag;
    
    public virtual Caching.ISessionAsync SessionBagAsync => ServiceStackProvider.SessionBagAsync;

    public virtual bool IsAuthenticated => ServiceStackProvider.IsAuthenticated;

    public virtual IAuthSession GetSession(bool reload = true) => ServiceStackProvider.GetSession(reload);

    public virtual Task<IAuthSession> GetSessionAsync(bool reload = false, CancellationToken token=default) => 
        ServiceStackProvider.GetSessionAsync(reload, token);

    public virtual TUserSession SessionAs<TUserSession>() => ServiceStackProvider.SessionAs<TUserSession>();

    public virtual Task<TUserSession> SessionAsAsync<TUserSession>(CancellationToken token=default) => 
        ServiceStackProvider.SessionAsAsync<TUserSession>(token);

    [Obsolete("Use SaveSessionAsync")]
    protected virtual void SaveSession(IAuthSession session, TimeSpan? expiresIn = null) => 
        ServiceStackProvider.Request.SaveSession(session, expiresIn);

    public virtual Task SaveSessionAsync(IAuthSession session, TimeSpan? expiresIn = null, CancellationToken token=default) => 
        ServiceStackProvider.Request.SaveSessionAsync(session, expiresIn, token);

    public virtual void ClearSession() => ServiceStackProvider.ClearSession();
    
    public virtual Task ClearSessionAsync(CancellationToken token=default) => ServiceStackProvider.ClearSessionAsync(token);

    public virtual T TryResolve<T>() => ServiceStackProvider.TryResolve<T>();

    public virtual T ResolveService<T>() where T : class, IService => ServiceStackProvider.ResolveService<T>();

    public virtual object ForwardRequestToServiceStack(IRequest request = null) => 
        ServiceStackProvider.Execute(request ?? ServiceStackProvider.Request);

    public virtual IServiceGateway Gateway => ServiceStackProvider.Gateway;
    
    public virtual RpcGateway RpcGateway => ServiceStackProvider.RpcGateway;
    
    private bool hasDisposed;
    public override void Dispose()
    {
        if (hasDisposed)
            return;

        hasDisposed = true;
        base.Dispose();

        if (serviceStackProvider != null)
        {
            serviceStackProvider.Dispose();
            serviceStackProvider = null;
        }

        EndServiceStackRequest();
    }

    public virtual void EndServiceStackRequest()
    {
        HostContext.AppHost.OnEndRequest(ServiceStackRequest);
    }
}

#endif
