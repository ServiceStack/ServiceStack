namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to handle dialog events. The methods of this class
    /// will be called on the browser process UI thread.
    /// </summary>
    public abstract unsafe partial class CefDialogHandler
    {
        private int on_file_dialog(cef_dialog_handler_t* self, cef_browser_t* browser, CefFileDialogMode mode, cef_string_t* title, cef_string_t* default_file_path, cef_string_list* accept_filters, int selected_accept_filter, cef_file_dialog_callback_t* callback)
        {
            CheckSelf(self);

            var mBrowser = CefBrowser.FromNative(browser);
            var mTitle = cef_string_t.ToString(title);
            var mDefaultFilePath = cef_string_t.ToString(default_file_path);
            var mAcceptFilters = cef_string_list.ToArray(accept_filters);
            var mCallback = CefFileDialogCallback.FromNative(callback);

            var result = OnFileDialog(mBrowser, mode, mTitle, mDefaultFilePath, mAcceptFilters, selected_accept_filter, mCallback);

            return result ? 1 : 0;
        }

        /// <summary>
        /// Called to run a file chooser dialog. |mode| represents the type of dialog
        /// to display. |title| to the title to be used for the dialog and may be empty
        /// to show the default title ("Open" or "Save" depending on the mode).
        /// |default_file_path| is the path with optional directory and/or file name
        /// component that should be initially selected in the dialog. |accept_filters|
        /// are used to restrict the selectable file types and may any combination of
        /// (a) valid lower-cased MIME types (e.g. "text/*" or "image/*"),
        /// (b) individual file extensions (e.g. ".txt" or ".png"), or (c) combined
        /// description and file extension delimited using "|" and ";" (e.g.
        /// "Image Types|.png;.gif;.jpg"). |selected_accept_filter| is the 0-based
        /// index of the filter that should be selected by default. To display a custom
        /// dialog return true and execute |callback| either inline or at a later time.
        /// To display the default dialog return false.
        /// </summary>
        protected virtual bool OnFileDialog(CefBrowser browser, CefFileDialogMode mode, string title, string defaultFilePath, string[] acceptFilters, int selectedAcceptFilter, CefFileDialogCallback callback)
        {
            return false;
        }
    }
}
