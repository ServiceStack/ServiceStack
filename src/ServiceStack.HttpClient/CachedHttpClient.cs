using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public class CachedHttpClient : ICachedServiceClient
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

        private long errorFallbackHits;
        public long ErrorFallbackHits
        {
            get { return errorFallbackHits; }
        }

        private long cachesAdded;
        public long CachesAdded
        {
            get { return cachesAdded; }
        }

        private long cachesRemoved;
        public long CachesRemoved
        {
            get { return cachesRemoved; }
        }

        private ConcurrentDictionary<string, HttpCacheEntry> cache = new ConcurrentDictionary<string, HttpCacheEntry>();

        private readonly Action<HttpRequestMessage> existingRequestFilter;
        private readonly ResultsFilterHttpDelegate existingResultsFilter;
        private readonly ResultsFilterHttpResponseDelegate existingResultsFilterResponse;
        private ExceptionFilterHttpDelegate existingExceptionFilter;

        private readonly JsonHttpClient client;

        public CachedHttpClient(JsonHttpClient client, ConcurrentDictionary<string, HttpCacheEntry> cache)
            : this(client)
        {
            if (cache != null)
                this.cache = cache;
        }

        public CachedHttpClient(JsonHttpClient client)
        {
            this.client = client;
            ClearExpiredCachesOlderThan = TimeSpan.FromHours(1);
            CleanCachesWhenCountExceeds = 1000;

            existingRequestFilter = client.RequestFilter;
            existingResultsFilter = client.ResultsFilter;
            existingResultsFilterResponse = client.ResultsFilterResponse;
            existingExceptionFilter = client.ExceptionFilter;

            client.RequestFilter = OnRequestFilter;
            client.ResultsFilter = OnResultsFilter;
            client.ResultsFilterResponse = OnResultsFilterResponse;
            client.ExceptionFilter = OnExceptionFilter;
        }

        private void OnRequestFilter(HttpRequestMessage webReq)
        {
            if (existingRequestFilter != null)
                existingRequestFilter(webReq);

            HttpCacheEntry entry;
            if (webReq.Method.Method == HttpMethods.Get && cache.TryGetValue(webReq.RequestUri.ToString(), out entry))
            {
                if (entry.ETag != null)
                    webReq.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(
                        entry.ETag.StripWeakRef(),
                        entry.ETag.StartsWith("W/")));

                if (entry.LastModified != null)
                    webReq.Headers.IfModifiedSince = entry.LastModified.Value;
            }
        }

        private object OnResultsFilter(Type responseType, string httpMethod, string requestUri, object request)
        {
            var ret = existingResultsFilter != null
                ? existingResultsFilter(responseType, httpMethod, requestUri, request)
                : null;

            HttpCacheEntry entry;
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

        public object OnExceptionFilter(HttpResponseMessage webRes, string requestUri, Type responseType)
        {
            if (existingExceptionFilter != null)
            {
                var response = existingExceptionFilter(webRes, requestUri, responseType);
                if (response != null)
                    return response;
            }

            HttpCacheEntry entry;
            if (cache.TryGetValue(requestUri, out entry))
            {
                if (webRes.StatusCode == HttpStatusCode.NotModified)
                {
                    Interlocked.Increment(ref notModifiedHits);
                    return entry.Response;
                }
                if (entry.CanUseCacheOnError())
                {
                    Interlocked.Increment(ref errorFallbackHits);
                    return entry.Response;
                }
            }

            return null;
        }

        private void OnResultsFilterResponse(HttpResponseMessage webRes, object response, string httpMethod, string requestUri, object request)
        {
            if (existingResultsFilterResponse != null)
                existingResultsFilterResponse(webRes, response, httpMethod, requestUri, request);

            if (httpMethod != HttpMethods.Get || response == null || webRes == null)
                return;
            
            var eTag = webRes.Headers.ETag != null ? webRes.Headers.ETag.Tag : null;
            
            if (eTag == null && webRes.Content.Headers.LastModified == null)
                return;

            var entry = new HttpCacheEntry(response)
            {
                ETag = eTag,
                ContentLength = webRes.Content.Headers.ContentLength
            };

            if (webRes.Content.Headers.LastModified != null)
                entry.LastModified = webRes.Content.Headers.LastModified.Value.UtcDateTime;

            entry.Age = webRes.Headers.Age;

            var cacheControl = webRes.Headers.CacheControl;
            if (cacheControl != null)
            {
                if (cacheControl.NoCache)
                    return;

                if (cacheControl.MaxAge != null)
                    entry.MaxAge = cacheControl.MaxAge.Value;

                entry.MustRevalidate = cacheControl.MustRevalidate;
                entry.NoCache = cacheControl.NoCache;

                entry.SetMaxAge(entry.MaxAge);
                cache[requestUri] = entry;
                Interlocked.Increment(ref cachesAdded);

                var runCleanupAfterEvery = CleanCachesWhenCountExceeds;
                if (cachesAdded % runCleanupAfterEvery == 0 &&
                    cache.Count > CleanCachesWhenCountExceeds)
                {
                    if (ClearExpiredCachesOlderThan != null)
                        RemoveExpiredCachesOlderThan(ClearExpiredCachesOlderThan.Value);
                    if (ClearCachesOlderThan != null)
                        RemoveCachesOlderThan(ClearCachesOlderThan.Value);
                }
            }
        }

        public void SetCache(ConcurrentDictionary<string, HttpCacheEntry> cache)
        {
            if (cache == null)
                throw new ArgumentNullException("cache");

            this.cache = cache;
        }

        public int RemoveCachesOlderThan(TimeSpan age)
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
                HttpCacheEntry ignore;
                if (cache.TryRemove(key, out ignore))
                    Interlocked.Increment(ref cachesRemoved);
            }

            return keysToRemove.Count;
        }

        public int RemoveExpiredCachesOlderThan(TimeSpan age)
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
                HttpCacheEntry ignore;
                if (cache.TryRemove(key, out ignore))
                    Interlocked.Increment(ref cachesRemoved);
            }

            return keysToRemove.Count;
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

        public List<TResponse> SendAll<TResponse>(IEnumerable<object> requests)
        {
            return client.SendAll<TResponse>(requests);
        }

        public void Publish(object requestDto)
        {
            client.Publish(requestDto);
        }

        public void PublishAll(IEnumerable<object> requestDtos)
        {
            client.PublishAll(requestDtos);
        }

        public Task<TResponse> SendAsync<TResponse>(object requestDto, CancellationToken token)
        {
            return client.SendAsync<TResponse>(requestDto, token);
        }

        public Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<object> requests, CancellationToken token)
        {
            return client.SendAllAsync<TResponse>(requests, token);
        }

        public Task PublishAsync(object requestDto, CancellationToken token)
        {
            return client.PublishAsync(requestDto, token);
        }

        public Task PublishAllAsync(IEnumerable<object> requestDtos, CancellationToken token)
        {
            return client.PublishAllAsync(requestDtos, token);
        }

        public string SessionId
        {
            get { return client.SessionId; }
            set { client.SessionId = value; }
        }

        public int Version
        {
            get { return client.Version; }
            set { client.Version = value; }
        }
    }

    public static class CachedHttpClientExtensions
    {
        public static IServiceClient WithCache(this JsonHttpClient client)
        {
            return new CachedHttpClient(client);
        }

        public static IServiceClient WithCache(this JsonHttpClient client, ConcurrentDictionary<string, HttpCacheEntry> cache)
        {
            return new CachedHttpClient(client, cache);
        }

        internal static string StripWeakRef(this string eTag)
        {
            return eTag != null && eTag.StartsWith("W/")
                ? eTag.Substring(2)
                : eTag;
        }
    }
}