using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Caching;
using ServiceStack.Html;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Cache the Response of a Service
/// </summary>
public class CacheResponseAttribute : RequestFilterAsyncAttribute
{
    /// <summary>
    /// Cache expiry in seconds
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// MaxAge in seconds
    /// </summary>
    public int MaxAge { get; set; } = -1;

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
    /// Vary cache for different HTTP Headers
    /// </summary>
    public string[] VaryByHeaders { get; set; }

    /// <summary>
    /// Use HostContext.LocalCache or HostContext.Cache
    /// </summary>
    public bool LocalCache { get; set; }

    /// <summary>
    /// Skip compression for this Cache Result
    /// </summary>
    public bool NoCompression { get; set; }

    public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
    {
        if (req.Verb != HttpMethods.Get && req.Verb != HttpMethods.Head)
            return;
        if (req.IsInProcessRequest())
            return;

        var feature = HostContext.GetPlugin<HttpCacheFeature>();
        if (feature == null)
            throw new NotSupportedException(ErrorMessages.CacheFeatureMustBeEnabled.LocalizeFmt(req, "[CacheResponse]"));
        if (feature.DisableCaching)
            return;

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

        if (VaryByRoles is { Length: > 0 })
        {
            var userSession = await req.GetSessionAsync().ConfigAwait();
            if (userSession != null)
            {
                var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(req);
                await using (authRepo as IAsyncDisposable)
                {
                    var allRoles = await userSession.GetRolesAsync(authRepo).ConfigAwait();
                    foreach (var role in VaryByRoles)
                    {
                        if (allRoles.Contains(role))
                            modifiers += (modifiers.Length > 0 ? "+" : "") + "role:" + role;
                    }
                }
            }
        }

        if (VaryByHeaders is { Length: > 0 })
        {
            foreach (var header in VaryByHeaders)
            {
                var value = req.GetHeader(header);
                if (!string.IsNullOrEmpty(value))
                {
                    modifiers += (modifiers.Length > 0 ? "+" : "") + $"{header}:{value}";
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

        if (await req.HandleValidCache(cacheInfo).ConfigAwait())
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

    public static async Task<bool> HandleValidCache(this IRequest req, CacheInfo cacheInfo, CancellationToken token=default)
    {
        if (cacheInfo == null)
            return false;

        ICacheClient cache;
        ICacheClientAsync cacheAsync = null; // only non-null if native ICacheClientAsync exists
        if (cacheInfo.LocalCache)
            cache = HostContext.AppHost.GetMemoryCacheClient(req);
        else
            HostContext.AppHost.TryGetNativeCacheClient(req, out cache, out cacheAsync);

        var cacheControl = HostContext.GetPlugin<HttpCacheFeature>().BuildCacheControlHeader(cacheInfo);

        var res = req.Response;
        DateTime? lastModified = null;

        var doHttpCaching = cacheInfo.MaxAge != null || cacheInfo.CacheControl != CacheControl.None;
        if (doHttpCaching)
        {
            lastModified = cacheAsync != null 
                ? await cacheAsync.GetAsync<DateTime?>(cacheInfo.LastModifiedKey(), token).ConfigAwait()
                : cache.Get<DateTime?>(cacheInfo.LastModifiedKey());
            if (req.HasValidCache(lastModified))
            {
                if (cacheControl != null)
                    res.AddHeader(HttpHeaders.CacheControl, cacheControl);

                res.EndNotModified();
                return true;
            }
        }

        var encoding = !cacheInfo.NoCompression 
            ? req.GetCompressionType()
            : null;

        var useCacheKey = encoding != null
            ? cacheInfo.CacheKey + "." + encoding
            : cacheInfo.CacheKey;
            
        var responseBytes = cacheAsync != null
            ? await cacheAsync.GetAsync<byte[]>(useCacheKey, token).ConfigAwait()
            : cache.Get<byte[]>(useCacheKey);

        if (responseBytes != null)
        {
            if (encoding != null)
                res.AddHeader(HttpHeaders.ContentEncoding, encoding);
            if (cacheInfo.VaryByUser)
                res.AddHeader(HttpHeaders.Vary, "Cookie");

            if (cacheControl != null)
                res.AddHeader(HttpHeaders.CacheControl, cacheControl);

            if (!doHttpCaching)
            {
                lastModified = cacheAsync != null ?
                    await cacheAsync.GetAsync<DateTime?>(cacheInfo.LastModifiedKey(), token).ConfigAwait() :
                    cache.Get<DateTime?>(cacheInfo.LastModifiedKey());
            }

            if (lastModified != null)
                res.AddHeader(HttpHeaders.LastModified, lastModified.Value.ToUniversalTime().ToString("r"));

            await res.WriteBytesToResponse(responseBytes, req.ResponseContentType, token).ConfigAwait();
            return true;
        }

        return false;
    }
}