namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to represent drag data. The methods of this class may be called
    /// on any thread.
    /// </summary>
    public sealed unsafe partial class CefDragData
    {
        /// <summary>
        /// Create a new CefDragData object.
        /// </summary>
        public static CefDragData Create()
        {
            return CefDragData.FromNative(cef_drag_data_t.create());
        }

        /// <summary>
        /// Returns a copy of the current object.
        /// </summary>
        public CefDragData Clone()
        {
            return CefDragData.FromNative(cef_drag_data_t.clone(_self));
        }

        /// <summary>
        /// Returns true if this object is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return cef_drag_data_t.is_read_only(_self) != 0; }
        }


        /// <summary>
        /// Returns true if the drag data is a link.
        /// </summary>
        public bool IsLink
        {
            get { return cef_drag_data_t.is_link(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the drag data is a text or html fragment.
        /// </summary>
        public bool IsFragment
        {
            get { return cef_drag_data_t.is_fragment(_self) != 0; }
        }

        /// <summary>
        /// Returns true if the drag data is a file.
        /// </summary>
        public bool IsFile
        {
            get { return cef_drag_data_t.is_file(_self) != 0; }
        }

        /// <summary>
        /// Return the link URL that is being dragged.
        /// </summary>
        public string LinkUrl
        {
            get
            {
                var n_result = cef_drag_data_t.get_link_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Return the title associated with the link being dragged.
        /// </summary>
        public string LinkTitle
        {
            get
            {
                var n_result = cef_drag_data_t.get_link_title(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Return the metadata, if any, associated with the link being dragged.
        /// </summary>
        public string LinkMetadata
        {
            get
            {
                var n_result = cef_drag_data_t.get_link_metadata(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Return the plain text fragment that is being dragged.
        /// </summary>
        public string FragmentText
        {
            get
            {
                var n_result = cef_drag_data_t.get_fragment_text(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Return the text/html fragment that is being dragged.
        /// </summary>
        public string FragmentHtml
        {
            get
            {
                var n_result = cef_drag_data_t.get_fragment_html(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Return the base URL that the fragment came from. This value is used for
        /// resolving relative URLs and may be empty.
        /// </summary>
        public string FragmentBaseUrl
        {
            get
            {
                var n_result = cef_drag_data_t.get_fragment_base_url(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Return the name of the file being dragged out of the browser window.
        /// </summary>
        public string FileName
        {
            get
            {
                var n_result = cef_drag_data_t.get_file_name(_self);
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Write the contents of the file being dragged out of the web view into
        /// |writer|. Returns the number of bytes sent to |writer|. If |writer| is
        /// NULL this method will return the size of the file contents in bytes.
        /// Call GetFileName() to get a suggested name for the file.
        /// </summary>
        public ulong GetFileContents(CefStreamWriter writer)
        {
            var n_writer = writer != null ? writer.ToNative() : null;
            return (ulong)cef_drag_data_t.get_file_contents(_self, n_writer);
        }

        /// <summary>
        /// Retrieve the list of file names that are being dragged into the browser
        /// window.
        /// </summary>
        public string[] GetFileNames()
        {
            cef_string_list* n_result = null;
            try
            {
                n_result = libcef.string_list_alloc();
                var success = cef_drag_data_t.get_file_names(_self, n_result) != 0;
                if (!success) return null;
                return cef_string_list.ToArray(n_result);
            }
            finally
            {
                if (n_result != null) libcef.string_list_free(n_result);
            }
        }

        /// <summary>
        /// Set the link URL that is being dragged.
        /// </summary>
        public void SetLinkURL(string url)
        {
            fixed (char* url_str = url)
            {
                var n_url = new cef_string_t(url_str, url != null ? url.Length : 0);
                cef_drag_data_t.set_link_url(_self, &n_url);
            }
        }

        /// <summary>
        /// Set the title associated with the link being dragged.
        /// </summary>
        public void SetLinkTitle(string title)
        {
            fixed (char* title_str = title)
            {
                var n_title = new cef_string_t(title_str, title != null ? title.Length : 0);
                cef_drag_data_t.set_link_title(_self, &n_title);
            }
        }

        /// <summary>
        /// Set the metadata associated with the link being dragged.
        /// </summary>
        public void SetLinkMetadata(string data)
        {
            fixed (char* data_str = data)
            {
                var n_data = new cef_string_t(data_str, data != null ? data.Length : 0);
                cef_drag_data_t.set_link_metadata(_self, &n_data);
            }
        }

        /// <summary>
        /// Set the plain text fragment that is being dragged.
        /// </summary>
        public void SetFragmentText(string text)
        {
            fixed (char* text_str = text)
            {
                var n_text = new cef_string_t(text_str, text != null ? text.Length : 0);
                cef_drag_data_t.set_fragment_text(_self, &n_text);
            }
        }

        /// <summary>
        /// Set the text/html fragment that is being dragged.
        /// </summary>
        public void SetFragmentHtml(string html)
        {
            fixed (char* html_str = html)
            {
                var n_html = new cef_string_t(html_str, html != null ? html.Length : 0);
                cef_drag_data_t.set_fragment_html(_self, &n_html);
            }
        }

        /// <summary>
        /// Set the base URL that the fragment came from.
        /// </summary>
        public void SetFragmentBaseURL(string baseUrl)
        {
            fixed (char* baseUrl_str = baseUrl)
            {
                var n_baseUrl = new cef_string_t(baseUrl_str, baseUrl != null ? baseUrl.Length : 0);
                cef_drag_data_t.set_fragment_base_url(_self, &n_baseUrl);
            }
        }

        /// <summary>
        /// Reset the file contents. You should do this before calling
        /// CefBrowserHost::DragTargetDragEnter as the web view does not allow us to
        /// drag in this kind of data.
        /// </summary>
        public void ResetFileContents()
        {
            cef_drag_data_t.reset_file_contents(_self);
        }

        /// <summary>
        /// Add a file that is being dragged into the webview.
        /// </summary>
        public void AddFile(string path, string displayName)
        {
            fixed (char* path_str = path)
            fixed (char* displayName_str = displayName)
            {
                var n_path = new cef_string_t(path_str, path != null ? path.Length : 0);
                var n_displayName = new cef_string_t(displayName_str, displayName != null ? displayName.Length : 0);

                cef_drag_data_t.add_file(_self, &n_path, &n_displayName);
            }
        }

        /// <summary>
        /// Get the image representation of drag data. May return NULL if no image
        /// representation is available.
        /// </summary>
        public CefImage GetImage()
        {
            var result = cef_drag_data_t.get_image(_self);
            return CefImage.FromNativeOrNull(result);
        }

        /// <summary>
        /// Get the image hotspot (drag start location relative to image dimensions).
        /// </summary>
        public CefPoint GetImageHotspot()
        {
            var result = cef_drag_data_t.get_image_hotspot(_self);
            return new CefPoint(result.x, result.y);
        }

        /// <summary>
        /// Returns true if an image representation of drag data is available.
        /// </summary>
        public bool HasImage
        {
            get
            {
                return cef_drag_data_t.has_image(_self) != 0;
            }
        }
    }
}
