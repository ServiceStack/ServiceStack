namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Callback interface used to asynchronously cancel a download.
    /// </summary>
    public sealed unsafe partial class CefDownloadItemCallback
    {
        /// <summary>
        /// Call to cancel the download.
        /// </summary>
        public void Cancel()
        {
            cef_download_item_callback_t.cancel(_self);
        }

        /// <summary>
        /// Call to pause the download.
        /// </summary>
        public void Pause()
        {
            cef_download_item_callback_t.pause(_self);
        }

        /// <summary>
        /// Call to resume the download.
        /// </summary>
        public void Resume()
        {
            cef_download_item_callback_t.resume(_self);
        }
    }
}
