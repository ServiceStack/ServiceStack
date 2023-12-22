using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Generic + Useful IService base class
/// </summary>
public class Service : IService, IServiceBase, IDisposable, IServiceFilters
    , IAsyncDisposable
{
    public static IResolver GlobalResolver { get; set; }

    private IResolver resolver;
    public virtual IResolver GetResolver() => resolver ?? GlobalResolver;

    public virtual Service SetResolver(IResolver resolver)
    {
        this.resolver = resolver;
        return this;
    }

    public virtual T TryResolve<T>()
    {
        return this.GetResolver() == null
            ? default(T)
            : this.GetResolver().TryResolve<T>();
    }

    public T GetPlugin<T>() where T : class, IPlugin  => GetResolver()?.TryResolve<T>() ?? HostContext.GetPlugin<T>();
    public T AssertPlugin<T>() where T : class, IPlugin  => GetResolver()?.TryResolve<T>() ?? HostContext.AssertPlugin<T>();

    public virtual T ResolveService<T>()
    {
        var service = TryResolve<T>();
        return HostContext.ResolveService(this.Request, service);
    }

    public IRequest Request { get; set; }

    protected virtual IResponse Response => Request?.Response;

    private ICacheClient cache;
    public virtual ICacheClient Cache => cache ??= HostContext.AppHost.GetCacheClient(Request);

    private ICacheClientAsync cacheAsync;
    public virtual ICacheClientAsync CacheAsync => cacheAsync ??= HostContext.AppHost.GetCacheClientAsync(Request);

    private MemoryCacheClient localCache;
    /// <summary>
    /// Returns <see cref="MemoryCacheClient"></see>. cache is only persisted for this running app instance.
    /// </summary>
    public virtual MemoryCacheClient LocalCache => localCache ??= HostContext.AppHost.GetMemoryCacheClient(Request);

    public virtual IDbConnection OpenDbConnection(string namedConnection) => HostContext.AppHost.GetDbConnection(namedConnection);

    private IDbConnection db;
    public virtual IDbConnection Db => db ??= HostContext.AppHost.GetDbConnection(Request);

    private IRedisClient redis;
    public virtual IRedisClient Redis => redis ??= HostContext.AppHost.GetRedisClient(Request);
        
    public virtual ValueTask<IRedisClientAsync> GetRedisAsync() => HostContext.AppHost.GetRedisClientAsync(Request);

    private IMessageProducer messageProducer;
    public virtual IMessageProducer MessageProducer => messageProducer ??= HostContext.AppHost.GetMessageProducer(Request);

    private ISessionFactory sessionFactory;
    public virtual ISessionFactory SessionFactory => sessionFactory ?? (sessionFactory = TryResolve<ISessionFactory>()) ?? new SessionFactory(Cache, CacheAsync);

    private IAuthRepository authRepository;
    public virtual IAuthRepository AuthRepository => authRepository ??= HostContext.AppHost.GetAuthRepository(Request);

    private IAuthRepositoryAsync authRepositoryAsync;
    public virtual IAuthRepositoryAsync AuthRepositoryAsync => authRepositoryAsync ??= HostContext.AppHost.GetAuthRepositoryAsync(Request);

    private IServiceGateway gateway;
    public virtual IServiceGateway Gateway => gateway ??= HostContext.AppHost.GetServiceGateway(Request);

    /// <summary>
    /// Cascading collection of virtual file sources, inc. Embedded Resources, File System, In Memory, S3
    /// </summary>
    public IVirtualPathProvider VirtualFileSources => HostContext.VirtualFileSources;

    /// <summary>
    /// Read/Write Virtual FileSystem. Defaults to FileSystemVirtualPathProvider
    /// </summary>
    public IVirtualFiles VirtualFiles => HostContext.VirtualFiles;

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
        return req.GetSessionAsync(reload, token);
    }

    /// <summary>
    /// Typed UserSession
    /// </summary>
    protected virtual TUserSession SessionAs<TUserSession>()
    {
        if (HostContext.TestMode)
        {
            var mockSession = TryResolve<TUserSession>();
            if (Equals(mockSession, default(TUserSession)))
                mockSession = TryResolve<IAuthSession>() is TUserSession 
                    ? (TUserSession)TryResolve<IAuthSession>() 
                    : default;

            if (!Equals(mockSession, default(TUserSession)))
                return mockSession;
        }

        return SessionFeature.GetOrCreateSession<TUserSession>(Cache, Request, Response);
    }

    /// <summary>
    /// Typed UserSession
    /// </summary>
    protected virtual async Task<TUserSession> SessionAsAsync<TUserSession>()
    {
        if (HostContext.TestMode)
        {
            var mockSession = TryResolve<TUserSession>();
            if (Equals(mockSession, default(TUserSession)))
                mockSession = TryResolve<IAuthSession>() is TUserSession 
                    ? (TUserSession)TryResolve<IAuthSession>() 
                    : default;

            if (!Equals(mockSession, default(TUserSession)))
                return mockSession;
        }

        return await SessionFeature.GetOrCreateSessionAsync<TUserSession>(CacheAsync, Request, Response);
    }

    /// <summary>
    /// If user found in session for this request is authenticated.
    /// </summary>
    public virtual bool IsAuthenticated => this.GetSession().IsAuthenticated;

    /// <summary>
    /// Publish a MQ message over the <see cref="IMessageProducer"></see> implementation.
    /// </summary>
    public virtual void PublishMessage<T>(T message) => HostContext.AppHost.PublishMessage(MessageProducer, message);

    private bool hasDisposed = false;
    /// <summary>
    /// Disposes all created disposable properties of this service
    /// and executes disposing of all request <see cref="IDisposable"></see>s 
    /// (warning, manually triggering this might lead to unwanted disposing of all request related objects and services.)
    /// </summary>
    public virtual void Dispose()
    {
        if (hasDisposed) return;
        hasDisposed = true;
        using (authRepository as IDisposable) { }
        db?.Dispose();
        redis?.Dispose();
        messageProducer?.Dispose();
        RequestContext.Instance.ReleaseDisposables();

        Request.ReleaseIfInProcessRequest();
    }

    public virtual void OnBeforeExecute(object requestDto) {}
    public virtual object OnAfterExecute(object response) => response;
    public virtual Task<object> OnExceptionAsync(object requestDto, Exception ex) => TypeConstants.EmptyTask;
        
    public async ValueTask DisposeAsync()
    {
        if (hasDisposed) return;
        await using (authRepositoryAsync as IAsyncDisposable) {}
        Dispose();
    }
}