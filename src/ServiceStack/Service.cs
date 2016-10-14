﻿using System;
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

        [Obsolete("Use Gateway")]
        public object ExecuteRequest(object requestDto)
        {
            return HostContext.ServiceController.Execute(requestDto, Request);
        }

        public IRequest Request { get; set; }

        protected virtual IResponse Response
        {
            get { return Request != null ? Request.Response : null; }
        }

        private ICacheClient cache;
        public virtual ICacheClient Cache
        {
            get { return cache ?? (cache = HostContext.AppHost.GetCacheClient(Request)); }
        }

        private MemoryCacheClient localCache;
        /// <summary>
        /// Returns <see cref="MemoryCacheClient"></see>. cache is only persisted for this running app instance.
        /// </summary>
        public virtual MemoryCacheClient LocalCache
        {
            get { return localCache ?? (localCache = HostContext.AppHost.GetMemoryCacheClient(Request)); }
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


        private IServiceGateway gateway;
        public virtual IServiceGateway Gateway
        {
            get { return gateway ?? (gateway = HostContext.AppHost.GetServiceGateway(Request)); }
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
            if (HostContext.TestMode)
            {
                var mockSession = TryResolve<TUserSession>();
                if (!Equals(mockSession, default(TUserSession)))
                    mockSession = TryResolve<IAuthSession>() is TUserSession 
                        ? (TUserSession)TryResolve<IAuthSession>() 
                        : default(TUserSession);

                if (!Equals(mockSession, default(TUserSession)))
                    return mockSession;
            }

            return SessionFeature.GetOrCreateSession<TUserSession>(Cache, Request, Response);
        }

        /// <summary>
        /// If user found in session for this request is authenticated.
        /// </summary>
        public virtual bool IsAuthenticated
        {
            get { return this.GetSession().IsAuthenticated; }
        }

        /// <summary>
        /// Publish a MQ message over the <see cref="IMessageProducer"></see> implementation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        public virtual void PublishMessage<T>(T message)
        {
            if (MessageProducer == null)
                throw new NullReferenceException("No IMessageFactory was registered, cannot PublishMessage");

            MessageProducer.Publish(message);
        }

        /// <summary>
        /// Disposes all created disposable properties of this service
        /// and executes disposing of all request <see cref="IDposable"></see>s 
        /// (warning, manualy triggering this might lead to unwanted disposing of all request related objects and services.)
        /// </summary>
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
