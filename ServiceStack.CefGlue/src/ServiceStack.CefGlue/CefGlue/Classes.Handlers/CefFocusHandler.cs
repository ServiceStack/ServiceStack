namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to handle events related to focus. The methods of
    /// this class will be called on the UI thread.
    /// </summary>
    public abstract unsafe partial class CefFocusHandler
    {
        private void on_take_focus(cef_focus_handler_t* self, cef_browser_t* browser, int next)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            OnTakeFocus(m_browser, next != 0);
        }

        /// <summary>
        /// Called when the browser component is about to loose focus. For instance, if
        /// focus was on the last HTML element and the user pressed the TAB key. |next|
        /// will be true if the browser is giving focus to the next component and false
        /// if the browser is giving focus to the previous component.
        /// </summary>
        protected virtual void OnTakeFocus(CefBrowser browser, bool next)
        {
        }


        private int on_set_focus(cef_focus_handler_t* self, cef_browser_t* browser, CefFocusSource source)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            return OnSetFocus(m_browser, source) ? 1 : 0;
        }

        /// <summary>
        /// Called when the browser component is requesting focus. |source| indicates
        /// where the focus request is originating from. Return false to allow the
        /// focus to be set or true to cancel setting the focus.
        /// </summary>
        protected virtual bool OnSetFocus(CefBrowser browser, CefFocusSource source)
        {
            return false;
        }


        private void on_got_focus(cef_focus_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);

            OnGotFocus(m_browser);
        }

        /// <summary>
        /// Called when the browser component has received focus.
        /// </summary>
        protected virtual void OnGotFocus(CefBrowser browser)
        {
        }
    }
}
