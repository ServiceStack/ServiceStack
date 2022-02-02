namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;
    
    /// <summary>
    /// Callback interface for CefBrowserHost::PrintToPDF. The methods of this class
    /// will be called on the browser process UI thread.
    /// </summary>
    public abstract unsafe partial class CefPdfPrintCallback
    {
        private void on_pdf_print_finished(cef_pdf_print_callback_t* self, cef_string_t* path, int ok)
        {
            CheckSelf(self);

            var m_path = cef_string_t.ToString(path);
            OnPdfPrintFinished(m_path, ok != 0);
        }
        
        /// <summary>
        /// Method that will be executed when the PDF printing has completed. |path|
        /// is the output path. |ok| will be true if the printing completed
        /// successfully or false otherwise.
        /// </summary>
        protected abstract void OnPdfPrintFinished(string path, bool ok);
    }
}
