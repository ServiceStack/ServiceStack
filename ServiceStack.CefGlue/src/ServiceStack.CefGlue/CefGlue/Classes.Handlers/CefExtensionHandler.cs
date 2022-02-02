namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;
    
    /// <summary>
    /// Implement this interface to handle events related to browser extensions.
    /// The methods of this class will be called on the UI thread. See
    /// CefRequestContext::LoadExtension for information about extension loading.
    /// </summary>
    public abstract unsafe partial class CefExtensionHandler
    {
        private void on_extension_load_failed(cef_extension_handler_t* self, CefErrorCode result)
        {
            CheckSelf(self);

            OnExtensionLoadFailed(result);
        }

        /// <summary>
        /// Called if the CefRequestContext::LoadExtension request fails. |result| will
        /// be the error code.
        /// </summary>
        protected virtual void OnExtensionLoadFailed(CefErrorCode result) { }


        private void on_extension_loaded(cef_extension_handler_t* self, cef_extension_t* extension)
        {
            CheckSelf(self);

            var mExtension = CefExtension.FromNative(extension);
            OnExtensionLoaded(mExtension);
        }

        /// <summary>
        /// Called if the CefRequestContext::LoadExtension request succeeds.
        /// |extension| is the loaded extension.
        /// </summary>
        protected virtual void OnExtensionLoaded(CefExtension extension) { }


        private void on_extension_unloaded(cef_extension_handler_t* self, cef_extension_t* extension)
        {
            CheckSelf(self);

            var mExtension = CefExtension.FromNative(extension);
            OnExtensionUnloaded(mExtension);
        }

        /// <summary>
        /// Called after the CefExtension::Unload request has completed.
        /// </summary>
        protected virtual void OnExtensionUnloaded(CefExtension extension) { }


        private int on_before_background_browser(cef_extension_handler_t* self, cef_extension_t* extension, cef_string_t* url, cef_client_t** client, cef_browser_settings_t* settings)
        {
            CheckSelf(self);

            var m_extension = CefExtension.FromNative(extension);
            var m_url = cef_string_t.ToString(url);
            var m_client = CefClient.FromNative(*client);
            var m_settings = new CefBrowserSettings(settings);

            var o_client = m_client;
            var cancel = OnBeforeBackgroundBrowser(m_extension, m_url, ref m_client, m_settings);

            if (!cancel && ((object)o_client != m_client && m_client != null))
            {
                *client = m_client.ToNative();
            }

            m_settings.Dispose();

            return cancel ? 1 : 0;
        }

        /// <summary>
        /// Called when an extension needs a browser to host a background script
        /// specified via the "background" manifest key. The browser will have no
        /// visible window and cannot be displayed. |extension| is the extension that
        /// is loading the background script. |url| is an internally generated
        /// reference to an HTML page that will be used to load the background script
        /// via a &lt;script&gt; src attribute. To allow creation of the browser optionally
        /// modify |client| and |settings| and return false. To cancel creation of the
        /// browser (and consequently cancel load of the background script) return
        /// true. Successful creation will be indicated by a call to
        /// CefLifeSpanHandler::OnAfterCreated, and CefBrowserHost::IsBackgroundHost
        /// will return true for the resulting browser. See
        /// https://developer.chrome.com/extensions/event_pages for more information
        /// about extension background script usage.
        /// </summary>
        protected virtual bool OnBeforeBackgroundBrowser(CefExtension extension, string url, ref CefClient client, CefBrowserSettings settings)
        {
            return false;
        }


        private int on_before_browser(cef_extension_handler_t* self, cef_extension_t* extension, cef_browser_t* browser, cef_browser_t* active_browser, int index, cef_string_t* url, int active, cef_window_info_t* windowInfo, cef_client_t** client, cef_browser_settings_t* settings)
        {
            CheckSelf(self);

            var m_extension = CefExtension.FromNative(extension);
            var m_browser = CefBrowser.FromNativeOrNull(browser);
            var m_activeBrowser = CefBrowser.FromNativeOrNull(active_browser);
            var m_url = cef_string_t.ToString(url);
            var m_active = active != 0;
            var m_windowInfo = CefWindowInfo.FromNative(windowInfo);
            var m_client = CefClient.FromNative(*client);
            var m_settings = new CefBrowserSettings(settings);

            var o_client = m_client;
            var cancel = OnBeforeBrowser(m_extension, m_browser, m_activeBrowser, index, m_url, m_active, m_windowInfo, ref m_client, m_settings);

            if (!cancel && ((object)o_client != m_client && m_client != null))
            {
                *client = m_client.ToNative();
            }

            m_windowInfo.Dispose();
            m_settings.Dispose();

            return cancel ? 1 : 0;
        }


        /// <summary>
        /// Called when an extension API (e.g. chrome.tabs.create) requests creation of
        /// a new browser. |extension| and |browser| are the source of the API call.
        /// |active_browser| may optionally be specified via the windowId property or
        /// returned via the GetActiveBrowser() callback and provides the default
        /// |client| and |settings| values for the new browser. |index| is the position
        /// value optionally specified via the index property. |url| is the URL that
        /// will be loaded in the browser. |active| is true if the new browser should
        /// be active when opened.  To allow creation of the browser optionally modify
        /// |windowInfo|, |client| and |settings| and return false. To cancel creation
        /// of the browser return true. Successful creation will be indicated by a call
        /// to CefLifeSpanHandler::OnAfterCreated. Any modifications to |windowInfo|
        /// will be ignored if |active_browser| is wrapped in a CefBrowserView.
        /// </summary>
        protected virtual bool OnBeforeBrowser(CefExtension extension, CefBrowser browser, CefBrowser activeBrowser, int index, string url, bool active, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings)
        {
            return false;
        }


        private cef_browser_t* get_active_browser(cef_extension_handler_t* self, cef_extension_t* extension, cef_browser_t* browser, int include_incognito)
        {
            CheckSelf(self);

            var m_extension = CefExtension.FromNative(extension);
            var m_browser = CefBrowser.FromNative(browser);
            var m_includeIncognito = include_incognito != 0;

            var result = GetActiveBrowser(m_extension, m_browser, m_includeIncognito);

            return result != null ? result.ToNative() : null;
        }
        
        /// <summary>
        /// Called when no tabId is specified to an extension API call that accepts a
        /// tabId parameter (e.g. chrome.tabs.*). |extension| and |browser| are the
        /// source of the API call. Return the browser that will be acted on by the API
        /// call or return NULL to act on |browser|. The returned browser must share
        /// the same CefRequestContext as |browser|. Incognito browsers should not be
        /// considered unless the source extension has incognito access enabled, in
        /// which case |include_incognito| will be true.
        /// </summary>
        protected virtual CefBrowser GetActiveBrowser(CefExtension extension, CefBrowser browser, bool includeIncognito)
        {
            return null;
        }


        private int can_access_browser(cef_extension_handler_t* self, cef_extension_t* extension, cef_browser_t* browser, int include_incognito, cef_browser_t* target_browser)
        {
            CheckSelf(self);

            var m_extension = CefExtension.FromNative(extension);
            var m_browser = CefBrowser.FromNativeOrNull(browser);
            var m_includeIcognito = include_incognito != 0;
            var m_targetBrowser = CefBrowser.FromNativeOrNull(target_browser);

            var result = CanAccessBrowser(m_extension, m_browser, m_includeIcognito, m_targetBrowser);

            return result ? 1 : 0;
        }
        
        /// <summary>
        /// Called when the tabId associated with |target_browser| is specified to an
        /// extension API call that accepts a tabId parameter (e.g. chrome.tabs.*).
        /// |extension| and |browser| are the source of the API call. Return true
        /// to allow access of false to deny access. Access to incognito browsers
        /// should not be allowed unless the source extension has incognito access
        /// enabled, in which case |include_incognito| will be true.
        /// </summary>
        protected virtual bool CanAccessBrowser(CefExtension extension, CefBrowser browser, bool includeIncognito, CefBrowser targetBrowser)
        {
            return true;
        }


        private int get_extension_resource(cef_extension_handler_t* self, cef_extension_t* extension, cef_browser_t* browser, cef_string_t* file, cef_get_extension_resource_callback_t* callback)
        {
            CheckSelf(self);

            var m_extension = CefExtension.FromNative(extension);
            var m_browser = CefBrowser.FromNativeOrNull(browser);
            var m_file = cef_string_t.ToString(file);
            var m_callback = CefGetExtensionResourceCallback.FromNativeOrNull(callback);

            var handled = GetExtensionResource(m_extension, m_browser, m_file, m_callback);

            if (!handled)
            {
                m_callback.Dispose();
            }

            return handled ? 1 : 0;
        }
        
        /// <summary>
        /// Called to retrieve an extension resource that would normally be loaded from
        /// disk (e.g. if a file parameter is specified to chrome.tabs.executeScript).
        /// |extension| and |browser| are the source of the resource request. |file| is
        /// the requested relative file path. To handle the resource request return
        /// true and execute |callback| either synchronously or asynchronously. For the
        /// default behavior which reads the resource from the extension directory on
        /// disk return false. Localization substitutions will not be applied to
        /// resources handled via this method.
        /// </summary>
        protected virtual bool GetExtensionResource(CefExtension extension, CefBrowser browser, string file, CefGetExtensionResourceCallback callback)
        {
            return false;
        }
    }
}
