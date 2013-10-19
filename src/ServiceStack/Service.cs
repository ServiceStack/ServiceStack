using System;
using System.Data;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
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
            var requiresContext = service as IRequiresRequest;
            if (requiresContext != null)
            {
                requiresContext.Request = this.Request;
            }
            return service;
        }

        public IRequest Request { get; set; }

        protected virtual IResponse Response
        {
            get { return Request != null ? Request.Response : null; }
        }

        private ICacheClient cache;
        public virtual ICacheClient Cache
        {
            get
            {
                return cache ??
                    (cache = TryResolve<ICacheClient>()) ??
                    (cache = (TryResolve<IRedisClientsManager>() != null ? TryResolve<IRedisClientsManager>().GetCacheClient() : null));
            }
        }

        private IDbConnection db;
        public virtual IDbConnection Db
        {
            get { return db ?? (db = TryResolve<IDbConnectionFactory>().OpenDbConnection()); }
        }

        private IRedisClient redis;
        public virtual IRedisClient Redis
        {
            get { return redis ?? (redis = TryResolve<IRedisClientsManager>().GetClient()); }
        }

        private IMessageFactory messageFactory;
        public virtual IMessageFactory MessageFactory
        {
            get { return messageFactory ?? (messageFactory = TryResolve<IMessageFactory>()); }
            set { messageFactory = value; }
        }

        private IMessageProducer messageProducer;
        public virtual IMessageProducer MessageProducer
        {
            get { return messageProducer ?? (messageProducer = MessageFactory.CreateMessageProducer()); }
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
