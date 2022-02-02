//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_urlrequest_flags_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// Flags used to customize the behavior of CefURLRequest.
    /// </summary>
    [Flags]
    public enum CefUrlRequestOptions
    {
        /// <summary>
        /// Default behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// If set the cache will be skipped when handling the request. Setting this
        /// value is equivalent to specifying the "Cache-Control: no-cache" request
        /// header. Setting this value in combination with UR_FLAG_ONLY_FROM_CACHE will
        /// cause the request to fail.
        /// </summary>
        SkipCache = 1 << 0,

        /// <summary>
        /// If set the request will fail if it cannot be served from the cache (or some
        /// equivalent local store). Setting this value is equivalent to specifying the
        /// "Cache-Control: only-if-cached" request header. Setting this value in
        /// combination with UR_FLAG_SKIP_CACHE or UR_FLAG_DISABLE_CACHE will cause the
        /// request to fail.
        /// </summary>
        OnlyFromCache = 1 << 1,

        /// <summary>
        /// If set the cache will not be used at all. Setting this value is equivalent
        /// to specifying the "Cache-Control: no-store" request header. Setting this
        /// value in combination with UR_FLAG_ONLY_FROM_CACHE will cause the request to
        /// fail.
        /// </summary>
        DisableCache = 1 << 2,

        /// <summary>
        /// If set user name, password, and cookies may be sent with the request, and
        /// cookies may be saved from the response.
        /// </summary>
        AllowStoredCredentials = 1 << 3,

        /// <summary>
        /// If set upload progress events will be generated when a request has a body.
        /// </summary>
        ReportUploadProgress = 1 << 4,

        /// <summary>
        /// If set the <c>CefUrlRequestClient.OnDownloadData</c> method will not be called.
        /// </summary>
        NoDownloadData = 1 << 5,

        /// <summary>
        /// If set 5XX redirect errors will be propagated to the observer instead of
        /// automatically re-tried. This currently only applies for requests
        /// originated in the browser process.
        /// </summary>
        NoRetryOn5XX = 1 << 6,

        /// <summary>
        /// If set 3XX responses will cause the fetch to halt immediately rather than
        /// continue through the redirect.
        /// </summary>
        StopOnRedirect = 1 << 7,
    }
}
