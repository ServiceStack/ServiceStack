namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to handle events related to browser load status. The
    /// methods of this class will be called on the browser process UI thread or
    /// render process main thread (TID_RENDERER).
    /// </summary>
    public abstract unsafe partial class CefLoadHandler
    {
        private void on_loading_state_change(cef_load_handler_t* self, cef_browser_t* browser, int isLoading, int canGoBack, int canGoForward)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);

            OnLoadingStateChange(mBrowser, isLoading != 0, canGoBack != 0, canGoForward != 0);
        }

        /// <summary>
        /// Called when the loading state has changed. This callback will be executed
        /// twice -- once when loading is initiated either programmatically or by user
        /// action, and once when loading is terminated due to completion, cancellation
        /// of failure. It will be called before any calls to OnLoadStart and after all
        /// calls to OnLoadError and/or OnLoadEnd.
        /// </summary>
        protected virtual void OnLoadingStateChange(CefBrowser browser, bool isLoading, bool canGoBack, bool canGoForward)
        {
        }


        private void on_load_start(cef_load_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, CefTransitionType transition_type)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_frame = CefFrame.FromNative(frame);

            OnLoadStart(m_browser, m_frame, transition_type);
        }

        /// <summary>
        /// Called after a navigation has been committed and before the browser begins
        /// loading contents in the frame. The |frame| value will never be empty --
        /// call the IsMain() method to check if this frame is the main frame.
        /// |transition_type| provides information about the source of the navigation
        /// and an accurate value is only available in the browser process. Multiple
        /// frames may be loading at the same time. Sub-frames may start or continue
        /// loading after the main frame load has ended. This method will not be called
        /// for same page navigations (fragments, history state, etc.) or for
        /// navigations that fail or are canceled before commit. For notification of
        /// overall browser load status use OnLoadingStateChange instead.
        /// </summary>
        protected virtual void OnLoadStart(CefBrowser browser, CefFrame frame, CefTransitionType transitionType)
        {
        }


        private void on_load_end(cef_load_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, int httpStatusCode)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_frame = CefFrame.FromNative(frame);

            OnLoadEnd(m_browser, m_frame, httpStatusCode);
        }

        /// <summary>
        /// Called when the browser is done loading a frame. The |frame| value will
        /// never be empty -- call the IsMain() method to check if this frame is the
        /// main frame. Multiple frames may be loading at the same time. Sub-frames may
        /// start or continue loading after the main frame load has ended. This method
        /// will not be called for same page navigations (fragments, history state,
        /// etc.) or for navigations that fail or are canceled before commit. For
        /// notification of overall browser load status use OnLoadingStateChange
        /// instead.
        /// </summary>
        protected virtual void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
        {
        }


        private void on_load_error(cef_load_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, CefErrorCode errorCode, cef_string_t* errorText, cef_string_t* failedUrl)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_frame = CefFrame.FromNative(frame);
            var m_errorText = cef_string_t.ToString(errorText);
            var m_failedUrl = cef_string_t.ToString(failedUrl);

            OnLoadError(m_browser, m_frame, errorCode, m_errorText, m_failedUrl);
        }

        /// <summary>
        /// Called when a navigation fails or is canceled. This method may be called
        /// by itself if before commit or in combination with OnLoadStart/OnLoadEnd if
        /// after commit. |errorCode| is the error code number, |errorText| is the
        /// error text and |failedUrl| is the URL that failed to load.
        /// See net\base\net_error_list.h for complete descriptions of the error codes.
        /// </summary>
        protected virtual void OnLoadError(CefBrowser browser, CefFrame frame, CefErrorCode errorCode, string errorText, string failedUrl)
        {
        }
    }
}
