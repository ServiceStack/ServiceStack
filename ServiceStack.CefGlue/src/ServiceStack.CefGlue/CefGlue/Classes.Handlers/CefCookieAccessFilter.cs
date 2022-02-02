namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;
    
    /// <summary>
    /// Implement this interface to filter cookies that may be sent or received from
    /// resource requests. The methods of this class will be called on the IO thread
    /// unless otherwise indicated.
    /// </summary>
    public abstract unsafe partial class CefCookieAccessFilter
    {
        private int can_send_cookie(cef_cookie_access_filter_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_request_t* request, cef_cookie_t* cookie)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNativeOrNull(browser);
            var m_frame = CefFrame.FromNativeOrNull(frame);
            var m_request = CefRequest.FromNative(request);
            var m_cookie = CefCookie.FromNative(cookie);

            var m_result = CanSendCookie(m_browser, m_frame, m_request, m_cookie);

            return m_result ? 1 : 0;
        }

        /// <summary>
        /// Called on the IO thread before a resource request is sent. The |browser|
        /// and |frame| values represent the source of the request, and may be NULL for
        /// requests originating from service workers or CefURLRequest. |request|
        /// cannot be modified in this callback. Return true if the specified cookie
        /// can be sent with the request or false otherwise.
        /// </summary>
        protected abstract bool CanSendCookie(CefBrowser browser, CefFrame frame, CefRequest request, CefCookie cookie);


        private int can_save_cookie(cef_cookie_access_filter_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_request_t* request, cef_response_t* response, cef_cookie_t* cookie)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNativeOrNull(browser);
            var m_frame = CefFrame.FromNativeOrNull(frame);
            var m_request = CefRequest.FromNative(request);
            var m_response = CefResponse.FromNative(response);
            var m_cookie = CefCookie.FromNative(cookie);

            var m_result = CanSaveCookie(m_browser, m_frame, m_request, m_response, m_cookie);

            return m_result ? 1 : 0;
        }
        
        /// <summary>
        /// Called on the IO thread after a resource response is received. The
        /// |browser| and |frame| values represent the source of the request, and may
        /// be NULL for requests originating from service workers or CefURLRequest.
        /// |request| cannot be modified in this callback. Return true if the specified
        /// cookie returned with the response can be saved or false otherwise.
        /// </summary>
        protected abstract bool CanSaveCookie(CefBrowser browser, CefFrame frame, CefRequest request, CefResponse response, CefCookie cookie);
    }
}
