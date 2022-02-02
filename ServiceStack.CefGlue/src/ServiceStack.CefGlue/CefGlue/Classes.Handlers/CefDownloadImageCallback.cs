namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Callback interface for CefBrowserHost::DownloadImage. The methods of this
    /// class will be called on the browser process UI thread.
    /// </summary>
    public abstract unsafe partial class CefDownloadImageCallback
    {
        private void on_download_image_finished(cef_download_image_callback_t* self, cef_string_t* image_url, int http_status_code, cef_image_t* image)
        {
            CheckSelf(self);

            var m_imageUrl = cef_string_t.ToString(image_url);
            var m_image = CefImage.FromNativeOrNull(image);
            OnDownloadImageFinished(m_imageUrl, http_status_code, m_image);
        }

        /// <summary>
        /// Method that will be executed when the image download has completed.
        /// |image_url| is the URL that was downloaded and |http_status_code| is the
        /// resulting HTTP status code. |image| is the resulting image, possibly at
        /// multiple scale factors, or empty if the download failed.
        /// </summary>
        protected abstract void OnDownloadImageFinished(string imageUrl, int httpStatusCode, CefImage image);
    }
}
