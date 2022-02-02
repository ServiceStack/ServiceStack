//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_resource_type_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Resource type for a request.
    /// </summary>
    public enum CefResourceType
    {
        /// <summary>
        /// Top level page.
        /// </summary>
        MainFrame = 0,

        /// <summary>
        /// Frame or iframe.
        /// </summary>
        SubFrame,

        /// <summary>
        /// CSS stylesheet.
        /// </summary>
        Stylesheet,

        /// <summary>
        /// External script.
        /// </summary>
        Script,

        /// <summary>
        /// Image (jpg/gif/png/etc).
        /// </summary>
        Image,

        /// <summary>
        /// Font.
        /// </summary>
        FontResource,

        /// <summary>
        /// Some other subresource. This is the default type if the actual type is
        /// unknown.
        /// </summary>
        SubResource,

        /// <summary>
        /// Object (or embed) tag for a plugin, or a resource that a plugin requested.
        /// </summary>
        Object,

        /// <summary>
        /// Media resource.
        /// </summary>
        Media,

        /// <summary>
        /// Main resource of a dedicated worker.
        /// </summary>
        Worker,

        /// <summary>
        /// Main resource of a shared worker.
        /// </summary>
        SharedWorker,

        /// <summary>
        /// Explicitly requested prefetch.
        /// </summary>
        Prefetch,

        /// <summary>
        /// Favicon.
        /// </summary>
        Favicon,

        /// <summary>
        /// XMLHttpRequest.
        /// </summary>
        Xhr,

        /// <summary>
        /// A request for a &lt;ping&gt;.
        /// </summary>
        Ping,

        /// <summary>
        /// Main resource of a service worker.
        /// </summary>
        ServiceWorker,

        /// <summary>
        /// A report of Content Security Policy violations.
        /// </summary>
        CspReport,

        /// <summary>
        /// A resource that a plugin requested.
        /// </summary>
        PluginResource,
    }
}
