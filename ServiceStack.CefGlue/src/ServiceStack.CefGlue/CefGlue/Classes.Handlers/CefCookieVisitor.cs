namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Interface to implement for visiting cookie values. The methods of this class
    /// will always be called on the UI thread.
    /// </summary>
    public abstract unsafe partial class CefCookieVisitor
    {
        private int visit(cef_cookie_visitor_t* self, cef_cookie_t* cookie, int count, int total, int* deleteCookie)
        {
            CheckSelf(self);

            var mCookie = CefCookie.FromNative(cookie);
            bool mDelete;

            var result = Visit(mCookie, count, total, out mDelete);

            *deleteCookie = mDelete ? 1 : 0;
            return result ? 1 : 0;
        }

        /// <summary>
        /// Method that will be called once for each cookie. |count| is the 0-based
        /// index for the current cookie. |total| is the total number of cookies.
        /// Set |deleteCookie| to true to delete the cookie currently being visited.
        /// Return false to stop visiting cookies. This method may never be called if
        /// no cookies are found.
        /// </summary>
        protected abstract bool Visit(CefCookie cookie, int count, int total, out bool delete);
    }
}
