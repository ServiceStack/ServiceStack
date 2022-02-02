namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to handle printing on Linux. Each browser will have
    /// only one print job in progress at a time. The methods of this class will be
    /// called on the browser process UI thread.
    /// </summary>
    public abstract unsafe partial class CefPrintHandler
    {
        private void on_print_start(cef_print_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            OnPrintStart(mBrowser);
        }

        /// <summary>
        /// Called when printing has started for the specified |browser|. This method
        /// will be called before the other OnPrint*() methods and irrespective of how
        /// printing was initiated (e.g. CefBrowserHost::Print(), JavaScript
        /// window.print() or PDF extension print button).
        /// </summary>
        protected virtual void OnPrintStart(CefBrowser browser)
        {
        }


        private void on_print_settings(cef_print_handler_t* self, cef_browser_t* browser, cef_print_settings_t* settings, int get_defaults)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            var m_settings = CefPrintSettings.FromNative(settings);
            OnPrintSettings(mBrowser, m_settings, get_defaults != 0);
            m_settings.Dispose();
        }

        /// <summary>
        /// Synchronize |settings| with client state. If |get_defaults| is true then
        /// populate |settings| with the default print settings. Do not keep a
        /// reference to |settings| outside of this callback.
        /// </summary>
        protected abstract void OnPrintSettings(CefBrowser browser, CefPrintSettings settings, bool getDefaults);

        private int on_print_dialog(cef_print_handler_t* self, cef_browser_t* browser, int has_selection, cef_print_dialog_callback_t* callback)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            var m_callback = CefPrintDialogCallback.FromNative(callback);
            var m_result = OnPrintDialog(mBrowser, has_selection != 0, m_callback);
            return m_result ? 1 : 0;
        }

        /// <summary>
        /// Show the print dialog. Execute |callback| once the dialog is dismissed.
        /// Return true if the dialog will be displayed or false to cancel the
        /// printing immediately.
        /// </summary>
        protected abstract bool OnPrintDialog(CefBrowser browser, bool hasSelection, CefPrintDialogCallback callback);

        private int on_print_job(cef_print_handler_t* self, cef_browser_t* browser, cef_string_t* document_name, cef_string_t* pdf_file_path, cef_print_job_callback_t* callback)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            var m_documentName = cef_string_t.ToString(document_name);
            var m_pdfFilePath = cef_string_t.ToString(pdf_file_path);
            var m_callback = CefPrintJobCallback.FromNative(callback);

            var m_result = OnPrintJob(mBrowser, m_documentName, m_pdfFilePath, m_callback);

            return m_result ? 1 : 0;
        }

        /// <summary>
        /// Send the print job to the printer. Execute |callback| once the job is
        /// completed. Return true if the job will proceed or false to cancel the job
        /// immediately.
        /// </summary>
        protected abstract bool OnPrintJob(CefBrowser browser, string documentName, string pdfFilePath, CefPrintJobCallback callback);


        private void on_print_reset(cef_print_handler_t* self, cef_browser_t* browser)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            OnPrintReset(mBrowser);
        }

        /// <summary>
        /// Reset client state related to printing.
        /// </summary>
        protected abstract void OnPrintReset(CefBrowser browser);


        private cef_size_t get_pdf_paper_size(cef_print_handler_t* self, int device_units_per_inch)
        {
            CheckSelf(self);

            var m_result = GetPdfPaperSize(device_units_per_inch);

            var n_result = new cef_size_t
            {
                width = m_result.Width,
                height = m_result.Height,
            };

            return n_result;
        }

        /// <summary>
        /// Return the PDF paper size in device units. Used in combination with
        /// CefBrowserHost::PrintToPDF().
        /// </summary>
        protected abstract CefSize GetPdfPaperSize(int deviceUnitsPerInch);
    }
}
