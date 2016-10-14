using System;
using ServiceStack.Html;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Cache the Response of a Service
    /// </summary>
    public class CacheResponseAttribute : RequestFilterAttribute
    {
        /// <summary>
        /// Cache expiry in seconds
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// MaxAge in seconds
        /// </summary>
        public int MaxAge { get; set; }

        /// <summary>
        /// Cache-Control HTTP Headers
        /// </summary>
        public CacheControl CacheControl { get; set; }

        /// <summary>
        /// Vary cache per user
        /// </summary>
        public bool VaryByUser { get; set; }

        /// <summary>
        /// Vary cache for users in these roles
        /// </summary>
        public string[] VaryByRoles { get; set; }

        /// <summary>
        /// Use HostContext.LocalCache or HostContext.Cache
        /// </summary>
        public bool LocalCache { get; set; }

        /// <summary>
        /// Skip compression for this Cache Result
        /// </summary>
        public bool NoCompression { get; set; }

        public CacheResponseAttribute()
        {
            MaxAge = -1;
        }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (req.Verb != HttpMethods.Get && req.Verb != HttpMethods.Head)
                return;
            if (req.IsInProcessRequest())
                return;

            var feature = HostContext.GetPlugin<HttpCacheFeature>();
            if (feature == null)
                throw new NotSupportedException(ErrorMessages.CacheFeatureMustBeEnabled.Fmt("[CacheResponse]"));

            var keyBase = "res:" + req.RawUrl;
            var keySuffix = MimeTypes.GetExtension(req.ResponseContentType);

            var modifiers = "";
            if (req.ResponseContentType == MimeTypes.Json)
            {
                string jsonp = req.GetJsonpCallback();
                if (jsonp != null)
                    modifiers = "jsonp:" + jsonp.SafeVarName();
            }

            if (VaryByUser)
                modifiers += (modifiers.Length > 0 ? "+" : "") + "user:" + req.GetSessionId();

            if (VaryByRoles != null && VaryByRoles.Length > 0)
            {
                var userSession = req.GetSession();
                if (userSession != null)
                {
                    var authRepo = HostContext.AppHost.GetAuthRepository(req);
                    using (authRepo as IDisposable)
                    {
                        foreach (var role in VaryByRoles)
                        {
                            if (userSession.HasRole(role, authRepo))
                                modifiers += (modifiers.Length > 0 ? "+" : "") + "role:" + role;
                        }
                    }
                }
            }

            if (modifiers.Length > 0)
                keySuffix += "+" + modifiers;

            var cacheInfo = new CacheInfo
            {
                KeyBase = keyBase,
                KeyModifiers = keySuffix,
                ExpiresIn = Duration > 0 ? TimeSpan.FromSeconds(Duration) : (TimeSpan?)null,
                MaxAge = MaxAge >= 0 ? TimeSpan.FromSeconds(MaxAge) : (TimeSpan?)null,
                CacheControl = CacheControl,
                VaryByUser = VaryByUser,
                LocalCache = LocalCache,
                NoCompression = NoCompression,
            };

            if (req.HandleValidCache(cacheInfo))
                return;

            req.Items[Keywords.CacheInfo] = cacheInfo;
        }
    }

    public static class CacheResponseExtensions
    {
        public static string LastModifiedKey(this CacheInfo cacheInfo)
        {
            return "date:" + cacheInfo.CacheKey;
        }

        public static bool HandleValidCache(this IRequest req, CacheInfo cacheInfo)
        {
            if (cacheInfo == null)
                return false;

            var res = req.Response;
            var cache = cacheInfo.LocalCache ? HostContext.LocalCache : HostContext.Cache;

            DateTime? lastModified = null;

            var doHttpCaching = cacheInfo.MaxAge != null || cacheInfo.CacheControl != CacheControl.None;
            if (doHttpCaching)
            {
                lastModified = cache.Get<DateTime?>(cacheInfo.LastModifiedKey());
                if (req.HasValidCache(lastModified))
                {
                    res.EndNotModified();
                    return true;
                }
            }

            var encoding = !cacheInfo.NoCompression 
                ? req.GetCompressionType()
                : null;

            var responseBytes = encoding != null
                ? cache.Get<byte[]>(cacheInfo.CacheKey + "." + encoding)
                : cache.Get<byte[]>(cacheInfo.CacheKey);

            if (responseBytes != null)
            {
                if (encoding != null)
                    res.AddHeader(HttpHeaders.ContentEncoding, encoding);
                if (cacheInfo.VaryByUser)
                    res.AddHeader(HttpHeaders.Vary, "Cookie");

                var cacheControl = HostContext.GetPlugin<HttpCacheFeature>().BuildCacheControlHeader(cacheInfo);
                if (cacheControl != null)
                    res.AddHeader(HttpHeaders.CacheControl, cacheControl);

                if (!doHttpCaching)
                    lastModified = cache.Get<DateTime?>(cacheInfo.LastModifiedKey());

                if (lastModified != null)
                    res.AddHeader(HttpHeaders.LastModified, lastModified.Value.ToUniversalTime().ToString("r"));

                res.WriteBytesToResponse(responseBytes, req.ResponseContentType);
                return true;
            }

            return false;
        }
    }
}