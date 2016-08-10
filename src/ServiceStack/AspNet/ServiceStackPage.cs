using System;
using System.Data;
using System.Web;
using System.Web.UI;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host.AspNet;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Web;

namespace ServiceStack.AspNet
{
    public class ServiceStackPage : Page, IHasServiceStackProvider
    {
        public ServiceStackPage()
        {
            this.PreLoad += ServiceStack_PreLoad;
        }

        /// <summary>
        /// Default redirct URL if [Authenticate] attribute doesn't permit access.
        /// </summary>
        public virtual string UnauthorizedRedirectUrl
        {
            get { return HostContext.GetPlugin<AuthFeature>().GetHtmlRedirect(); }
        }

        /// <summary>
        /// Default redirct URL if Required Role or Permission attributes doesn't permit access.
        /// </summary>
        public virtual string ForbiddenRedirectUrl
        {
            get { return HostContext.GetPlugin<AuthFeature>().GetHtmlRedirect(); }
        }

        protected virtual void ServiceStack_PreLoad(object sender, EventArgs e)
        {
            var page = GetType();

            var authAttr = page.FirstAttribute<AuthenticateAttribute>();
            if (!this.IsAuthorized(authAttr))
            {
                var authError = authAttr != null && authAttr.HtmlRedirect != null
                    ? authAttr.HtmlRedirect.AddQueryParam("redirect", Request.Url.PathAndQuery)
                    : UnauthorizedRedirectUrl != null ? UnauthorizedRedirectUrl + "?redirect={0}#f=Unauthorized".Fmt(Request.Url.PathAndQuery.UrlEncode()) : null;

                if (authError != null)
                {
                    base.Response.Redirect(authError);
                }
                else
                {
                    base.Response.StatusCode = 401;
                    base.Response.StatusDescription = "Unauthorized";
                }
                return;
            }

            if (!this.HasAccess(
                page.AllAttributes<RequiredRoleAttribute>(),
                page.AllAttributes<RequiresAnyRoleAttribute>(),
                page.AllAttributes<RequiredPermissionAttribute>(),
                page.AllAttributes<RequiresAnyPermissionAttribute>()))
            {
                var authError = authAttr != null && authAttr.HtmlRedirect != null
                    ? authAttr.HtmlRedirect.AddQueryParam("redirect", Request.Url.PathAndQuery)
                    : ForbiddenRedirectUrl != null ? ForbiddenRedirectUrl + "?redirect={0}#f=Forbidden".Fmt(Request.Url.PathAndQuery.UrlEncode()) : null;

                if (authError != null)
                {
                    base.Response.Redirect(authError);
                }
                else
                {
                    base.Response.StatusCode = 403;
                    base.Response.StatusDescription = "Forbidden";
                }
                return;
            }
        }

        private IServiceStackProvider serviceStackProvider;
        public virtual IServiceStackProvider ServiceStackProvider
        {
            get
            {
                return serviceStackProvider ?? (serviceStackProvider = new ServiceStackProvider(
                        new AspNetRequest(new HttpContextWrapper(base.Context), GetType().Name)));
            }
        }
        public virtual IAppSettings AppSettings
        {
            get { return ServiceStackProvider.AppSettings; }
        }
        public virtual IHttpRequest ServiceStackRequest
        {
            get { return ServiceStackProvider.Request; }
        }
        public virtual IHttpResponse ServiceStackResponse
        {
            get { return ServiceStackProvider.Response; }
        }
        public virtual ICacheClient Cache
        {
            get { return ServiceStackProvider.Cache; }
        }
        public virtual IDbConnection Db
        {
            get { return ServiceStackProvider.Db; }
        }
        public virtual IRedisClient Redis
        {
            get { return ServiceStackProvider.Redis; }
        }
        public virtual IMessageProducer MessageProducer
        {
            get { return ServiceStackProvider.MessageProducer; }
        }
        public virtual IAuthRepository AuthRepository
        {
            get { return ServiceStackProvider.AuthRepository; }
        }
        public virtual ISessionFactory SessionFactory
        {
            get { return ServiceStackProvider.SessionFactory; }
        }
        public virtual ISession SessionBag
        {
            get { return ServiceStackProvider.SessionBag; }
        }
        public virtual bool IsAuthenticated
        {
            get { return ServiceStackProvider.IsAuthenticated; }
        }
        public virtual IAuthSession GetSession(bool reload = true)
        {
            return ServiceStackProvider.GetSession(reload);
        }
        public virtual TUserSession SessionAs<TUserSession>()
        {
            return ServiceStackProvider.SessionAs<TUserSession>();
        }
        protected virtual void SaveSession(IAuthSession session, TimeSpan? expiresIn = null)
        {
            ServiceStackProvider.Request.SaveSession(session, expiresIn);
        }
        public virtual void ClearSession()
        {
            ServiceStackProvider.ClearSession();
        }
        public virtual T TryResolve<T>()
        {
            return ServiceStackProvider.TryResolve<T>();
        }
        public virtual T ResolveService<T>()
        {
            return ServiceStackProvider.ResolveService<T>();
        }
        public virtual object ForwardRequestToServiceStack(IRequest request = null)
        {
            return ServiceStackProvider.Execute(request ?? ServiceStackProvider.Request);
        }

        public virtual IServiceGateway Gateway
        {
            get { return ServiceStackProvider.Gateway; }
        }
        [Obsolete("Use Gateway")]
        protected virtual object Execute(object requestDto)
        {
            return ServiceStackProvider.Execute(requestDto);
        }
        [Obsolete("Use Gateway")]
        protected virtual TResponse Execute<TResponse>(IReturn<TResponse> requestDto)
        {
            return ServiceStackProvider.Execute(requestDto);
        }
        [Obsolete("Use Gateway")]
        protected virtual void PublishMessage<T>(T message)
        {
            ServiceStackProvider.PublishMessage(message);
        }

        private bool hasDisposed = false;
        public override void Dispose()
        {
            if (hasDisposed)
                return;

            hasDisposed = true;
            base.Dispose();

            if (serviceStackProvider != null)
            {
                serviceStackProvider.Dispose();
                serviceStackProvider = null;
            }

            EndServiceStackRequest();
        }

        public virtual void EndServiceStackRequest()
        {
            HostContext.AppHost.OnEndRequest(ServiceStackRequest);
        }
    }
}