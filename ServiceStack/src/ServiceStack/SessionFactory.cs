using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Caching;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Web;

namespace ServiceStack;

public class SessionFactory : ISessionFactory
{
    private readonly ICacheClient cacheClient;
    private readonly ICacheClientAsync cacheClientAsync;

    public SessionFactory(ICacheClient cacheClient)
        : this(cacheClient, null) {}

    public SessionFactory(ICacheClient cacheClient, ICacheClientAsync cacheClientAsync)
    {
        this.cacheClient = cacheClient;
        this.cacheClientAsync = cacheClientAsync ?? cacheClient.AsAsync();
    }

    public class SessionCacheClient : ISession
    {
        private readonly ICacheClient cacheClient;
        private readonly string prefixNs;

        public SessionCacheClient(ICacheClient cacheClient, string sessionId)
        {
            this.cacheClient = cacheClient;
            this.prefixNs = "sess:" + sessionId + ":";
        }

        private string EnsurePrefix(string s) => s != null && !s.StartsWith(prefixNs)
            ? prefixNs + s
            : s;

        public object this[string key]
        {
            get => cacheClient.Get<object>(EnsurePrefix(key));
            set
            {
                JsWriter.WriteDynamic(() =>
                    cacheClient.Set(EnsurePrefix(key), value));
            }
        }

        public void Set<T>(string key, T value)
        {
            var expiry = HostContext.GetPlugin<SessionFeature>()?.SessionBagExpiry;
            if (expiry != null)
                cacheClient.Set(EnsurePrefix(key), value, expiry.Value);
            else
                cacheClient.Set(EnsurePrefix(key), value);
        }

        public T Get<T>(string key)
        {
            return cacheClient.Get<T>(EnsurePrefix(key));
        }

        public bool Remove(string key)
        {
            return cacheClient.Remove(EnsurePrefix(key));
        }

        public void RemoveAll()
        {
            cacheClient.RemoveByPattern(this.prefixNs + "*");
        }
    }

    public class SessionCacheClientAsync : ISessionAsync
    {
        private readonly ICacheClientAsync cacheClient;
        private readonly string prefixNs;

        public SessionCacheClientAsync(ICacheClientAsync cacheClient, string sessionId)
        {
            this.cacheClient = cacheClient;
            this.prefixNs = "sess:" + sessionId + ":";
        }

        private string EnsurePrefix(string s) => s != null && !s.StartsWith(prefixNs)
            ? prefixNs + s
            : s;

        public async Task SetAsync<T>(string key, T value, CancellationToken token=default)
        {
            var expiry = HostContext.GetPlugin<SessionFeature>()?.SessionBagExpiry;
            if (expiry != null)
                await cacheClient.SetAsync(EnsurePrefix(key), value, expiry.Value, token).ConfigAwait();
            else
                await cacheClient.SetAsync(EnsurePrefix(key), value, token).ConfigAwait();
        }

        public Task<T> GetAsync<T>(string key, CancellationToken token=default)
        {
            return cacheClient.GetAsync<T>(EnsurePrefix(key), token);
        }

        public Task<bool> RemoveAsync(string key, CancellationToken token=default)
        {
            return cacheClient.RemoveAsync(EnsurePrefix(key), token);
        }

        public Task RemoveAllAsync(CancellationToken token=default)
        {
            return cacheClient.RemoveByPatternAsync(this.prefixNs + "*", token);
        }
    }

    public ISession GetOrCreateSession(IRequest httpReq, IResponse httpRes)
    {
        var sessionId = httpReq.GetSessionId() ?? httpRes.CreateSessionIds(httpReq);
        return new SessionCacheClient(cacheClient, sessionId);
    }

    public ISessionAsync GetOrCreateSessionAsync(IRequest httpReq, IResponse httpRes)
    {
        var sessionId = httpReq.GetSessionId() ?? httpRes.CreateSessionIds(httpReq);
        return new SessionCacheClientAsync(cacheClientAsync, sessionId);
    }

    public ISession GetOrCreateSession()
    {
        var request = HostContext.GetCurrentRequest();
        return GetOrCreateSession(request, request.Response);
    }

    public ISessionAsync GetOrCreateSessionAsync()
    {
        var request = HostContext.GetCurrentRequest();
        return GetOrCreateSessionAsync(request, request.Response);
    }

    public ISession CreateSession(string sessionId)
    {
        return new SessionCacheClient(cacheClient, sessionId);
    }

    public ISessionAsync CreateSessionAsync(string sessionId)
    {
        return new SessionCacheClientAsync(cacheClientAsync, sessionId);
    }
}