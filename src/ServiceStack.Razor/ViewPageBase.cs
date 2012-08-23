using System;
using System.Collections.Generic;
using System.Data;
using ServiceStack.CacheAccess;
using ServiceStack.Html;
using ServiceStack.OrmLite;
using ServiceStack.Razor.Templating;
using ServiceStack.Redis;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor
{
    public abstract class ViewPageBase<TModel>
        : TemplateBase<TModel>, IRazorTemplate
    {
        public abstract Type ModelType { get; }

        public abstract void Init(IRazorViewEngine viewEngine, ViewDataDictionary viewData, IHttpRequest httpReq, IHttpResponse httpRes);

        public UrlHelper Url = new UrlHelper();

        private IAppHost appHost;

        public virtual IViewEngine ViewEngine { get; set; }

        public IAppHost AppHost
        {
            get { return appHost ?? EndpointHost.AppHost; }
            set { appHost = value; }
        }

        public T Get<T>()
        {
            return this.AppHost.TryResolve<T>();
        }

        public IHttpRequest Request { get; set; }

        public IHttpResponse Response { get; set; }

        private ICacheClient cache;
        public ICacheClient Cache
        {
            get { return cache ?? (cache = Get<ICacheClient>()); }
        }

        private IDbConnection db;
        public IDbConnection Db
        {
            get { return db ?? (db = Get<IDbConnectionFactory>().OpenDbConnection()); }
        }

        private IRedisClient redis;
        public IRedisClient Redis
        {
            get { return redis ?? (redis = Get<IRedisClientsManager>().GetClient()); }
        }

        private ISessionFactory sessionFactory;
        private ISession session;
        public virtual ISession Session
        {
            get
            {
                if (sessionFactory == null)
                    sessionFactory = new SessionFactory(Cache);

                return session ?? (session = sessionFactory.GetOrCreateSession(Request, Response));
            }
        }

        private IAuthSession userSession;
        public virtual T GetSession<T>() where T : class, IAuthSession, new()
        {
            if (userSession != null) return (T)userSession;
            if (SessionKey != null)
                userSession = Cache.Get<T>(SessionKey);
            else
                SessionFeature.CreateSessionIds();

            var unAuthorizedSession = new T();
            return (T)(userSession ?? (userSession = unAuthorizedSession));
        }

        public string SessionKey
        {
            get
            {
                var sessionId = SessionFeature.GetSessionId();
                return sessionId == null ? null : SessionFeature.GetSessionKey(sessionId);
            }
        }

        public void ClearSession()
        {
            userSession = null;
            this.Cache.Remove(SessionKey);
        }

        public virtual void Dispose()
        {
            try
            {
                if (db != null) db.Dispose();
            }
            catch { }
            try
            {
                if (redis != null) redis.Dispose();
            }
            catch { }
        }
        
        public string Layout { get; set; }

        public Dictionary<string, object> ScopeArgs { get; set; }

        public string Href(string url)
        {
            return Url.Content(url);
        }

        public void Prepend(string contents)
        {
            if (contents == null) return;
            Builder.Insert(0, contents);
        }
    }
}