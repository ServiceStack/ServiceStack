namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to handle events related to find results. The
    /// methods of this class will be called on the UI thread.
    /// </summary>
    public abstract unsafe partial class CefFindHandler
    {
        private void on_find_result(cef_find_handler_t* self, cef_browser_t* browser, int identifier, int count, cef_rect_t* selectionRect, int activeMatchOrdinal, int finalUpdate)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            var mSelectionRect = new CefRectangle(selectionRect->x, selectionRect->y, selectionRect->width, selectionRect->height);

            OnFindResult(mBrowser, identifier, count, mSelectionRect, activeMatchOrdinal, finalUpdate != 0);
        }

        /// <summary>
        /// Called to report find results returned by CefBrowserHost::Find().
        /// |identifer| is the identifier passed to Find(), |count| is the number of
        /// matches currently identified, |selectionRect| is the location of where the
        /// match was found (in window coordinates), |activeMatchOrdinal| is the
        /// current position in the search results, and |finalUpdate| is true if this
        /// is the last find notification.
        /// </summary>
        protected abstract void OnFindResult(CefBrowser browser, int identifier, int count, CefRectangle selectionRect, int activeMatchOrdinal, bool finalUpdate);
    }
}
