namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Callback interface used for asynchronous continuation of url requests.
    /// </summary>
    public sealed unsafe partial class CefRequestCallback
    {
        /// <summary>
        /// Continue the url request. If |allow| is true the request will be continued.
        /// Otherwise, the request will be canceled.
        /// </summary>
        public void Continue(bool allow)
        {
            cef_request_callback_t.cont(_self, allow ? 1 : 0);
        }

        /// <summary>
        /// Cancel the url request.
        /// </summary>
        public void Cancel()
        {
            cef_request_callback_t.cancel(_self);
        }
    }
}
