using System;
using System.Collections.Generic;
using ServiceStack.Caching;
using ServiceStack.Web;
using ServiceStack.Text;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack;

public static class CacheClientExtensions
{
    public static void Set<T>(this ICacheClient cache, string cacheKey, T value, TimeSpan? expireCacheIn)
    {
        if (expireCacheIn.HasValue)
            cache.Set(cacheKey, value, expireCacheIn.Value);
        else
            cache.Set(cacheKey, value);
    }

    public static async Task SetAsync<T>(this ICacheClientAsync cache, string cacheKey, T value, TimeSpan? expireCacheIn, CancellationToken token=default)
    {
        if (expireCacheIn.HasValue)
            await cache.SetAsync(cacheKey, value, expireCacheIn.Value, token);
        else
            await cache.SetAsync(cacheKey, value, token);
    }

    private static string DateCacheKey(string cacheKey) => cacheKey + ".created";

    public static DateTime? GetDate(this IRequest req)
    {
        var date = req.Headers[HttpHeaders.Date];
        if (date == null)
            return null;

        if (!DateTime.TryParse(date, new DateTimeFormatInfo(), DateTimeStyles.RoundtripKind, out var value))
            return null;

        return value;
    }

    public static DateTime? GetIfModifiedSince(this IRequest req)
    {
        var ifModifiedSince = req.Headers[HttpHeaders.IfModifiedSince];
        if (ifModifiedSince == null)
            return null;

        if (!DateTime.TryParse(ifModifiedSince, new DateTimeFormatInfo(), DateTimeStyles.RoundtripKind, out var value))
            return null;

        return value;
    }

    public static bool HasValidCache(this ICacheClient cache, IRequest req, string cacheKey, DateTime? checkLastModified, out DateTime? lastModified)
    {
        lastModified = null;

        if (!HostContext.GetPlugin<HttpCacheFeature>().ShouldAddLastModifiedToOptimizedResults())
            return false;

        var ticks = cache.Get<long>(DateCacheKey(cacheKey));
        if (ticks > 0)
        {
            lastModified = new DateTime(ticks, DateTimeKind.Utc);
            if (checkLastModified == null)
                return false;

            return checkLastModified.Value <= lastModified.Value;
        }

        return false;
    }
        
    public struct ValidCache
    {
        public static ValidCache NotValid = new ValidCache(false, DateTime.MinValue);
        public bool IsValid { get; }
        public DateTime LastModified { get; }
        public ValidCache(bool isValid, DateTime lastModified)
        {
            IsValid = isValid;
            LastModified = lastModified;
        }
    }

    public static async Task<ValidCache> HasValidCacheAsync(this ICacheClientAsync cache, IRequest req, string cacheKey, DateTime? checkLastModified, 
        CancellationToken token=default)
    {
        if (!HostContext.GetPlugin<HttpCacheFeature>().ShouldAddLastModifiedToOptimizedResults())
            return ValidCache.NotValid;

        var ticks = await cache.GetAsync<long>(DateCacheKey(cacheKey), token).ConfigAwait();
        if (ticks > 0)
        {
            if (checkLastModified == null)
                return ValidCache.NotValid;

            var lastModified = new DateTime(ticks, DateTimeKind.Utc);
            return new ValidCache(checkLastModified.Value <= lastModified, lastModified);
        }

        return ValidCache.NotValid;
    }

    public static object ResolveFromCache(this ICacheClient cache, string cacheKey, IRequest req)
    {
        var checkModifiedSince = GetIfModifiedSince(req);

        if (!req.ResponseContentType.IsBinary())
        {
            string modifiers = null;
            if (req.ResponseContentType == MimeTypes.Json)
            {
                string jsonp = req.GetJsonpCallback();
                if (jsonp != null)
                    modifiers = ".jsonp," + jsonp.SafeVarName();
            }

            var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, req.ResponseContentType, modifiers);

            var compressionType = req.GetCompressionType();
            bool doCompression = compressionType != null;
            if (doCompression)
            {
                var cacheKeySerializedZip = GetCacheKeyForCompressed(cacheKeySerialized, compressionType);

                if (cache.HasValidCache(req, cacheKeySerializedZip, checkModifiedSince, out var lastModified))
                    return HttpResult.NotModified();

                if (req.Response.GetHeader(HttpHeaders.CacheControl) != null)
                    lastModified = null;

                var compressedResult = cache.Get<byte[]>(cacheKeySerializedZip);
                if (compressedResult != null)
                {
                    return new CompressedResult(
                        compressedResult,
                        compressionType,
                        req.ResponseContentType)
                    {
                        LastModified = lastModified,
                    };
                }
            }
            else
            {
                if (cache.HasValidCache(req, cacheKeySerialized, checkModifiedSince, out _))
                    return HttpResult.NotModified();

                var serializedResult = cache.Get<string>(cacheKeySerialized);
                if (serializedResult != null)
                {
                    return serializedResult;
                }
            }
        }
        else
        {
            var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, req.ResponseContentType, modifiers:null);
            if (cache.HasValidCache(req, cacheKeySerialized, checkModifiedSince, out _))
                return HttpResult.NotModified();

            var serializedResult = cache.Get<byte[]>(cacheKeySerialized);
            if (serializedResult != null)
            {
                return serializedResult;
            }
        }

        return null;
    }

    public static async Task<object> ResolveFromCacheAsync(this ICacheClientAsync cache, string cacheKey, IRequest req, 
        CancellationToken token=default)
    {
        var checkModifiedSince = GetIfModifiedSince(req);

        if (!req.ResponseContentType.IsBinary())
        {
            string modifiers = null;
            if (req.ResponseContentType == MimeTypes.Json)
            {
                string jsonp = req.GetJsonpCallback();
                if (jsonp != null)
                    modifiers = ".jsonp," + jsonp.SafeVarName();
            }

            var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, req.ResponseContentType, modifiers);

            var compressionType = req.GetCompressionType();
            bool doCompression = compressionType != null;
            if (doCompression)
            {
                var cacheKeySerializedZip = GetCacheKeyForCompressed(cacheKeySerialized, compressionType);

                var validCache = await cache.HasValidCacheAsync(req, cacheKeySerializedZip, checkModifiedSince, token).ConfigAwait(); 
                if (validCache.IsValid)
                    return HttpResult.NotModified();
                    
                DateTime? lastModified = validCache.LastModified;
                if (req.Response.GetHeader(HttpHeaders.CacheControl) != null)
                    lastModified = null;

                var compressedResult = await cache.GetAsync<byte[]>(cacheKeySerializedZip, token).ConfigAwait();
                if (compressedResult != null)
                {
                    return new CompressedResult(
                        compressedResult,
                        compressionType,
                        req.ResponseContentType)
                    {
                        LastModified = lastModified,
                    };
                }
            }
            else
            {
                if ((await cache.HasValidCacheAsync(req, cacheKeySerialized, checkModifiedSince, token).ConfigAwait()).IsValid)
                    return HttpResult.NotModified();

                var serializedResult = await cache.GetAsync<string>(cacheKeySerialized, token).ConfigAwait();
                if (serializedResult != null)
                {
                    return serializedResult;
                }
            }
        }
        else
        {
            var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, req.ResponseContentType, modifiers:null);
            if ((await cache.HasValidCacheAsync(req, cacheKeySerialized, checkModifiedSince, token).ConfigAwait()).IsValid)
                return HttpResult.NotModified();

            var serializedResult = await cache.GetAsync<byte[]>(cacheKeySerialized, token).ConfigAwait();
            if (serializedResult != null)
            {
                return serializedResult;
            }
        }

        return null;
    }

    internal static string SerializeToString(this IRequest request, object responseDto)
    {
        var str = responseDto as string;
        return str ?? HostContext.ContentTypes.SerializeToString(request, responseDto);
    }

    public static object Cache(this ICacheClient cache,
        string cacheKey,
        object responseDto,
        IRequest req,
        TimeSpan? expireCacheIn = null)
    {

        req.Response.Dto = responseDto;
        cache.Set(cacheKey, responseDto, expireCacheIn);

        if (!req.ResponseContentType.IsBinary())
        {
            string serializedDto = SerializeToString(req, responseDto);

            string modifiers = null;
            if (req.ResponseContentType.MatchesContentType(MimeTypes.Json))
            {
                var jsonp = req.GetJsonpCallback();
                if (jsonp != null)
                {
                    modifiers = ".jsonp," + jsonp.SafeVarName();
                    serializedDto = jsonp + "(" + serializedDto + ")";

                    //Add a default expire timespan for jsonp requests,
                    //because they aren't cleared when calling ClearCaches()
                    if (expireCacheIn == null)
                        expireCacheIn = HostContext.Config.DefaultJsonpCacheExpiration;
                }
            }

            var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, req.ResponseContentType, modifiers);
            cache.Set(cacheKeySerialized, serializedDto, expireCacheIn);

            var compressionType = req.GetCompressionType();
            bool doCompression = compressionType != null;
            if (doCompression)
            {
                var lastModified = HostContext.GetPlugin<HttpCacheFeature>().ShouldAddLastModifiedToOptimizedResults()
                                   && string.IsNullOrEmpty(req.Response.GetHeader(HttpHeaders.CacheControl))
                    ? DateTime.UtcNow
                    : (DateTime?)null;

                var cacheKeySerializedZip = GetCacheKeyForCompressed(cacheKeySerialized, compressionType);

                byte[] compressedSerializedDto = serializedDto.Compress(compressionType);
                cache.Set(cacheKeySerializedZip, compressedSerializedDto, expireCacheIn);

                if (lastModified != null)
                    cache.Set(DateCacheKey(cacheKeySerializedZip), lastModified.Value.Ticks, expireCacheIn);

                return compressedSerializedDto != null
                    ? new CompressedResult(compressedSerializedDto, compressionType, req.ResponseContentType)
                    {
                        Status = req.Response.StatusCode,
                        LastModified = lastModified,
                    }
                    : null;
            }

            return serializedDto;
        }
        else
        {
            string modifiers = null;
            byte[] serializedDto = HostContext.ContentTypes.SerializeToBytes(req, responseDto);
            var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, req.ResponseContentType, modifiers);
            cache.Set(cacheKeySerialized, serializedDto, expireCacheIn);
            return serializedDto;
        }
    }

    public static async Task<object> CacheAsync(this ICacheClientAsync cache,
        string cacheKey,
        object responseDto,
        IRequest req,
        TimeSpan? expireCacheIn = null,
        CancellationToken token=default)
    {

        req.Response.Dto = responseDto;
        await cache.SetAsync(cacheKey, responseDto, expireCacheIn, token).ConfigAwait();

        if (!req.ResponseContentType.IsBinary())
        {
            string serializedDto = SerializeToString(req, responseDto);

            string modifiers = null;
            if (req.ResponseContentType.MatchesContentType(MimeTypes.Json))
            {
                var jsonp = req.GetJsonpCallback();
                if (jsonp != null)
                {
                    modifiers = ".jsonp," + jsonp.SafeVarName();
                    serializedDto = jsonp + "(" + serializedDto + ")";

                    //Add a default expire timespan for jsonp requests,
                    //because they aren't cleared when calling ClearCaches()
                    if (expireCacheIn == null)
                        expireCacheIn = HostContext.Config.DefaultJsonpCacheExpiration;
                }
            }

            var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, req.ResponseContentType, modifiers);
            await cache.SetAsync(cacheKeySerialized, serializedDto, expireCacheIn, token).ConfigAwait();

            var compressionType = req.GetCompressionType();
            bool doCompression = compressionType != null;
            if (doCompression)
            {
                var lastModified = HostContext.GetPlugin<HttpCacheFeature>().ShouldAddLastModifiedToOptimizedResults()
                                   && string.IsNullOrEmpty(req.Response.GetHeader(HttpHeaders.CacheControl))
                    ? DateTime.UtcNow
                    : (DateTime?)null;

                var cacheKeySerializedZip = GetCacheKeyForCompressed(cacheKeySerialized, compressionType);

                byte[] compressedSerializedDto = serializedDto.Compress(compressionType);
                await cache.SetAsync(cacheKeySerializedZip, compressedSerializedDto, expireCacheIn, token).ConfigAwait();

                if (lastModified != null)
                    await cache.SetAsync(DateCacheKey(cacheKeySerializedZip), lastModified.Value.Ticks, expireCacheIn, token).ConfigAwait();

                return compressedSerializedDto != null
                    ? new CompressedResult(compressedSerializedDto, compressionType, req.ResponseContentType)
                    {
                        Status = req.Response.StatusCode,
                        LastModified = lastModified,
                    }
                    : null;
            }

            return serializedDto;
        }
        else
        {
            string modifiers = null;
            byte[] serializedDto = HostContext.ContentTypes.SerializeToBytes(req, responseDto);
            var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, req.ResponseContentType, modifiers);
            await cache.SetAsync(cacheKeySerialized, serializedDto, expireCacheIn, token).ConfigAwait();
            return serializedDto;
        }
    }

    private static List<string> GetAllContentCacheKeys(string[] cacheKeys)
    {
        var allContentTypes = new List<string>(HostContext.ContentTypes.ContentTypeFormats.Values) {
            MimeTypes.XmlText, MimeTypes.JsonText, MimeTypes.JsvText
        };

        var allCacheKeys = new List<string>();

        foreach (var cacheKey in cacheKeys)
        {
            allCacheKeys.Add(cacheKey);
            foreach (var serializedExt in allContentTypes)
            {
                var serializedCacheKey = GetCacheKeyForSerialized(cacheKey, serializedExt, null);
                allCacheKeys.Add(serializedCacheKey);
                allCacheKeys.Add(DateCacheKey(serializedCacheKey));

                foreach (var compressionType in CompressionTypes.AllCompressionTypes)
                {
                    var compressedCacheKey = GetCacheKeyForCompressed(serializedCacheKey, compressionType);
                    allCacheKeys.Add(compressedCacheKey);
                    allCacheKeys.Add(DateCacheKey(compressedCacheKey));
                }
            }
        }

        return allCacheKeys;
    }

    public static void ClearCaches(this ICacheClient cache, params string[] cacheKeys)
    {
        var allCacheKeys = GetAllContentCacheKeys(cacheKeys);
        cache.RemoveAll(allCacheKeys);
    }

    public static async Task ClearCachesAsync(this ICacheClientAsync cache, string[] cacheKeys, CancellationToken token=default)
    {
        var allCacheKeys = GetAllContentCacheKeys(cacheKeys);
        await cache.RemoveAllAsync(allCacheKeys, token);
    }

    public static string GetCacheKeyForSerialized(string cacheKey, string mimeType, string modifiers)
    {
        return cacheKey + MimeTypes.GetExtension(mimeType) + modifiers;
    }

    public static string GetCacheKeyForCompressed(string cacheKeySerialized, string compressionType)
    {
        return cacheKeySerialized + "." + compressionType;
    }

    /// <summary>
    /// Removes items from cache that have keys matching the specified wildcard pattern
    /// </summary>
    /// <param name="cacheClient">Cache client</param>
    /// <param name="pattern">The wildcard, where "*" means any sequence of characters and "?" means any single character.</param>
    public static void RemoveByPattern(this ICacheClient cacheClient, string pattern)
    {
        if (!(cacheClient is IRemoveByPattern canRemoveByPattern))
            throw new NotImplementedException(
                "IRemoveByPattern is not implemented on: " + cacheClient.GetType().FullName);

        canRemoveByPattern.RemoveByPattern(pattern);
    }

    /// <summary>
    /// Removes items from cache that have keys matching the specified wildcard pattern
    /// </summary>
    /// <param name="cacheClient">Cache client</param>
    /// <param name="pattern">The wildcard, where "*" means any sequence of characters and "?" means any single character.</param>
    /// <param name="token"></param>
    public static Task RemoveByPatternAsync(this ICacheClientAsync cacheClient, string pattern, CancellationToken token=default)
    {
        if (!(cacheClient is IRemoveByPatternAsync canRemoveByPattern))
            throw new NotImplementedException(
                "IRemoveByPattern is not implemented on: " + cacheClient.GetType().FullName);

        return canRemoveByPattern.RemoveByPatternAsync(pattern, token);
    }

    /// <summary>
    /// Removes items from the cache based on the specified regular expression pattern
    /// </summary>
    /// <param name="cacheClient">Cache client</param>
    /// <param name="regex">Regular expression pattern to search cache keys</param>
    public static void RemoveByRegex(this ICacheClient cacheClient, string regex)
    {
        if (!(cacheClient is IRemoveByPattern canRemoveByPattern))
            throw new NotImplementedException("IRemoveByPattern is not implemented by: " + cacheClient.GetType().FullName);

        canRemoveByPattern.RemoveByRegex(regex);
    }

    /// <summary>
    /// Removes items from the cache based on the specified regular expression pattern
    /// </summary>
    /// <param name="cacheClient">Cache client</param>
    /// <param name="regex">Regular expression pattern to search cache keys</param>
    public static Task RemoveByRegexAsync(this ICacheClientAsync cacheClient, string regex)
    {
        if (!(cacheClient is IRemoveByPatternAsync canRemoveByPattern))
            throw new NotImplementedException("IRemoveByPattern is not implemented by: " + cacheClient.GetType().FullName);

        return canRemoveByPattern.RemoveByRegexAsync(regex);
    }

    public static IEnumerable<string> GetKeysByPattern(this ICacheClient cache, string pattern)
    {
        if (!(cache is ICacheClientExtended extendedCache))
            throw new NotImplementedException("ICacheClientExtended is not implemented by: " + cache.GetType().FullName);

        return extendedCache.GetKeysByPattern(pattern);
    }

    public static IEnumerable<string> GetAllKeys(this ICacheClient cache)
    {
        return cache.GetKeysByPattern("*");
    }

    public static IEnumerable<string> GetKeysStartingWith(this ICacheClient cache, string prefix)
    {
        return cache.GetKeysByPattern(prefix + "*");
    }
        
    public static IAsyncEnumerable<string> GetKeysByPatternAsync(this ICacheClientAsync cache, string pattern)
    {
        return cache.GetKeysByPatternAsync(pattern);
    }

    public static async IAsyncEnumerable<string> GetAllKeysAsync(this ICacheClientAsync cache)
    {
        await foreach (var key in cache.GetKeysByPatternAsync("*"))
        {
            yield return key;
        }
    }

    public static async IAsyncEnumerable<string> GetKeysStartingWithAsync(this ICacheClientAsync cache, string prefix)
    {
        await foreach (var key in cache.GetKeysByPatternAsync(prefix + "*"))
        {
            yield return key;
        }
    }

    public static T GetOrCreate<T>(this ICacheClient cache,
        string key, Func<T> createFn)
    {
        var value = cache.Get<T>(key);
        if (Equals(value, default(T)))
        {
            value = createFn();
            cache.Set(key, value);
        }
        return value;
    }

    public static async Task<T> GetOrCreateAsync<T>(this ICacheClientAsync cache,
        string key, Func<Task<T>> createFn)
    {
        var value = await cache.GetAsync<T>(key);
        if (Equals(value, default(T)))
        {
            value = await createFn().ConfigAwait();
            await cache.SetAsync(key, value);
        }
        return value;
    }

    public static T GetOrCreate<T>(this ICacheClient cache,
        string key, TimeSpan expiresIn, Func<T> createFn)
    {
        var value = cache.Get<T>(key);
        if (Equals(value, default(T)))
        {
            value = createFn();
            cache.Set(key, value, expiresIn);
        }
        return value;
    }

    public static async Task<T> GetOrCreateAsync<T>(this ICacheClientAsync cache,
        string key, TimeSpan expiresIn, Func<Task<T>> createFn)
    {
        var value = await cache.GetAsync<T>(key);
        if (Equals(value, default(T)))
        {
            value = await createFn().ConfigAwait();
            await cache.SetAsync(key, value, expiresIn);
        }
        return value;
    }

    public static TimeSpan? GetTimeToLive(this ICacheClient cache, string key)
    {
        if (!(cache is ICacheClientExtended extendedCache))
            throw new Exception("GetTimeToLive is not implemented by: " + cache.GetType().FullName);

        return extendedCache.GetTimeToLive(key);
    }
}