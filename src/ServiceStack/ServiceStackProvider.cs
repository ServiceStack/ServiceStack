using System;
using System.Data;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Web;

namespace ServiceStack
{
    //implemented by PageBase and 
    public interface IHasServiceStackProvider
    {
        IServiceStackProvider ServiceStackProvider { get; }
    }

    public interface IServiceStackProvider : IDisposable
    {
        void SetResolver(IResolver resolver);
        IResolver GetResolver();
        IHttpRequest Request { get; }
        IHttpResponse Response { get; }
        ICacheClient Cache { get; }
        IDbConnection Db { get; }
        IRedisClient Redis { get; }
        IMessageFactory MessageFactory { get; set; }
        IMessageProducer MessageProducer { get; }
        ISessionFactory SessionFactory { get; }
        ISession SessionBag { get; }
        bool IsAuthenticated { get; }
        T TryResolve<T>();
        T ResolveService<T>();
        IAuthSession GetSession(bool reload = false);
        TUserSession SessionAs<TUserSession>();
        void ClearSession();
        void PublishMessage<T>(T message);
    }

    //Add extra functionality common to ASP.NET ServiceStackPage or ServiceStackController
    public static class ServiceStackProviderExtensions
    {
    }

    public class ServiceStackProvider : IServiceStackProvider
    {
        public ServiceStackProvider(IHttpRequest request, IResolver resolver = null)
        {
            this.request = request;
            this.resolver = resolver ?? Service.GlobalResolver ??  HostContext.AppHost;
        }

        private IResolver resolver;
        public virtual void SetResolver(IResolver resolver)
        {
            this.resolver = resolver;
        }

        public virtual IResolver GetResolver()
        {
            return resolver;
        }

        private readonly IHttpRequest request;
        public virtual IHttpRequest Request
        {
            get { return request; }
        }

        public virtual IHttpResponse Response
        {
            get { return (IHttpResponse)Request.Response; }
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
                requiresContext.Request = Request;
            }
            return service;
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
        /// Typed UserSession
        /// </summary>
        public virtual TUserSession SessionAs<TUserSession>()
        {
            var ret = TryResolve<TUserSession>();
            return !Equals(ret, default(TUserSession))
                ? ret
                : Cache.SessionAs<TUserSession>(Request, Response);
        }

        public virtual void ClearSession()
        {
            Cache.ClearSession();
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
        }
    }
}