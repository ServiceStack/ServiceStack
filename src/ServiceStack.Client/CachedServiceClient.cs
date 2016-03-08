#if !SL5
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public class CachedServiceClient : IServiceClient
    {
        public TimeSpan? ClearCachesOlderThan { get; set; }
        public TimeSpan? ClearExpiredCachesOlderThan { get; set; }

        public int CleanCachesWhenCountExceeds { get; set; }

        public int CacheCount
        {
            get { return cache.Count; }
        }

        private long cacheHits;
        public long CacheHits
        {
            get { return cacheHits; }
        }

        private long notModifiedHits;
        public long NotModifiedHits
        {
            get { return notModifiedHits; }
        }

        private long cachesCreated;
        public long CachesCreated
        {
            get { return cachesCreated; }
        }

        private long cachesRemoved;
        public long CachesRemoved
        {
            get { return cachesRemoved; }
        }

        class CacheEntry
        {
            public CacheEntry(object response)
            {
                Response = response;
                Created = DateTime.UtcNow;
            }

            public DateTime Created;
            public string ETag;
            public DateTime? LastModified;
            public bool MustRevalidate;
            public TimeSpan Age;
            public TimeSpan MaxAge;
            public DateTime Expires;
            public object Response;

            public bool ShouldRevalidate()
            {
                return MustRevalidate || DateTime.UtcNow > Expires;
            }
        }

        private ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>();

        private readonly Action<HttpWebRequest> existingRequestFilter;
        private readonly ResultsFilterDelegate existingResultsFilter;
        private readonly ResultsFilterResponseDelegate existingResultsFilterResponse;

        private readonly ServiceClientBase client;

        public CachedServiceClient(ServiceClientBase client)
        {
            this.client = client;
            ClearExpiredCachesOlderThan = TimeSpan.FromHours(1);
            CleanCachesWhenCountExceeds = 1000;

            existingRequestFilter = client.RequestFilter;
            existingResultsFilter = client.ResultsFilter;
            existingResultsFilterResponse = client.ResultsFilterResponse;

            client.RequestFilter = OnRequestFilter;
            client.ResultsFilter = OnResultsFilter;
            client.ResultsFilterResponse = OnResultsFilterResponse;
            client.NotModifiedFilter = OnNotModifiedFilter;
        }

        private void OnRequestFilter(HttpWebRequest webReq)
        {
            if (existingRequestFilter != null)
                existingRequestFilter(webReq);

            CacheEntry entry;
            if (webReq.Method == HttpMethods.Get && cache.TryGetValue(webReq.RequestUri.ToString(), out entry))
            {
                if (entry.ETag != null)
                    webReq.Headers[HttpRequestHeader.IfNoneMatch] = entry.ETag;

                if (entry.LastModified != null)
                    webReq.IfModifiedSince = entry.LastModified.Value;
            }
        }

        private object OnResultsFilter(Type responseType, string httpMethod, string requestUri, object request)
        {
            var ret = existingResultsFilter != null 
                ? existingResultsFilter(responseType, httpMethod, requestUri, request)
                : null;

            CacheEntry entry;
            if (httpMethod == HttpMethods.Get && cache.TryGetValue(requestUri, out entry))
            {
                if (!entry.ShouldRevalidate())
                {
                    Interlocked.Increment(ref cacheHits);
                    return entry.Response;
                }
            }

            return ret;
        }

        public object OnNotModifiedFilter(WebResponse webRes, string requestUri, Type responseType)
        {
            CacheEntry entry;
            if (cache.TryGetValue(requestUri, out entry))
            {
                Interlocked.Increment(ref notModifiedHits);
                return entry.Response;
            }

            return null;
        }

        private void OnResultsFilterResponse(WebResponse webRes, object response, string httpMethod, string requestUri, object request)
        {
            if (existingResultsFilterResponse != null)
                existingResultsFilterResponse(webRes, response, httpMethod, requestUri, request);

            if (httpMethod != HttpMethods.Get || response == null)
                return;

            var eTag = webRes.Headers[HttpHeaders.ETag];
            var lastModifiedStr = webRes.Headers[HttpResponseHeader.LastModified];

            if (eTag == null && lastModifiedStr == null)
                return;

            var entry = new CacheEntry(response)
            {
                ETag = eTag,
            };

            if (lastModifiedStr != null)
            {
                DateTime lastModified;
                if (DateTime.TryParse(lastModifiedStr, out lastModified))
                    entry.LastModified = lastModified.ToUniversalTime();
            }

            long secs;
            var ageStr = webRes.Headers[HttpResponseHeader.Age];
            if (ageStr != null && long.TryParse(ageStr, out secs))
                entry.Age = TimeSpan.FromSeconds(secs);

            var cacheControl = webRes.Headers[HttpResponseHeader.CacheControl];
            if (cacheControl != null)
            {
                var parts = cacheControl.Split(',');

                foreach (var part in parts)
                {
                    var kvp = part.Split('=');
                    var key = kvp[0].Trim().ToLower();

                    switch (key)
                    {
                        case "max-age":
                            if (kvp.Length == 2 && long.TryParse(kvp[1], out secs))
                                entry.MaxAge = TimeSpan.FromSeconds(secs);
                            break;
                        case "must-revalidate":
                            entry.MustRevalidate = true;
                            break;
                        case "no-cache":
                            return; //don't cache
                    }
                }

                if (entry.MaxAge > TimeSpan.FromSeconds(0))
                {
                    entry.Expires = entry.Created + entry.MaxAge;
                    cache[requestUri] = entry;
                    Interlocked.Increment(ref cachesCreated);

                    var runCleanupAfterEvery = CleanCachesWhenCountExceeds;
                    if (cachesCreated % runCleanupAfterEvery == 0 && 
                        cache.Count > CleanCachesWhenCountExceeds)
                    {
                        if (ClearExpiredCachesOlderThan != null)
                            RemoveExpiredCachesOlderThan(ClearExpiredCachesOlderThan.Value);
                        if (ClearCachesOlderThan != null)
                            RemoveCachesOlderThan(ClearCachesOlderThan.Value);
                    }
                }
            }

        }

        public void ClearCache()
        {
            cache = new ConcurrentDictionary<string, CacheEntry>();
        }

        public void RemoveCachesOlderThan(TimeSpan age)
        {
            var keysToRemove = new List<string>();
            var now = DateTime.UtcNow;

            foreach (var entry in cache)
            {
                if (now - entry.Value.Created > age)
                    keysToRemove.Add(entry.Key);
            }

            foreach (var key in keysToRemove)
            {
                CacheEntry ignore;
                if (cache.TryRemove(key, out ignore))
                    Interlocked.Increment(ref cachesRemoved);
            }
        }

        public void RemoveExpiredCachesOlderThan(TimeSpan age)
        {
            var keysToRemove = new List<string>();
            var now = DateTime.UtcNow;

            foreach (var entry in cache)
            {
                if (now - entry.Value.Expires > age)
                    keysToRemove.Add(entry.Key);
            }

            foreach (var key in keysToRemove)
            {
                CacheEntry ignore;
                if (cache.TryRemove(key, out ignore))
                    Interlocked.Increment(ref cachesRemoved);
            }
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public void SetCredentials(string userName, string password)
        {
            client.SetCredentials(userName, password);
        }

        public Task<TResponse> GetAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return client.GetAsync(requestDto);
        }

        public Task<TResponse> GetAsync<TResponse>(object requestDto)
        {
            return client.GetAsync<TResponse>(requestDto);
        }

        public Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl)
        {
            return client.GetAsync<TResponse>(relativeOrAbsoluteUrl);
        }

        public Task GetAsync(IReturnVoid requestDto)
        {
            return client.GetAsync(requestDto);
        }

        public Task<TResponse> DeleteAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return client.DeleteAsync(requestDto);
        }

        public Task<TResponse> DeleteAsync<TResponse>(object requestDto)
        {
            return client.DeleteAsync<TResponse>(requestDto);
        }

        public Task<TResponse> DeleteAsync<TResponse>(string relativeOrAbsoluteUrl)
        {
            return client.DeleteAsync<TResponse>(relativeOrAbsoluteUrl);
        }

        public Task DeleteAsync(IReturnVoid requestDto)
        {
            return client.DeleteAsync(requestDto);
        }

        public Task<TResponse> PostAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return client.PostAsync(requestDto);
        }

        public Task<TResponse> PostAsync<TResponse>(object requestDto)
        {
            return client.PostAsync<TResponse>(requestDto);
        }

        public Task<TResponse> PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return client.PostAsync<TResponse>(relativeOrAbsoluteUrl, request);
        }

        public Task PostAsync(IReturnVoid requestDto)
        {
            return client.PostAsync(requestDto);
        }

        public Task<TResponse> PutAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return client.PutAsync(requestDto);
        }

        public Task<TResponse> PutAsync<TResponse>(object requestDto)
        {
            return client.PutAsync<TResponse>(requestDto);
        }

        public Task<TResponse> PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return client.PutAsync<TResponse>(relativeOrAbsoluteUrl, request);
        }

        public Task PutAsync(IReturnVoid requestDto)
        {
            return client.PutAsync(requestDto);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
        {
            return client.CustomMethodAsync(httpVerb, requestDto);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, object requestDto)
        {
            return client.CustomMethodAsync<TResponse>(httpVerb, requestDto);
        }

        public Task CustomMethodAsync(string httpVerb, IReturnVoid requestDto)
        {
            return client.CustomMethodAsync(httpVerb, requestDto);
        }

        public Task<TResponse> CustomMethodAsync<TResponse>(string httpVerb, string relativeOrAbsoluteUrl, object request)
        {
            return client.CustomMethodAsync<TResponse>(httpVerb, relativeOrAbsoluteUrl, request);
        }

        public void CancelAsync()
        {
            client.CancelAsync();
        }

        public Task<TResponse> SendAsync<TResponse>(IReturn<TResponse> requestDto)
        {
            return client.SendAsync(requestDto);
        }

        public Task<TResponse> SendAsync<TResponse>(object requestDto)
        {
            return client.SendAsync<TResponse>(requestDto);
        }

        public Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<IReturn<TResponse>> requests)
        {
            return client.SendAllAsync(requests);
        }

        public void SendOneWay(object requestDto)
        {
            client.SendOneWay(requestDto);
        }

        public void SendOneWay(string relativeOrAbsoluteUri, object requestDto)
        {
            client.SendOneWay(relativeOrAbsoluteUri, requestDto);
        }

        public void SendAllOneWay(IEnumerable<object> requests)
        {
            client.SendAllOneWay(requests);
        }

        public void AddHeader(string name, string value)
        {
            client.AddHeader(name, value);
        }

        public void ClearCookies()
        {
            client.ClearCookies();
        }

        public Dictionary<string, string> GetCookieValues()
        {
            return client.GetCookieValues();
        }

        public void SetCookie(string name, string value, TimeSpan? expiresIn = null)
        {
            client.SetCookie(name, value, expiresIn);
        }

        public void Get(IReturnVoid request)
        {
            client.Get(request);
        }

        public TResponse Get<TResponse>(IReturn<TResponse> requestDto)
        {
            return client.Get(requestDto);
        }

        public TResponse Get<TResponse>(object requestDto)
        {
            return client.Get<TResponse>(requestDto);
        }

        public TResponse Get<TResponse>(string relativeOrAbsoluteUrl)
        {
            return client.Get<TResponse>(relativeOrAbsoluteUrl);
        }

        public IEnumerable<TResponse> GetLazy<TResponse>(IReturn<QueryResponse<TResponse>> queryDto)
        {
            return client.GetLazy(queryDto);
        }

        public void Delete(IReturnVoid requestDto)
        {
            client.Delete(requestDto);
        }

        public TResponse Delete<TResponse>(IReturn<TResponse> request)
        {
            return client.Delete(request);
        }

        public TResponse Delete<TResponse>(object request)
        {
            return client.Delete<TResponse>(request);
        }

        public TResponse Delete<TResponse>(string relativeOrAbsoluteUrl)
        {
            return client.Delete<TResponse>(relativeOrAbsoluteUrl);
        }

        public void Post(IReturnVoid requestDto)
        {
            client.Post(requestDto);
        }

        public TResponse Post<TResponse>(IReturn<TResponse> requestDto)
        {
            return client.Post(requestDto);
        }

        public TResponse Post<TResponse>(object requestDto)
        {
            return client.Post<TResponse>(requestDto);
        }

        public TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request)
        {
            return client.Post<TResponse>(relativeOrAbsoluteUrl, request);
        }

        public void Put(IReturnVoid requestDto)
        {
            client.Put(requestDto);
        }

        public TResponse Put<TResponse>(IReturn<TResponse> requestDto)
        {
            return client.Put(requestDto);
        }

        public TResponse Put<TResponse>(object requestDto)
        {
            return client.Put<TResponse>(requestDto);
        }

        public TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object requestDto)
        {
            return client.Put<TResponse>(relativeOrAbsoluteUrl, requestDto);
        }

        public void Patch(IReturnVoid requestDto)
        {
            client.Patch(requestDto);
        }

        public TResponse Patch<TResponse>(IReturn<TResponse> requestDto)
        {
            return client.Patch(requestDto);
        }

        public TResponse Patch<TResponse>(object requestDto)
        {
            return client.Patch<TResponse>(requestDto);
        }

        public TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object requestDto)
        {
            return client.Patch<TResponse>(relativeOrAbsoluteUrl, requestDto);
        }

        public void CustomMethod(string httpVerb, IReturnVoid requestDto)
        {
            client.CustomMethod(httpVerb, requestDto);
        }

        public TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto)
        {
            return client.CustomMethod(httpVerb, requestDto);
        }

        public TResponse CustomMethod<TResponse>(string httpVerb, object requestDto)
        {
            return client.CustomMethod<TResponse>(httpVerb, requestDto);
        }

        public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType)
        {
            return client.PostFile<TResponse>(relativeOrAbsoluteUrl, fileToUpload, fileName, mimeType);
        }

        public TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "upload")
        {
            return client.PostFileWithRequest<TResponse>(fileToUpload, fileName, request, fieldName);
        }

        public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName,
            object request, string fieldName = "upload")
        {
            return client.PostFileWithRequest<TResponse>(relativeOrAbsoluteUrl, fileToUpload, fileName, request, fieldName);
        }

        public TResponse PostFilesWithRequest<TResponse>(object request, IEnumerable<UploadFile> files)
        {
            return client.PostFilesWithRequest<TResponse>(request, files);
        }

        public TResponse PostFilesWithRequest<TResponse>(string relativeOrAbsoluteUrl, object request, IEnumerable<UploadFile> files)
        {
            return client.PostFilesWithRequest<TResponse>(relativeOrAbsoluteUrl, request, files);
        }

        public TResponse Send<TResponse>(object request)
        {
            return client.Send<TResponse>(request);
        }

        public TResponse Send<TResponse>(IReturn<TResponse> request)
        {
            return client.Send(request);
        }

        public void Send(IReturnVoid request)
        {
            client.Send(request);
        }

        public List<TResponse> SendAll<TResponse>(IEnumerable<IReturn<TResponse>> requests)
        {
            return client.SendAll(requests);
        }

        public string SessionId { get; set; }
        public int Version { get; set; }
    }

    public static class CachedServiceClientExtensions
    {
        public static IServiceClient WithCache(this ServiceClientBase client)
        {
            return new CachedServiceClient(client);
        }
    }
}
#endif
