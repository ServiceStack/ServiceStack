namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;
    
    /// <summary>
    /// Callback interface used for asynchronous continuation of
    /// CefExtensionHandler::GetExtensionResource.
    /// </summary>
    public sealed unsafe partial class CefGetExtensionResourceCallback
    {
        /// <summary>
        /// Continue the request. Read the resource contents from |stream|.
        /// </summary>
        public void Continue(CefStreamReader stream)
        {
            var n_stream = stream.ToNative();
            cef_get_extension_resource_callback_t.cont(_self, n_stream);
        }
        
        /// <summary>
        /// Cancel the request.
        /// </summary>
        public void Cancel()
        {
            cef_get_extension_resource_callback_t.cancel(_self);
        }
        
    }
}
