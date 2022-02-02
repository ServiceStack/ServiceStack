using System;

namespace ServiceStack
{
    public class CacheInfo
    {
        /// <summary>
        /// The CacheKey to be use store the response against
        /// </summary>
        public string CacheKey => KeyBase + KeyModifiers;

        /// <summary>
        /// The base Cache Key used to cache the Service response
        /// </summary>
        public string KeyBase { get; set; }

        /// <summary>
        /// Additional CacheKey Modifiers used to cache different outputs for a single Service Response
        /// </summary>
        public string KeyModifiers { get; set; }

        /// <summary>
        /// How long to cache the resource for. Fallsback to HttpCacheFeature.DefaultExpiresIn
        /// </summary>
        public TimeSpan? ExpiresIn { get; set; }

        /// <summary>
        /// The unique ETag returned for this resource clients can use to determine whether their local version has changed
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// The Age for this resource returned to clients
        /// </summary>
        public TimeSpan? Age { get; set; }

        /// <summary>
        /// The MaxAge returned to clients to indicate how long they can use their local cache before re-validating
        /// </summary>
        public TimeSpan? MaxAge { get; set; }

        /// <summary>
        /// The LastModified date to use for the Cache and HTTP Header 
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Cache-Control HTTP Headers
        /// </summary>
        public CacheControl CacheControl { get; set; }

        /// <summary>
        /// Create unique cache per user
        /// </summary>
        public bool VaryByUser { get; set; }

        /// <summary>
        /// Use HostContext.LocalCache or HostContext.Cache
        /// </summary>
        public bool LocalCache { get; set; }

        /// <summary>
        /// Skip compression for this Cache Result
        /// </summary>
        public bool NoCompression { get; set; }
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
                LastModified = httpResult.LastModified,
                CacheControl = httpResult.CacheControl,
            };
        }
    }
}