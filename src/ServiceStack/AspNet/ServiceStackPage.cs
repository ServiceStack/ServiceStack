using System;
using System.Data;
using System.Web;
using System.Web.UI;
using ServiceStack.Auth;
using ServiceStack.Caching;
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

        protected virtual void ServiceStack_PreLoad(object sender, EventArgs e)
        {
            var auth = GetType().FirstAttribute<AuthenticateAttribute>();
            if (auth == null) return;
            if (IsAuthenticated) return;

            var htmlRedirect = auth.HtmlRedirect ?? HostContext.GetPlugin<AuthFeature>().HtmlRedirect;
            if (htmlRedirect == null)
                throw new UnauthorizedAccessException("This page requires authentication");

            base.Response.Redirect(htmlRedirect);
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
        public virtual IMessageFactory MessageFactory
        {
            get { return ServiceStackProvider.MessageFactory; }
        }
        public virtual IMessageProducer MessageProducer
        {
            get { return ServiceStackProvider.MessageProducer; }
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
        public virtual T TryResolve<T>()
        {
            return ServiceStackProvider.TryResolve<T>();
        }
        public virtual T ResolveService<T>()
        {
            return ServiceStackProvider.ResolveService<T>();
        }
        public virtual IAuthSession GetSession(bool reload = true)
        {
            return ServiceStackProvider.GetSession(reload);
        }
        public virtual TUserSession SessionAs<TUserSession>()
        {
            return ServiceStackProvider.SessionAs<TUserSession>();
        }
        public virtual void ClearSession()
        {
            ServiceStackProvider.ClearSession();
        }
        public virtual void PublishMessage<T>(T message)
        {
            ServiceStackProvider.PublishMessage(message);
        }
        public override void Dispose()
        {
            base.Dispose();

            if (serviceStackProvider != null)
            {
                serviceStackProvider.Dispose();
                serviceStackProvider = null;
            }
        }
    }
}