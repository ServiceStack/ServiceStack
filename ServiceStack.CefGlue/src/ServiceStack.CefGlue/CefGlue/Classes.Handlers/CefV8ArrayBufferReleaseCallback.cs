namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;
    
    /// <summary>
    /// Callback interface that is passed to CefV8Value::CreateArrayBuffer.
    /// </summary>
    public abstract unsafe partial class CefV8ArrayBufferReleaseCallback
    {
        private void release_buffer(cef_v8array_buffer_release_callback_t* self, void* buffer)
        {
            CheckSelf(self);

            ReleaseBuffer((IntPtr)buffer);
        }
        
        /// <summary>
        /// Called to release |buffer| when the ArrayBuffer JS object is garbage
        /// collected. |buffer| is the value that was passed to CreateArrayBuffer along
        /// with this object.
        /// </summary>
        protected abstract void ReleaseBuffer(IntPtr buffer);
    }
}
