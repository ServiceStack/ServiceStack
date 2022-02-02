namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to handle events related to browser life span. The
    /// methods of this class will be called on the UI thread unless otherwise
    /// indicated.
    /// </summary>
    public abstract unsafe partial class CefLifeSpanHandler
    {
        private int on_before_popup(cef_life_span_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_string_t* target_url, cef_string_t* target_frame_name, CefWindowOpenDisposition target_disposition, int user_gesture, cef_popup_features_t* popupFeatures, cef_window_info_t* windowInfo, cef_client_t** client, cef_browser_settings_t* settings, cef_dictionary_value_t** extra_info, int* no_javascript_access)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_frame = CefFrame.FromNative(frame);
            var m_targetUrl = cef_string_t.ToString(target_url);
            var m_targetFrameName = cef_string_t.ToString(target_frame_name);
            var m_userGesture = user_gesture != 0;
            var m_popupFeatures = new CefPopupFeatures(popupFeatures);
            var m_windowInfo = CefWindowInfo.FromNative(windowInfo);
            var m_client = CefClient.FromNative(*client);
            var m_settings = new CefBrowserSettings(settings);
            var m_extraInfo = CefDictionaryValue.FromNativeOrNull(*extra_info);
            var m_noJavascriptAccess = (*no_javascript_access) != 0;

            var o_extraInfo = m_extraInfo;
            var o_client = m_client;
            var result = OnBeforePopup(m_browser, m_frame, m_targetUrl, m_targetFrameName, target_disposition, m_userGesture, m_popupFeatures, m_windowInfo, ref m_client, m_settings, ref m_extraInfo, ref m_noJavascriptAccess);

            if ((object)o_client != m_client && m_client != null)
            {
                *client = m_client.ToNative();
            }

            if ((object)o_extraInfo != m_extraInfo)
            {
                *extra_info = m_extraInfo != null ? m_extraInfo.ToNative() : null;
            }
            
            *no_javascript_access = m_noJavascriptAccess ? 1 : 0;

            m_popupFeatures.Dispose();
            m_windowInfo.Dispose();
            m_settings.Dispose();

            return result ? 1 : 0;
        }

        /// <summary>
        /// Called on the UI thread before a new popup browser is created. The
        /// |browser| and |frame| values represent the source of the popup request. The
        /// |target_url| and |target_frame_name| values indicate where the popup
        /// browser should navigate and may be empty if not specified with the request.
        /// The |target_disposition| value indicates where the user intended to open
        /// the popup (e.g. current tab, new tab, etc). The |user_gesture| value will
        /// be true if the popup was opened via explicit user gesture (e.g. clicking a
        /// link) or false if the popup opened automatically (e.g. via the
        /// DomContentLoaded event). The |popupFeatures| structure contains additional
        /// information about the requested popup window. To allow creation of the
        /// popup browser optionally modify |windowInfo|, |client|, |settings| and
        /// |no_javascript_access| and return false. To cancel creation of the popup
        /// browser return true. The |client| and |settings| values will default to the
        /// source browser's values. If the |no_javascript_access| value is set to
        /// false the new browser will not be scriptable and may not be hosted in the
        /// same renderer process as the source browser. Any modifications to
        /// |windowInfo| will be ignored if the parent browser is wrapped in a
        /// CefBrowserView. Popup browser creation will be canceled if the parent
        /// browser is destroyed before the popup browser creation completes (indicated
        /// by a call to OnAfterCreated for the popup browser). The |extra_info|
        /// parameter provides an opportunity to specify extra information specific
        /// to the created popup browser that will be passed to
        /// CefRenderProcessHandler::OnBrowserCreated() in the render process.
        /// </summary>
        protected virtual bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, CefWindowOpenDisposition targetDisposition, bool userGesture, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess)
        {
            return false;
        }


        private void on_after_created(cef_life_span_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            OnAfterCreated(m_browser);
        }

        /// <summary>
        /// Called after a new browser is created. This callback will be the first
        /// notification that references |browser|.
        /// </summary>
        protected virtual void OnAfterCreated(CefBrowser browser)
        {
        }


        private int do_close(cef_life_span_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            return DoClose(m_browser) ? 1 : 0;
        }

        /// <summary>
        /// Called when a browser has recieved a request to close. This may result
        /// directly from a call to CefBrowserHost::*CloseBrowser() or indirectly if
        /// the browser is parented to a top-level window created by CEF and the user
        /// attempts to close that window (by clicking the 'X', for example). The
        /// DoClose() method will be called after the JavaScript 'onunload' event has
        /// been fired.
        /// An application should handle top-level owner window close notifications by
        /// calling CefBrowserHost::TryCloseBrowser() or
        /// CefBrowserHost::CloseBrowser(false) instead of allowing the window to close
        /// immediately (see the examples below). This gives CEF an opportunity to
        /// process the 'onbeforeunload' event and optionally cancel the close before
        /// DoClose() is called.
        /// When windowed rendering is enabled CEF will internally create a window or
        /// view to host the browser. In that case returning false from DoClose() will
        /// send the standard close notification to the browser's top-level owner
        /// window (e.g. WM_CLOSE on Windows, performClose: on OS X, "delete_event" on
        /// Linux or CefWindowDelegate::CanClose() callback from Views). If the
        /// browser's host window/view has already been destroyed (via view hierarchy
        /// tear-down, for example) then DoClose() will not be called for that browser
        /// since is no longer possible to cancel the close.
        /// When windowed rendering is disabled returning false from DoClose() will
        /// cause the browser object to be destroyed immediately.
        /// If the browser's top-level owner window requires a non-standard close
        /// notification then send that notification from DoClose() and return true.
        /// The CefLifeSpanHandler::OnBeforeClose() method will be called after
        /// DoClose() (if DoClose() is called) and immediately before the browser
        /// object is destroyed. The application should only exit after OnBeforeClose()
        /// has been called for all existing browsers.
        /// The below examples describe what should happen during window close when the
        /// browser is parented to an application-provided top-level window.
        /// Example 1: Using CefBrowserHost::TryCloseBrowser(). This is recommended for
        /// clients using standard close handling and windows created on the browser
        /// process UI thread.
        /// 1.  User clicks the window close button which sends a close notification to
        /// the application's top-level window.
        /// 2.  Application's top-level window receives the close notification and
        /// calls TryCloseBrowser() (which internally calls CloseBrowser(false)).
        /// TryCloseBrowser() returns false so the client cancels the window close.
        /// 3.  JavaScript 'onbeforeunload' handler executes and shows the close
        /// confirmation dialog (which can be overridden via
        /// CefJSDialogHandler::OnBeforeUnloadDialog()).
        /// 4.  User approves the close.
        /// 5.  JavaScript 'onunload' handler executes.
        /// 6.  CEF sends a close notification to the application's top-level window
        /// (because DoClose() returned false by default).
        /// 7.  Application's top-level window receives the close notification and
        /// calls TryCloseBrowser(). TryCloseBrowser() returns true so the client
        /// allows the window close.
        /// 8.  Application's top-level window is destroyed.
        /// 9.  Application's OnBeforeClose() handler is called and the browser object
        /// is destroyed.
        /// 10. Application exits by calling CefQuitMessageLoop() if no other browsers
        /// exist.
        /// Example 2: Using CefBrowserHost::CloseBrowser(false) and implementing the
        /// DoClose() callback. This is recommended for clients using non-standard
        /// close handling or windows that were not created on the browser process UI
        /// thread.
        /// 1.  User clicks the window close button which sends a close notification to
        /// the application's top-level window.
        /// 2.  Application's top-level window receives the close notification and:
        /// A. Calls CefBrowserHost::CloseBrowser(false).
        /// B. Cancels the window close.
        /// 3.  JavaScript 'onbeforeunload' handler executes and shows the close
        /// confirmation dialog (which can be overridden via
        /// CefJSDialogHandler::OnBeforeUnloadDialog()).
        /// 4.  User approves the close.
        /// 5.  JavaScript 'onunload' handler executes.
        /// 6.  Application's DoClose() handler is called. Application will:
        /// A. Set a flag to indicate that the next close attempt will be allowed.
        /// B. Return false.
        /// 7.  CEF sends an close notification to the application's top-level window.
        /// 8.  Application's top-level window receives the close notification and
        /// allows the window to close based on the flag from #6B.
        /// 9.  Application's top-level window is destroyed.
        /// 10. Application's OnBeforeClose() handler is called and the browser object
        /// is destroyed.
        /// 11. Application exits by calling CefQuitMessageLoop() if no other browsers
        /// exist.
        /// </summary>
        protected virtual bool DoClose(CefBrowser browser)
        {
            return false;
        }


        private void on_before_close(cef_life_span_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            OnBeforeClose(m_browser);
        }

        /// <summary>
        /// Called just before a browser is destroyed. Release all references to the
        /// browser object and do not attempt to execute any methods on the browser
        /// object (other than GetIdentifier or IsSame) after this callback returns.
        /// This callback will be the last notification that references |browser| on
        /// the UI thread. Any in-progress network requests associated with |browser|
        /// will be aborted when the browser is destroyed, and
        /// CefResourceRequestHandler callbacks related to those requests may still
        /// arrive on the IO thread after this method is called. See DoClose()
        /// documentation for additional usage information.
        /// </summary>
        protected virtual void OnBeforeClose(CefBrowser browser)
        {
        }
    }
}
