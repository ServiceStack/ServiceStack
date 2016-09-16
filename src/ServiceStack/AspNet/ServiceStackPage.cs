#if !NETSTANDARD1_6

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
        public virtual string UnauthorizedRedirectUrl => HostContext.GetPlugin<AuthFeature>().GetHtmlRedirect();

        /// <summary>
        /// Default redirct URL if Required Role or Permission attributes doesn't permit access.
        /// </summary>
        public virtual string ForbiddenRedirectUrl => HostContext.GetPlugin<AuthFeature>().GetHtmlRedirect();

        protected virtual void ServiceStack_PreLoad(object sender, EventArgs e)
        {
            var page = GetType();

            var authAttr = page.FirstAttribute<AuthenticateAttribute>();
            if (!this.IsAuthorized(authAttr))
            {
                var authError = authAttr?.HtmlRedirect != null
                    ? authAttr.HtmlRedirect.AddQueryParam("redirect", Request.Url.PathAndQuery)
                    : UnauthorizedRedirectUrl != null 
                        ? $"{UnauthorizedRedirectUrl}?redirect={Request.Url.PathAndQuery.UrlEncode()}#f=Unauthorized"
                        : null;

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
                var authError = authAttr?.HtmlRedirect != null
                    ? authAttr.HtmlRedirect.AddQueryParam("redirect", Request.Url.PathAndQuery)
                    : ForbiddenRedirectUrl != null 
                        ? $"{ForbiddenRedirectUrl}?redirect={Request.Url.PathAndQuery.UrlEncode()}#f=Forbidden"
                        : null;

                if (authError != null)
                {
                    base.Response.Redirect(authError);
                }
                else
                {
                    base.Response.StatusCode = 403;
                    base.Response.StatusDescription = "Forbidden";
                }
            }
        }

        private IServiceStackProvider serviceStackProvider;
        public virtual IServiceStackProvider ServiceStackProvider => 
            serviceStackProvider ?? (serviceStackProvider = new ServiceStackProvider(
                new AspNetRequest(new HttpContextWrapper(base.Context), GetType().Name)));

        public virtual IAppSettings AppSettings => ServiceStackProvider.AppSettings;

        public virtual IHttpRequest ServiceStackRequest => ServiceStackProvider.Request;

        public virtual IHttpResponse ServiceStackResponse => ServiceStackProvider.Response;

        public new virtual ICacheClient Cache => ServiceStackProvider.Cache;

        public virtual IDbConnection Db => ServiceStackProvider.Db;

        public virtual IRedisClient Redis => ServiceStackProvider.Redis;

        public virtual IMessageProducer MessageProducer => ServiceStackProvider.MessageProducer;

        public virtual IAuthRepository AuthRepository => ServiceStackProvider.AuthRepository;

        public virtual ISessionFactory SessionFactory => ServiceStackProvider.SessionFactory;

        public virtual ISession SessionBag => ServiceStackProvider.SessionBag;

        public virtual bool IsAuthenticated => ServiceStackProvider.IsAuthenticated;

        public virtual IAuthSession GetSession(bool reload = true) => ServiceStackProvider.GetSession(reload);

        public virtual TUserSession SessionAs<TUserSession>() => ServiceStackProvider.SessionAs<TUserSession>();

        protected virtual void SaveSession(IAuthSession session, TimeSpan? expiresIn = null) => 
            ServiceStackProvider.Request.SaveSession(session, expiresIn);

        public virtual void ClearSession() => ServiceStackProvider.ClearSession();

        public virtual T TryResolve<T>() => ServiceStackProvider.TryResolve<T>();

        public virtual T ResolveService<T>() => ServiceStackProvider.ResolveService<T>();

        public virtual object ForwardRequestToServiceStack(IRequest request = null) => 
            ServiceStackProvider.Execute(request ?? ServiceStackProvider.Request);

        public virtual IServiceGateway Gateway => ServiceStackProvider.Gateway;

        [Obsolete("Use Gateway")]
        protected virtual object Execute(object requestDto) => ServiceStackProvider.Execute(requestDto);

        [Obsolete("Use Gateway")]
        protected virtual TResponse Execute<TResponse>(IReturn<TResponse> requestDto) => ServiceStackProvider.Execute(requestDto);

        [Obsolete("Use Gateway")]
        protected virtual void PublishMessage<T>(T message) => ServiceStackProvider.PublishMessage(message);

        private bool hasDisposed;
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

#endif
