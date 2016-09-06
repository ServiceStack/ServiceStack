using System;
using System.Data;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Generic + Useful IService base class
    /// </summary>
    public class Service : IService, IServiceBase, IDisposable
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

        public virtual T ResolveService<T>()
        {
            var service = TryResolve<T>();
            return HostContext.ResolveService(this.Request, service);
        }

        [Obsolete("Use Gateway")]
        public object ExecuteRequest(object requestDto)
        {
            return HostContext.ServiceController.Execute(requestDto, Request);
        }

        public IRequest Request { get; set; }

        protected virtual IResponse Response => Request?.Response;

        private ICacheClient cache;
        public virtual ICacheClient Cache => cache ?? (cache = HostContext.AppHost.GetCacheClient(Request));

        private MemoryCacheClient localCache;
        public virtual MemoryCacheClient LocalCache => localCache ?? (localCache = HostContext.AppHost.GetMemoryCacheClient(Request));

        private IDbConnection db;
        public virtual IDbConnection Db => db ?? (db = HostContext.AppHost.GetDbConnection(Request));

        private IRedisClient redis;
        public virtual IRedisClient Redis => redis ?? (redis = HostContext.AppHost.GetRedisClient(Request));

        private IMessageProducer messageProducer;
        public virtual IMessageProducer MessageProducer => messageProducer ?? (messageProducer = HostContext.AppHost.GetMessageProducer(Request));

        private ISessionFactory sessionFactory;
        public virtual ISessionFactory SessionFactory => sessionFactory ?? (sessionFactory = TryResolve<ISessionFactory>()) ?? new SessionFactory(Cache);

        private IAuthRepository authRepository;
        public virtual IAuthRepository AuthRepository => authRepository ?? (authRepository = HostContext.AppHost.GetAuthRepository(Request));


        private IServiceGateway gateway;
        public virtual IServiceGateway Gateway => gateway ?? (gateway = HostContext.AppHost.GetServiceGateway(Request));

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
        public virtual ISession SessionBag => session ?? (session = TryResolve<ISession>() //Easier to mock
            ?? SessionFactory.GetOrCreateSession(Request, Response));

        public virtual IAuthSession GetSession(bool reload = false)
        {
            var req = this.Request;
            if (req.GetSessionId() == null)
                req.Response.CreateSessionIds(req);
            return req.GetSession(reload);
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
                        : default(TUserSession);

                if (!Equals(mockSession, default(TUserSession)))
                    return mockSession;
            }

            return SessionFeature.GetOrCreateSession<TUserSession>(Cache, Request, Response);
        }

        public virtual bool IsAuthenticated => this.GetSession().IsAuthenticated;

        public virtual void PublishMessage<T>(T message)
        {
            if (MessageProducer == null)
                throw new NullReferenceException("No IMessageFactory was registered, cannot PublishMessage");

            MessageProducer.Publish(message);
        }

        public virtual void Dispose()
        {
            db?.Dispose();
            redis?.Dispose();
            messageProducer?.Dispose();
            using (authRepository as IDisposable) { }

            RequestContext.Instance.ReleaseDisposables();

            Request.ReleaseIfInProcessRequest();
        }
    }

}
