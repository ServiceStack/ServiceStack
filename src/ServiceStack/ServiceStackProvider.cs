using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
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
        IAppSettings AppSettings { get; }
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
        object Execute(object requestDto);
        object Execute(IRequest request);
        IAuthSession GetSession(bool reload = false);
        TUserSession SessionAs<TUserSession>();
        void ClearSession();
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
            return authSession != null && authSession.IsAuthenticated;
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
            if (authSession == null || !authSession.IsAuthenticated)
                return false;

            var httpReq = hasProvider.ServiceStackProvider.Request;
            var userAuthRepo = httpReq.TryResolve<IAuthRepository>();
            var hasRoles = roleAttrs.All(x => x.HasAllRoles(httpReq, authSession, userAuthRepo));
            if (!hasRoles)
                return false;

            var hasAnyRole = anyRoleAttrs.All(x => x.HasAnyRoles(httpReq, authSession, userAuthRepo));
            if (!hasAnyRole)
                return false;

            var hasPermssions = permAttrs.All(x => x.HasAllPermissions(httpReq, authSession, userAuthRepo));
            if (!hasPermssions)
                return false;

            var hasAnyPermission = anyPermAttrs.All(x => x.HasAnyPermissions(httpReq, authSession, userAuthRepo));
            if (!hasAnyPermission)
                return false;

            return true;
        }
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

        public IAppSettings AppSettings
        {
            get { return HostContext.AppSettings; }
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
            return HostContext.ResolveService(Request, service);
        }

        public object Execute(object requestDto)
        {
            return HostContext.ServiceController.Execute(requestDto, Request);
        }

        public object Execute(IRequest request)
        {
            return HostContext.ServiceController.Execute(Request);
        }

        public object ForwardRequest()
        {
            return HostContext.ServiceController.Execute(Request);
        }

        private ICacheClient cache;
        public virtual ICacheClient Cache
        {
            get
            {
                return cache ?? (cache = HostContext.AppHost.GetCacheClient());
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
                : SessionFeature.GetOrCreateSession<TUserSession>(Cache, Request, Response);
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