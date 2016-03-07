using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack
{
    public class HttpCacheFeature : IPlugin
    {
        public TimeSpan DefaultMaxAge { get; set; }

        public Func<string, string> CacheControlFilter { get; set; }

        public HttpCacheFeature()
        {
            DefaultMaxAge = TimeSpan.FromHours(1);
        }

        public void Register(IAppHost appHost)
        {
            appHost.GlobalResponseFilters.Add(HandleCacheResponses);
        }

        public void HandleCacheResponses(IRequest req, IResponse res, object response)
        {
            var httpResult = response as HttpResult;
            if (httpResult == null)
                return;

            if (httpResult.StatusCode != HttpStatusCode.OK && 
                httpResult.StatusCode != HttpStatusCode.NotModified)
                return;

            if (httpResult.LastModified != null)
                httpResult.Headers[HttpHeaders.LastModified] = httpResult.LastModified.Value.ToUniversalTime().ToString("r");

            if (httpResult.ETag != null)
                httpResult.Headers[HttpHeaders.ETag] = httpResult.ETag;

            if (httpResult.Expires != null)
                httpResult.Headers[HttpHeaders.Expires] = httpResult.Expires.Value.ToUniversalTime().ToString("r");

            if (httpResult.Age != null)
                httpResult.Headers[HttpHeaders.Age] = httpResult.Age.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture);

            var maxAge = httpResult.MaxAge;
            if (maxAge == null && (httpResult.LastModified != null || httpResult.ETag != null))
                maxAge = DefaultMaxAge;

            var cacheHeader = new List<string>();
            if (maxAge != null)
                cacheHeader.Add("max-age=" + maxAge.Value.TotalSeconds);

            if (httpResult.CacheControl != CacheControl.None)
            {
                var cache = httpResult.CacheControl;
                if (cache.HasFlag(CacheControl.Public))
                    cacheHeader.Add("public");
                else if (cache.HasFlag(CacheControl.Private))
                    cacheHeader.Add("private");

                if (cache.HasFlag(CacheControl.NoCache))
                    cacheHeader.Add("no-cache");
                if (cache.HasFlag(CacheControl.NoStore))
                    cacheHeader.Add("no-store");
                if (cache.HasFlag(CacheControl.MustRevalidate))
                    cacheHeader.Add("must-revalidate");
            }

            if (cacheHeader.Count > 0)
            {
                var cacheControl = cacheHeader.ToArray().Join(", ");
                if (CacheControlFilter != null)
                    cacheControl = CacheControlFilter(cacheControl);

                if (cacheControl != null)
                    httpResult.Headers[HttpHeaders.CacheControl] = cacheControl;
            }

            if (req.ETagMatch(httpResult.ETag) || req.NotModifiedSince(httpResult.LastModified))
            {
                res.EndNotModified();
            }
        }
    }

    public static class HttpCacheExtensions
    {
        public static void EndNotModified(this IResponse res, string description=null)
        {
            res.StatusCode = 304;
            res.StatusDescription = description ?? HostContext.ResolveLocalizedString(LocalizedStrings.NotModified);
            res.EndRequest();
        }

        public static bool ETagMatch(this IRequest req, string eTag)
        {
            return eTag != null && eTag == req.Headers[HttpHeaders.IfNoneMatch];
        }

        public static bool NotModifiedSince(this IRequest req, DateTime? lastModified)
        {
            if (lastModified != null)
            {
                var ifModifiedSince = req.Headers[HttpHeaders.IfModifiedSince];
                if (ifModifiedSince != null)
                {
                    DateTime modifiedSinceDate;
                    if (DateTime.TryParse(ifModifiedSince, out modifiedSinceDate))
                        return modifiedSinceDate <= lastModified.Value;
                }
            }

            return false;
        }

        public static bool HasValidCache(this IRequest req, string eTag)
        {
            return req.ETagMatch(eTag);
        }

        public static bool HasValidCache(this IRequest req, DateTime? lastModified)
        {
            return req.NotModifiedSince(lastModified);
        }

        public static bool HasValidCache(this IRequest req, string eTag, DateTime? lastModified)
        {
            return req.ETagMatch(eTag) || req.NotModifiedSince(lastModified);
        }
    }
}