using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using ServiceStack.CacheAccess;
using ServiceStack.Common.Web;
using ServiceStack.Html;
using ServiceStack.Messaging;
using ServiceStack.OrmLite;
using ServiceStack.Razor.Templating;
using ServiceStack.Redis;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor
{
    public abstract class ViewPageBase<TModel>
        : TemplateBase<TModel>, IRazorTemplate, ICloneable
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

        public object ModelError { get; set; } 

        public ResponseStatus ResponseStatus
        {
            get
            {
                return ToResponseStatus(ModelError) ?? ToResponseStatus(Model);
            }
        }

        private ResponseStatus ToResponseStatus<T>(T modelError)
        {
            var ret = modelError.ToResponseStatus();
            if (ret != null) return ret;

            if (modelError is DynamicObject)
            {
                var dynError = modelError as dynamic;
                return (ResponseStatus)dynError.ResponseStatus;
            }

            return null;
        }

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

        private IMessageProducer messageProducer;
        public virtual IMessageProducer MessageProducer
        {
            get { return messageProducer ?? (messageProducer = Get<IMessageFactory>().CreateMessageProducer()); }
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
            return (T)(userSession = SessionFeature.GetOrCreateSession<T>(Cache));
        }

        public string SessionKey
        {
            get
            {
                return SessionFeature.GetSessionKey();
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
                if (cache != null) cache.Dispose();
                cache = null;
            }
            catch { }
            try
            {
                if (db != null) db.Dispose();
                db = null;
            }
            catch { }
            try
            {
                if (redis != null) redis.Dispose();
                redis = null;
            }
            catch { }
            try
            {
                if (messageProducer != null) messageProducer.Dispose();
                messageProducer = null;
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