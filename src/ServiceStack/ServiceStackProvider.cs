using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
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
        IMessageProducer MessageProducer { get; }
        IAuthRepository AuthRepository { get; }
        ISessionFactory SessionFactory { get; }
        ISession SessionBag { get; }
        bool IsAuthenticated { get; }
        IAuthSession GetSession(bool reload = false);
        TUserSession SessionAs<TUserSession>();
        void ClearSession();
        T TryResolve<T>();
        T ResolveService<T>();

        IServiceGateway Gateway { get; }

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
            var userAuthRepo = HostContext.AppHost.GetAuthRepository(hasProvider.ServiceStackProvider.Request);
            using (userAuthRepo as IDisposable)
            {
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
    }

    public class ServiceStackProvider : IServiceStackProvider
    {
        public ServiceStackProvider(IHttpRequest request, IResolver resolver = null)
        {
            this.request = request;
            this.resolver = resolver ?? Service.GlobalResolver ?? HostContext.AppHost;
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

        public IAppSettings AppSettings => HostContext.AppSettings;

        private readonly IHttpRequest request;
        public virtual IHttpRequest Request => request;

        public virtual IHttpResponse Response => (IHttpResponse)Request.Response;

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

        private IServiceGateway gateway;
        public virtual IServiceGateway Gateway => gateway ?? (gateway = HostContext.AppHost.GetServiceGateway(Request));

        public object Execute(object requestDto)
        {
            var response = HostContext.ServiceController.Execute(requestDto, Request);
            var ex = response as Exception;
            if (ex != null)
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
            var ex = response as Exception;
            if (ex != null)
                throw ex;

            return response;
        }

        public object ForwardRequest()
        {
            return Execute(Request);
        }

        private ICacheClient cache;
        public virtual ICacheClient Cache => cache ?? (cache = HostContext.AppHost.GetCacheClient(Request));

        private IDbConnection db;
        public virtual IDbConnection Db => db ?? (db = HostContext.AppHost.GetDbConnection(Request));

        private IRedisClient redis;
        public virtual IRedisClient Redis => redis ?? (redis = HostContext.AppHost.GetRedisClient(Request));

        private IMessageProducer messageProducer;
        public virtual IMessageProducer MessageProducer => messageProducer ?? (messageProducer = HostContext.AppHost.GetMessageProducer(Request));

        private IAuthRepository authRepository;
        public IAuthRepository AuthRepository => authRepository ?? (authRepository = HostContext.AppHost.GetAuthRepository(Request));

        private ISessionFactory sessionFactory;
        public virtual ISessionFactory SessionFactory => sessionFactory ?? (sessionFactory = TryResolve<ISessionFactory>()) ?? new SessionFactory(Cache);


        /// <summary>
        /// Typed UserSession
        /// </summary>
        public virtual TUserSession SessionAs<TUserSession>()
        {
            return SessionFeature.GetOrCreateSession<TUserSession>(Cache, Request, Response);
        }

        public virtual void ClearSession()
        {
            Cache.ClearSession();
        }

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
            using (authRepository as IDisposable) {}
        }
    }
}