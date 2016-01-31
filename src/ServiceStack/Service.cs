using System;
using System.Data;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
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
        public virtual IResolver GetResolver()
        {
            return resolver ?? GlobalResolver;
        }

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

        public object ExecuteRequest(object requestDto)
        {
            return HostContext.ServiceController.Execute(requestDto, Request);
        }

        public IRequest Request { get; set; }

        protected virtual IResponse Response
        {
            get { return Request != null ? Request.Response : null; }
        }


        [Obsolete("Db instance now resolved from AppHost.GetDbConnection(). This will be removed in future - declare own property if needed")]
        public virtual IDbConnectionFactory DbFactory
        {
            get { return TryResolve<IDbConnectionFactory>(); }
        }

        [Obsolete("Redis instance now resolved from AppHost.GetCacheClient(). This will be removed in future - declare own property if needed")]
        public virtual IRedisClientsManager RedisManager
        {
            get { return TryResolve<IRedisClientsManager>(); }
        }

        [Obsolete("MessageProducer instance now resolved from AppHost.GetMessageProducer(). This will be removed in future - declare own property if needed")]
        private IMessageFactory messageFactory;
        public virtual IMessageFactory MessageFactory
        {
            get { return messageFactory ?? (messageFactory = TryResolve<IMessageFactory>()); }
        }


        private ICacheClient cache;
        public virtual ICacheClient Cache
        {
            get { return cache ?? HostContext.AppHost.GetCacheClient(Request); }
        }

        private IDbConnection db;
        public virtual IDbConnection Db
        {
            get { return db ?? (db = HostContext.AppHost.GetDbConnection(Request)); }
        }

        private IRedisClient redis;
        public virtual IRedisClient Redis
        {
            get { return redis ?? (redis = HostContext.AppHost.GetRedisClient(Request)); }
        }

        private IMessageProducer messageProducer;
        public virtual IMessageProducer MessageProducer
        {
            get { return messageProducer ?? (messageProducer = HostContext.AppHost.GetMessageProducer(Request)); }
        }


        private ISessionFactory sessionFactory;
        public virtual ISessionFactory SessionFactory
        {
            get { return sessionFactory ?? (sessionFactory = TryResolve<ISessionFactory>()) ?? new SessionFactory(Cache); }
        }

        /// <summary>
        /// Cascading collection of virtual file sources, inc. Embedded Resources, File System, In Memory, S3
        /// </summary>
        public IVirtualPathProvider VirtualFileSources
        {
            get { return HostContext.VirtualFileSources; }
        }

        /// <summary>
        /// Read/Write Virtual FileSystem. Defaults to FileSystemVirtualPathProvider
        /// </summary>
        public IVirtualFiles VirtualFiles
        {
            get { return HostContext.VirtualFiles; }
        }

        /// <summary>
        /// Dynamic Session Bag
        /// </summary>
        private ISession session;
        public virtual ISession SessionBag
        {
            get
            {
                return session ?? (session = TryResolve<ISession>() //Easier to mock
                    ?? SessionFactory.GetOrCreateSession(Request, Response));
            }
        }

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
            var ret = TryResolve<TUserSession>();
            return !Equals(ret, default(TUserSession))
                ? ret
                : SessionFeature.GetOrCreateSession<TUserSession>(Cache, Request, Response);
        }

        public virtual bool IsAuthenticated
        {
            get { return this.GetSession().IsAuthenticated; }
        }

        public virtual void PublishMessage<T>(T message)
        {
            if (MessageProducer == null)
                throw new NullReferenceException("No IMessageFactory was registered, cannot PublishMessage");

            MessageProducer.Publish(message);
        }

        public virtual void Dispose()
        {
            if (db != null)
                db.Dispose();
            if (redis != null)
                redis.Dispose();
            if (messageProducer != null)
                messageProducer.Dispose();

            RequestContext.Instance.ReleaseDisposables();

            Request.ReleaseIfInProcessRequest();
        }
    }

}
