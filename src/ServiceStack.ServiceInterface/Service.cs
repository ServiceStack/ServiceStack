using System;
using System.Data;
using ServiceStack.CacheAccess;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Generic + Useful IService base class
    /// </summary>
    public class Service : IService, IRequiresRequestContext, IServiceBase, IDisposable
    {
        public IRequestContext RequestContext { get; set; }

        private IResolver resolver;
        public virtual IResolver GetResolver()
        {
            return resolver ?? EndpointHost.AppHost;
        }

        public virtual Service SetResolver(IResolver resolver)
        {
            this.resolver = resolver;
            return this;
        }

        [Obsolete("Use SetResolver")]
        public virtual Service SetAppHost(IAppHost appHost)
        {
            this.resolver = appHost;
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
            var requiresContext = service as IRequiresRequestContext;
            if (requiresContext != null)
            {
                requiresContext.RequestContext = this.RequestContext;
            }
            return service;
        }

        private IHttpRequest request;
        protected virtual IHttpRequest Request
        {
            get { return request ?? (request = RequestContext != null ? RequestContext.Get<IHttpRequest>() : TryResolve<IHttpRequest>()); }
        }

        private IHttpResponse response;
        protected virtual IHttpResponse Response
        {
            get { return response ?? (response = RequestContext != null ? RequestContext.Get<IHttpResponse>() : TryResolve<IHttpResponse>()); }
        }

        private ICacheClient cache;
        public virtual ICacheClient Cache
        {
            get { return cache ?? (cache = TryResolve<ICacheClient>()); }
        }

        private IDbConnection db;
        public virtual IDbConnection Db
        {
            get { return db ?? (db = TryResolve<IDbConnectionFactory>().Open()); }
        }

        private IRedisClient redis;
        public virtual IRedisClient Redis
        {
            get { return redis ?? (redis = TryResolve<IRedisClientsManager>().GetClient()); }
        }

        private IMessageProducer messageProducer;
        public virtual IMessageProducer MessageProducer
        {
            get { return messageProducer ?? (messageProducer = TryResolve<IMessageFactory>().CreateMessageProducer()); }
        }

        private ISessionFactory sessionFactory;
        public virtual ISessionFactory SessionFactory
        {
            get { return sessionFactory ?? (sessionFactory = TryResolve<ISessionFactory>()) ?? new SessionFactory(Cache); }
        }

        /// <summary>
        /// Dynamic Session Bag
        /// </summary>
        private ISession session;
        public virtual ISession Session
        {
            get
            {
                return session ?? (session = TryResolve<ISession>() //Easier to mock
                    ?? SessionFactory.GetOrCreateSession(Request, Response));
            }
        }

        /// <summary>
        /// Typed UserSession
        /// </summary>
        private object userSession;
        protected virtual TUserSession SessionAs<TUserSession>()
        {
            if (userSession == null)
            {
                userSession = TryResolve<TUserSession>(); //Easier to mock
                if (userSession == null)
                    userSession = Cache.SessionAs<TUserSession>(Request, Response);
            }
            return (TUserSession)userSession;
        }

        public virtual void PublishMessage<T>(T message)
        {
            //TODO: Register In-Memory IMessageFactory by default
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
        }
    }

}