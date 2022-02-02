namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Callback interface for CefRequestContext::ResolveHost.
    /// </summary>
    public abstract unsafe partial class CefResolveCallback
    {
        private void on_resolve_completed(cef_resolve_callback_t* self, CefErrorCode result, cef_string_list* resolved_ips)
        {
            CheckSelf(self);

            var mResolvedIps = cef_string_list.ToArray(resolved_ips);
            OnResolveCompleted(result, mResolvedIps);
        }

        /// <summary>
        /// Called on the UI thread after the ResolveHost request has completed.
        /// |result| will be the result code. |resolved_ips| will be the list of
        /// resolved IP addresses or empty if the resolution failed.
        /// </summary>
        protected abstract void OnResolveCompleted(CefErrorCode result, string[] resolvedIps);
    }
}
