using System;
using ServiceStack.Caching;

namespace ServiceStack
{
    public class CacheInfo
    {
        public string CacheKey { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public TimeSpan? ExpiresIn { get; set; }
        public string ETag { get; set; }
        public TimeSpan? Age { get; set; }
        public TimeSpan? MaxAge { get; set; }
        public DateTime? Expires { get; set; }
        public DateTime? LastModified { get; set; }
        public CacheControl CacheControl { get; set; }
        public bool VaryByUser { get; set; }
        public bool LocalCache { get; set; }
    }

    public static class CacheInfoExtensions
    {
        public static CacheInfo ToCacheInfo(this HttpResult httpResult)
        {
            if (httpResult == null)
                return null;

            return new CacheInfo
            {
                ETag = httpResult.ETag,
                Age = httpResult.Age,
                MaxAge = httpResult.MaxAge,
                Expires = httpResult.Expires,
                LastModified = httpResult.LastModified,
                CacheControl = httpResult.CacheControl,
            };
        }
    }
}