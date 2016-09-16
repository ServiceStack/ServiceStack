﻿using System;
using System.Collections.Generic;
using ServiceStack.Caching;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class CacheClientExtensions
    {
        public static void Set<T>(this ICacheClient cacheClient, string cacheKey, T value, TimeSpan? expireCacheIn)
        {
            if (expireCacheIn.HasValue)
                cacheClient.Set(cacheKey, value, expireCacheIn.Value);
            else
                cacheClient.Set(cacheKey, value);
        }

        private static string DateCacheKey(string cacheKey)
        {
            return cacheKey + ".created";
        }

        private static DateTime? CheckModifiedSince(IRequest req)
        {
            var ifModifiedSince = req.Headers[HttpHeaders.IfModifiedSince];
            if (ifModifiedSince == null)
                return null;

            DateTime checkLastModified;
            if (!DateTime.TryParse(ifModifiedSince, out checkLastModified))
                return null;

            return checkLastModified;
        }

        public static bool HasValidCache(this ICacheClient cacheClient, IRequest req, string cacheKey, DateTime? checkLastModified, out DateTime? lastModified)
        {
            lastModified = null;

            if (!HostContext.GetPlugin<HttpCacheFeature>().ShouldAddLastModifiedToOptimizedResults())
                return false;

            var ticks = cacheClient.Get<long>(DateCacheKey(cacheKey));
            if (ticks > 0)
            {
                lastModified = new DateTime(ticks, DateTimeKind.Utc);
                if (checkLastModified == null)
                    return false;

                return checkLastModified.Value <= lastModified.Value;
            }

            return false;
        }

        public static object ResolveFromCache(this ICacheClient cacheClient,
            string cacheKey,
            IRequest request)
        {
            DateTime? lastModified;
            var checkModifiedSince = CheckModifiedSince(request);

            string modifiers = null;
            if (!request.ResponseContentType.IsBinary())
            {
                if (request.ResponseContentType == MimeTypes.Json)
                {
                    string jsonp = request.GetJsonpCallback();
                    if (jsonp != null)
                        modifiers = ".jsonp," + jsonp.SafeVarName();
                }

                var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, request.ResponseContentType, modifiers);

                var compressionType = request.GetCompressionType();
                bool doCompression = compressionType != null;
                if (doCompression)
                {
                    var cacheKeySerializedZip = GetCacheKeyForCompressed(cacheKeySerialized, compressionType);

                    if (cacheClient.HasValidCache(request, cacheKeySerializedZip, checkModifiedSince, out lastModified))
                        return HttpResult.NotModified();

                    if (request.Response.GetHeader(HttpHeaders.CacheControl) != null)
                        lastModified = null;

                    var compressedResult = cacheClient.Get<byte[]>(cacheKeySerializedZip);
                    if (compressedResult != null)
                    {
                        return new CompressedResult(
                            compressedResult,
                            compressionType,
                            request.ResponseContentType)
                        {
                            LastModified = lastModified,
                        };
                    }
                }
                else
                {
                    if (cacheClient.HasValidCache(request, cacheKeySerialized, checkModifiedSince, out lastModified))
                        return HttpResult.NotModified();

                    var serializedResult = cacheClient.Get<string>(cacheKeySerialized);
                    if (serializedResult != null)
                    {
                        return serializedResult;
                    }
                }
            }
            else
            {
                var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, request.ResponseContentType, modifiers);
                if (cacheClient.HasValidCache(request, cacheKeySerialized, checkModifiedSince, out lastModified))
                    return HttpResult.NotModified();

                var serializedResult = cacheClient.Get<byte[]>(cacheKeySerialized);
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

        public static object Cache(this ICacheClient cacheClient,
            string cacheKey,
            object responseDto,
            IRequest request,
            TimeSpan? expireCacheIn = null)
        {

            request.Response.Dto = responseDto;
            cacheClient.Set(cacheKey, responseDto, expireCacheIn);

            if (!request.ResponseContentType.IsBinary())
            {
                string serializedDto = SerializeToString(request, responseDto);

                string modifiers = null;
                if (request.ResponseContentType.MatchesContentType(MimeTypes.Json))
                {
                    var jsonp = request.GetJsonpCallback();
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

                var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, request.ResponseContentType, modifiers);
                cacheClient.Set(cacheKeySerialized, serializedDto, expireCacheIn);

                var compressionType = request.GetCompressionType();
                bool doCompression = compressionType != null;
                if (doCompression)
                {
                    var lastModified = HostContext.GetPlugin<HttpCacheFeature>().ShouldAddLastModifiedToOptimizedResults()
                        && request.Response.GetHeader(HttpHeaders.CacheControl) == null
                        ? DateTime.UtcNow
                        : (DateTime?)null;

                    var cacheKeySerializedZip = GetCacheKeyForCompressed(cacheKeySerialized, compressionType);

                    byte[] compressedSerializedDto = serializedDto.Compress(compressionType);
                    cacheClient.Set(cacheKeySerializedZip, compressedSerializedDto, expireCacheIn);

                    if (lastModified != null)
                        cacheClient.Set(DateCacheKey(cacheKeySerializedZip), lastModified.Value.Ticks, expireCacheIn);

                    return compressedSerializedDto != null
                        ? new CompressedResult(compressedSerializedDto, compressionType, request.ResponseContentType)
                          {
                              Status = request.Response.StatusCode,
                              LastModified = lastModified,
                          }
                        : null;
                }

                return serializedDto;
            }
            else
            {
                string modifiers = null;
                byte[] serializedDto = HostContext.ContentTypes.SerializeToBytes(request, responseDto);
                var cacheKeySerialized = GetCacheKeyForSerialized(cacheKey, request.ResponseContentType, modifiers);
                cacheClient.Set(cacheKeySerialized, serializedDto, expireCacheIn);
                return serializedDto;
            }
        }

        public static void ClearCaches(this ICacheClient cacheClient, params string[] cacheKeys)
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

                    foreach (var compressionType in CompressionTypes.AllCompressionTypes)
                    {
                        allCacheKeys.Add(GetCacheKeyForCompressed(serializedCacheKey, compressionType));
                    }
                }
            }

            cacheClient.RemoveAll(allCacheKeys);
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
            var canRemoveByPattern = cacheClient as IRemoveByPattern;
            if (canRemoveByPattern == null)
                throw new NotImplementedException(
                    "IRemoveByPattern is not implemented on: " + cacheClient.GetType().FullName);

            canRemoveByPattern.RemoveByPattern(pattern);
        }
        /// <summary>
        /// Removes items from the cache based on the specified regular expression pattern
        /// </summary>
        /// <param name="cacheClient">Cache client</param>
        /// <param name="regex">Regular expression pattern to search cache keys</param>
        public static void RemoveByRegex(this ICacheClient cacheClient, string regex)
        {
            var canRemoveByPattern = cacheClient as IRemoveByPattern;
            if (canRemoveByPattern == null)
                throw new NotImplementedException("IRemoveByPattern is not implemented by: " + cacheClient.GetType().FullName);

            canRemoveByPattern.RemoveByRegex(regex);
        }

        public static IEnumerable<string> GetKeysByPattern(this ICacheClient cache, string pattern)
        {
            var extendedCache = cache as ICacheClientExtended;
            if (extendedCache == null)
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

        public static TimeSpan? GetTimeToLive(this ICacheClient cache, string key)
        {
            var extendedCache = cache as ICacheClientExtended;
            if (extendedCache == null)
                throw new Exception("GetTimeToLive is not implemented by: " + cache.GetType().FullName);

            return extendedCache.GetTimeToLive(key);
        }
    }
}
