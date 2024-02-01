#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Web;

namespace ServiceStack;

//implemented by PageBase and 
public interface IHasServiceStackProvider
{
    IServiceStackProvider ServiceStackProvider { get; }
}

public interface IServiceStackProvider : IDisposable
{
    void SetResolver(IResolver resolver);
    IResolver GetResolver();
    IAppSettings AppSettings { get; }
    IHttpRequest Request { get; }
    IHttpResponse Response { get; }
    ICacheClient Cache { get; }
    ICacheClientAsync CacheAsync { get; }
    IDbConnection Db { get; }
    IRedisClient Redis { get; }
    ValueTask<IRedisClientAsync> GetRedisAsync();
    IMessageProducer MessageProducer { get; }
    IAuthRepository AuthRepository { get; }
    IAuthRepositoryAsync AuthRepositoryAsync { get; }
    ISessionFactory SessionFactory { get; }
    ISession SessionBag { get; }
    ISessionAsync SessionBagAsync { get; }
    bool IsAuthenticated { get; }
    IAuthSession GetSession(bool reload = false);
    Task<IAuthSession> GetSessionAsync(bool reload = false, CancellationToken token=default);
    TUserSession SessionAs<TUserSession>();
    Task<TUserSession> SessionAsAsync<TUserSession>(CancellationToken token=default);
    void ClearSession();
    Task ClearSessionAsync(CancellationToken token=default);
    T TryResolve<T>();
    T ResolveService<T>() where T : class, IService;

    IServiceGateway Gateway { get; }
        
    RpcGateway RpcGateway { get; }

    object Execute(IRequest request);

    [Obsolete("Use Gateway")]
    object Execute(object requestDto);

    [Obsolete("Use Gateway")]
    TResponse Execute<TResponse>(IReturn<TResponse> requestDto);

    [Obsolete("Use Gateway")]
    void PublishMessage<T>(T message);
}

//Add extra functionality common to ASP.NET ServiceStackPage or ServiceStackController
public static class ServiceStackProviderExtensions
{
    public static bool IsAuthorized(this IHasServiceStackProvider hasProvider, AuthenticateAttribute authAttr)
    {
        if (authAttr == null)
            return true;

        var authSession = hasProvider.ServiceStackProvider.GetSession();
        return authSession is { IsAuthenticated: true };
    }

    public static bool HasAccess(
        this IHasServiceStackProvider hasProvider,
        ICollection<RequiredRoleAttribute> roleAttrs,
        ICollection<RequiresAnyRoleAttribute> anyRoleAttrs,
        ICollection<RequiredPermissionAttribute> permAttrs,
        ICollection<RequiresAnyPermissionAttribute> anyPermAttrs)
    {
        if (roleAttrs.Count + anyRoleAttrs.Count + permAttrs.Count + anyPermAttrs.Count == 0)
            return true;

        var authSession = hasProvider.ServiceStackProvider.GetSession();
        if (authSession is not { IsAuthenticated: true })
            return false;

        var httpReq = hasProvider.ServiceStackProvider.Request;
        var userAuthRepo = HostContext.AppHost.GetAuthRepository(hasProvider.ServiceStackProvider.Request);
        using (userAuthRepo as IDisposable)
        {
            var hasRoles = roleAttrs.All(x => x.HasAllRoles(httpReq, authSession, userAuthRepo));
            if (!hasRoles)
                return false;

            var hasAnyRole = anyRoleAttrs.All(x => x.HasAnyRoles(httpReq, authSession, userAuthRepo));
            if (!hasAnyRole)
                return false;

            var hasPermissions = permAttrs.All(x => x.HasAllPermissions(httpReq, authSession, userAuthRepo));
            if (!hasPermissions)
                return false;

            var hasAnyPermission = anyPermAttrs.All(x => x.HasAnyPermissions(httpReq, authSession, userAuthRepo));
            if (!hasAnyPermission)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Resolve ServiceStack Validator in external ServiceStack provider class like ServiceStackController 
    /// </summary>
    public static IValidator<T> ResolveValidator<T>(this IHasServiceStackProvider provider)
    {
        var validator = provider.ServiceStackProvider.TryResolve<IValidator<T>>();
        if (validator is IRequiresRequest requiresReq)
        {
            requiresReq.Request = provider.ServiceStackProvider.Request;
        }
        return validator;
    }
}

public class ServiceStackProvider(IHttpRequest request, IResolver resolver = null) : IServiceStackProvider
{
    private IResolver resolver = resolver ?? Service.GlobalResolver ?? HostContext.AppHost;
    public virtual void SetResolver(IResolver resolver)
    {
        this.resolver = resolver;
    }

    public virtual IResolver GetResolver()
    {
        return resolver;
    }

    public IAppSettings AppSettings => HostContext.AppSettings;

    public virtual IHttpRequest Request => request;

    public virtual IHttpResponse Response => (IHttpResponse)Request.Response;

    public virtual T TryResolve<T>()
    {
        return this.GetResolver() == null
            ? default(T)
            : this.GetResolver().TryResolve<T>();
    }

    public virtual T ResolveService<T>() where T : class, IService
    {
        var service = TryResolve<T>();
        return HostContext.ResolveService(Request, service);
    }

    private IServiceGateway gateway;
    public virtual IServiceGateway Gateway => gateway ??= HostContext.AppHost.GetServiceGateway(Request);

    private RpcGateway rpcGateway;
    public RpcGateway RpcGateway => rpcGateway ??= HostContext.AppHost.RpcGateway;

    public object Execute(object requestDto)
    {
        var response = HostContext.ServiceController.Execute(requestDto, Request);
        if (response is Exception ex)
            throw ex;

        return response;
    }

    public TResponse Execute<TResponse>(IReturn<TResponse> requestDto)
    {
        return (TResponse)Execute((object)requestDto);
    }

    public object Execute(IRequest request)
    {
        var response = HostContext.ServiceController.Execute(request, applyFilters:true);
        if (response is Exception ex)
            throw ex;

        return response;
    }

    public object ForwardRequest()
    {
        return Execute(Request);
    }

    private ICacheClient cache;
    public virtual ICacheClient Cache => cache ??= HostContext.AppHost.GetCacheClient(Request);

    private ICacheClientAsync cacheAsync;
    public virtual ICacheClientAsync CacheAsync => cacheAsync ??= HostContext.AppHost.GetCacheClientAsync(Request);

    private IDbConnection db;
    public virtual IDbConnection Db => db ??= HostContext.AppHost.GetDbConnection(Request);

    private IRedisClient redis;
    public virtual IRedisClient Redis => redis ??= HostContext.AppHost.GetRedisClient(Request);
        
    public virtual ValueTask<IRedisClientAsync> GetRedisAsync() => HostContext.AppHost.GetRedisClientAsync(Request);

    private IMessageProducer messageProducer;
    public virtual IMessageProducer MessageProducer => messageProducer ??= HostContext.AppHost.GetMessageProducer(Request);

    private IAuthRepository authRepository;
    public IAuthRepository AuthRepository => authRepository ??= HostContext.AppHost.GetAuthRepository(Request);

    private IAuthRepositoryAsync authRepositoryAsync;
    public IAuthRepositoryAsync AuthRepositoryAsync => authRepositoryAsync ??= HostContext.AppHost.GetAuthRepositoryAsync(Request);

    private ISessionFactory sessionFactory;
    public virtual ISessionFactory SessionFactory => sessionFactory ?? (sessionFactory = TryResolve<ISessionFactory>()) 
        ?? new SessionFactory(Cache, CacheAsync);

    /// <summary>
    /// Typed UserSession
    /// </summary>
    public virtual TUserSession SessionAs<TUserSession>()
    {
        return SessionFeature.GetOrCreateSession<TUserSession>(Cache, Request, Response);
    }
        
    /// <summary>
    /// Typed UserSession
    /// </summary>
    public virtual Task<TUserSession> SessionAsAsync<TUserSession>(CancellationToken token=default)
    {
        return SessionFeature.GetOrCreateSessionAsync<TUserSession>(CacheAsync, Request, Response, token);
    }

    public virtual void ClearSession()
    {
        Cache.ClearSession();
    }

    public Task ClearSessionAsync(CancellationToken token=default)
    {
        return CacheAsync.ClearSessionAsync(token: token);
    }

    /// <summary>
    /// Dynamic Session Bag
    /// </summary>
    private ISession session;
    public virtual ISession SessionBag => session ??= TryResolve<ISession>() //Easier to mock
                                                      ?? SessionFactory.GetOrCreateSession(Request, Response);

    /// <summary>
    /// Dynamic Session Bag
    /// </summary>
    private ISessionAsync sessionAsync;
    public virtual ISessionAsync SessionBagAsync => sessionAsync ??= TryResolve<ISessionAsync>() //Easier to mock
                                                                     ?? SessionFactory.GetOrCreateSessionAsync(Request, Response);

    public virtual IAuthSession GetSession(bool reload = false)
    {
        var req = this.Request;
        if (req.GetSessionId() == null)
            req.Response.CreateSessionIds(req);
        return req.GetSession(reload);
    }

    public virtual Task<IAuthSession> GetSessionAsync(bool reload = false, CancellationToken token=default)
    {
        var req = this.Request;
        if (req.GetSessionId() == null)
            req.Response.CreateSessionIds(req);
        return req.GetSessionAsync(reload, token: token);
    }

    public virtual bool IsAuthenticated => this.GetSession().IsAuthenticated;

    public virtual void PublishMessage<T>(T message) => HostContext.AppHost.PublishMessage(MessageProducer, message);

    public virtual void Dispose()
    {
        db?.Dispose();
        redis?.Dispose();
        messageProducer?.Dispose();
        using (authRepository as IDisposable) {}
    }
        
    public async ValueTask DisposeAsync()
    {
        await using (authRepositoryAsync as IAsyncDisposable) {}
    }
}