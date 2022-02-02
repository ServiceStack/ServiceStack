namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to handle file downloads. The methods of this class will called
    /// on the browser process UI thread.
    /// </summary>
    public abstract unsafe partial class CefDownloadHandler
    {
        private void on_before_download(cef_download_handler_t* self, cef_browser_t* browser, cef_download_item_t* download_item, cef_string_t* suggested_name, cef_before_download_callback_t* callback)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_download_item = CefDownloadItem.FromNative(download_item);
            var m_suggested_name = cef_string_t.ToString(suggested_name);
            var m_callback = CefBeforeDownloadCallback.FromNative(callback);

            OnBeforeDownload(m_browser, m_download_item, m_suggested_name, m_callback);

            m_download_item.Dispose();
        }

        /// <summary>
        /// Called before a download begins. |suggested_name| is the suggested name for
        /// the download file. By default the download will be canceled. Execute
        /// |callback| either asynchronously or in this method to continue the download
        /// if desired. Do not keep a reference to |download_item| outside of this
        /// method.
        /// </summary>
        protected virtual void OnBeforeDownload(CefBrowser browser, CefDownloadItem downloadItem, string suggestedName, CefBeforeDownloadCallback callback)
        {
        }


        private void on_download_updated(cef_download_handler_t* self, cef_browser_t* browser, cef_download_item_t* download_item, cef_download_item_callback_t* callback)
        {
            CheckSelf(self);

            var m_browser = CefBrowser.FromNative(browser);
            var m_download_item = CefDownloadItem.FromNative(download_item);
            var m_callback = CefDownloadItemCallback.FromNative(callback);

            OnDownloadUpdated(m_browser, m_download_item, m_callback);

            m_download_item.Dispose();
        }

        /// <summary>
        /// Called when a download's status or progress information has been updated.
        /// This may be called multiple times before and after OnBeforeDownload().
        /// Execute |callback| either asynchronously or in this method to cancel the
        /// download if desired. Do not keep a reference to |download_item| outside of
        /// this method.
        /// </summary>
        protected virtual void OnDownloadUpdated(CefBrowser browser, CefDownloadItem downloadItem, CefDownloadItemCallback callback)
        {
        }
    }
}
