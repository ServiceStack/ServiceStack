using System;
using System.Web;
using ServiceStack.Caching;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.Text.Common;

namespace ServiceStack.ServiceInterface
{
    public class SessionFactory : ISessionFactory
    {
        private readonly ICacheClient cacheClient;

        public SessionFactory(ICacheClient cacheClient)
        {
            this.cacheClient = cacheClient;
        }

        public class SessionCacheClient : ISession
        {
            private readonly ICacheClient cacheClient;
            private string prefixNs;

            public SessionCacheClient(ICacheClient cacheClient, string sessionId)
            {
                this.cacheClient = cacheClient;
                this.prefixNs = "sess:" + sessionId + ":";
            }

            public object this[string key]
            {
                get
                {
                    return cacheClient.Get<object>(this.prefixNs + key);
                }
                set
                {
                    JsWriter.WriteDynamic(() => 
                        cacheClient.Set(this.prefixNs + key, value));
                }
            }

            public void Set<T>(string key, T value)
            {
                cacheClient.Set(this.prefixNs + key, value);
            }

            public T Get<T>(string key)
            {
                return cacheClient.Get<T>(this.prefixNs + key);
            }
        }

        public ISession GetOrCreateSession(IHttpRequest httpReq, IHttpResponse httpRes)
        {
            var sessionId = httpReq.GetSessionId()
                ?? httpRes.CreateSessionIds(httpReq);

            return new SessionCacheClient(cacheClient, sessionId);
        }

        public ISession GetOrCreateSession()
        {
            if (HttpContext.Current != null)
                return GetOrCreateSession(
                    HttpContext.Current.Request.ToRequest(),
                    HttpContext.Current.Response.ToResponse());
            
            throw new NotImplementedException("Only ASP.NET Requests can be accessed via Singletons");
        }
    }
}