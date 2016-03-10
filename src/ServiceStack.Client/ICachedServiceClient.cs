#if !SL5
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ServiceStack
{
    public interface ICachedServiceClient : IServiceClient
    {
        int CacheCount { get; }
        long CacheHits { get; }
        long NotModifiedHits { get; }
        long ErrorFallbackHits { get; }
        long CachesAdded { get; }
        long CachesRemoved { get; }

        void SetCache(ConcurrentDictionary<string, HttpCacheEntry> cache);
        int RemoveCachesOlderThan(TimeSpan age);
        int RemoveExpiredCachesOlderThan(TimeSpan age);
    }

    public static class ICachedServiceClientExtensions
    {
        public static void ClearCache(this ICachedServiceClient client)
        {
            client.SetCache(new ConcurrentDictionary<string, HttpCacheEntry>());
        }

        public static Dictionary<string, string> GetStats(this ICachedServiceClient client)
        {
            return new Dictionary<string,string>
            {
                { "CacheCount", client.CacheCount + "" },
                { "CacheHits", client.CacheHits + "" },
                { "NotModifiedHits", client.NotModifiedHits + "" },
                { "ErrorFallbackHits", client.ErrorFallbackHits + "" },
                { "CachesAdded", client.CachesAdded + "" },
                { "CachesRemoved", client.CachesRemoved + "" },
            };
        }
    }
}
#endif
