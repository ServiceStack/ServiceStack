namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to provide handler implementations. The handler
    /// instance will not be released until all objects related to the context have
    /// been destroyed.
    /// </summary>
    public abstract unsafe partial class CefRequestContextHandler
    {
        private void on_request_context_initialized(cef_request_context_handler_t* self, cef_request_context_t* request_context)
        {
            CheckSelf(self);

            var mRequestContext = CefRequestContext.FromNative(request_context);
            OnRequestContextInitialized(mRequestContext);
        }

        /// <summary>
        /// Called on the browser process UI thread immediately after the request
        /// context has been initialized.
        /// </summary>
        protected virtual void OnRequestContextInitialized(CefRequestContext requestContext) { }


        private int on_before_plugin_load(cef_request_context_handler_t* self, cef_string_t* mime_type, cef_string_t* plugin_url, int is_main_frame, cef_string_t* top_origin_url, cef_web_plugin_info_t* plugin_info, CefPluginPolicy* plugin_policy)
        {
            CheckSelf(self);

            var mMimeType = cef_string_t.ToString(mime_type);
            var mPluginUrl = cef_string_t.ToString(plugin_url);
            var mIsMainFrame = is_main_frame != 0;
            var mTopOriginUrl = cef_string_t.ToString(top_origin_url);
            var mPluginInfo = CefWebPluginInfo.FromNative(plugin_info);
            var mPluginPolicy = *plugin_policy;

            var result = OnBeforePluginLoad(mMimeType, mPluginUrl, mIsMainFrame, mTopOriginUrl, mPluginInfo, ref mPluginPolicy);

            *plugin_policy = mPluginPolicy;

            return result ? 1 : 0;
        }

        /// <summary>
        /// Called on multiple browser process threads before a plugin instance is
        /// loaded. |mime_type| is the mime type of the plugin that will be loaded.
        /// |plugin_url| is the content URL that the plugin will load and may be empty.
        /// |is_main_frame| will be true if the plugin is being loaded in the main
        /// (top-level) frame, |top_origin_url| is the URL for the top-level frame that
        /// contains the plugin when loading a specific plugin instance or empty when
        /// building the initial list of enabled plugins for 'navigator.plugins'
        /// JavaScript state. |plugin_info| includes additional information about the
        /// plugin that will be loaded. |plugin_policy| is the recommended policy.
        /// Modify |plugin_policy| and return true to change the policy. Return false
        /// to use the recommended policy. The default plugin policy can be set at
        /// runtime using the `--plugin-policy=[allow|detect|block]` command-line flag.
        /// Decisions to mark a plugin as disabled by setting |plugin_policy| to
        /// PLUGIN_POLICY_DISABLED may be cached when |top_origin_url| is empty. To
        /// purge the plugin list cache and potentially trigger new calls to this
        /// method call CefRequestContext::PurgePluginListCache.
        /// </summary>
        protected virtual bool OnBeforePluginLoad(string mimeType, string pluginUrl, bool isMainFrame, string topOriginUrl, CefWebPluginInfo pluginInfo, ref CefPluginPolicy pluginPolicy)
        {
            return false;
        }


        private cef_resource_request_handler_t* get_resource_request_handler(cef_request_context_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_request_t* request, int is_navigation, int is_download, cef_string_t* request_initiator, int* disable_default_handling)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNativeOrNull(browser);
            var m_frame = CefFrame.FromNativeOrNull(frame);
            var m_request = CefRequest.FromNative(request);
            var m_isNavigation = is_navigation != 0;
            var m_isDownload = is_download != 0;
            var m_requestInitiator = cef_string_t.ToString(request_initiator);
            var m_disableDefaultHandling = *disable_default_handling != 0;

            var m_result = GetResourceRequestHandler(m_browser, m_frame, m_request, m_isNavigation, m_isDownload, m_requestInitiator, ref m_disableDefaultHandling);

            *disable_default_handling = m_disableDefaultHandling ? 1 : 0;

            return m_result != null ? m_result.ToNative() : null;
        }

        /// <summary>
        /// Called on the browser process IO thread before a resource request is
        /// initiated. The |browser| and |frame| values represent the source of the
        /// request, and may be NULL for requests originating from service workers or
        /// CefURLRequest. |request| represents the request contents and cannot be
        /// modified in this callback. |is_navigation| will be true if the resource
        /// request is a navigation. |is_download| will be true if the resource request
        /// is a download. |request_initiator| is the origin (scheme + domain) of the
        /// page that initiated the request. Set |disable_default_handling| to true to
        /// disable default handling of the request, in which case it will need to be
        /// handled via CefResourceRequestHandler::GetResourceHandler or it will be
        /// canceled. To allow the resource load to proceed with default handling
        /// return NULL. To specify a handler for the resource return a
        /// CefResourceRequestHandler object. This method will not be called if the
        /// client associated with |browser| returns a non-NULL value from
        /// CefRequestHandler::GetResourceRequestHandler for the same request
        /// (identified by CefRequest::GetIdentifier).
        /// </summary>
        protected abstract CefResourceRequestHandler GetResourceRequestHandler(CefBrowser browser, CefFrame frame, CefRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling);
    }
}
