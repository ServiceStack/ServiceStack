namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used to represent a browser window. When used in the browser process
    /// the methods of this class may be called on any thread unless otherwise
    /// indicated in the comments. When used in the render process the methods of
    /// this class may only be called on the main thread.
    /// </summary>
    public sealed unsafe partial class CefBrowser
    {
        /// <summary>
        /// Returns the browser host object. This method can only be called in the
        /// browser process.
        /// </summary>
        public CefBrowserHost GetHost()
        {
            return CefBrowserHost.FromNative(
                cef_browser_t.get_host(_self)
                );
        }

        /// <summary>
        /// Returns true if the browser can navigate backwards.
        /// </summary>
        public bool CanGoBack
        {
            get { return cef_browser_t.can_go_back(_self) != 0; }
        }

        /// <summary>
        /// Navigate backwards.
        /// </summary>
        public void GoBack()
        {
            cef_browser_t.go_back(_self);
        }

        /// <summary>
        /// Returns true if the browser can navigate forwards.
        /// </summary>
        public bool CanGoForward
        {
            get { return cef_browser_t.can_go_forward(_self) != 0; }
        }

        /// <summary>
        /// Navigate forwards.
        /// </summary>
        public void GoForward()
        {
            cef_browser_t.go_forward(_self);
        }

        /// <summary>
        /// Returns true if the browser is currently loading.
        /// </summary>
        public bool IsLoading
        {
            get { return cef_browser_t.is_loading(_self) != 0; }
        }

        /// <summary>
        /// Reload the current page.
        /// </summary>
        public void Reload()
        {
            cef_browser_t.reload(_self);
        }

        /// <summary>
        /// Reload the current page ignoring any cached data.
        /// </summary>
        public void ReloadIgnoreCache()
        {
            cef_browser_t.reload_ignore_cache(_self);
        }

        /// <summary>
        /// Stop loading the page.
        /// </summary>
        public void StopLoad()
        {
            cef_browser_t.stop_load(_self);
        }

        /// <summary>
        /// Returns the globally unique identifier for this browser. This value is also
        /// used as the tabId for extension APIs.
        /// </summary>
        public int Identifier
        {
            get { return cef_browser_t.get_identifier(_self); }
        }

        /// <summary>
        /// Returns true if this object is pointing to the same handle as |that|
        /// object.
        /// </summary>
        public bool IsSame(CefBrowser that)
        {
            if (that == null) return false;
            return cef_browser_t.is_same(_self, that.ToNative()) != 0;
        }

        /// <summary>
        /// Returns true if the window is a popup window.
        /// </summary>
        public bool IsPopup
        {
            get { return cef_browser_t.is_popup(_self) != 0; }
        }

        /// <summary>
        /// Returns true if a document has been loaded in the browser.
        /// </summary>
        public bool HasDocument
        {
            get { return cef_browser_t.has_document(_self) != 0; }
        }

        /// <summary>
        /// Returns the main (top-level) frame for the browser window.
        /// </summary>
        public CefFrame GetMainFrame()
        {
            return CefFrame.FromNative(
                cef_browser_t.get_main_frame(_self)
                );
        }

        /// <summary>
        /// Returns the focused frame for the browser window.
        /// </summary>
        public CefFrame GetFocusedFrame()
        {
            return CefFrame.FromNative(
                cef_browser_t.get_focused_frame(_self)
                );
        }

        /// <summary>
        /// Returns the frame with the specified identifier, or NULL if not found.
        /// </summary>
        public CefFrame GetFrame(long identifier)
        {
            return CefFrame.FromNativeOrNull(
                cef_browser_t.get_frame_byident(_self, identifier)
                );
        }

        /// <summary>
        /// Returns the frame with the specified name, or NULL if not found.
        /// </summary>
        public CefFrame GetFrame(string name)
        {
            fixed (char* name_str = name)
            {
                var n_name = new cef_string_t(name_str, name.Length);

                return CefFrame.FromNativeOrNull(
                    cef_browser_t.get_frame(_self, &n_name)
                    );
            }
        }

        /// <summary>
        /// Returns the number of frames that currently exist.
        /// </summary>
        public int FrameCount
        {
            get { return (int)cef_browser_t.get_frame_count(_self); }
        }

        /// <summary>
        /// Returns the identifiers of all existing frames.
        /// </summary>
        public long[] GetFrameIdentifiers()
        {
            var frameCount = FrameCount;
            var identifiers = new long[frameCount * 2];
            UIntPtr n_count = (UIntPtr)frameCount;

            fixed (long* identifiers_ptr = identifiers)
            {
                cef_browser_t.get_frame_identifiers(_self, &n_count, identifiers_ptr);
            }

            if ((int)n_count < 0)
            {
                throw new InvalidOperationException("Invalid number of frames.");
            }

            if ((int)n_count > identifiers.Length)
            {
                throw new InvalidOperationException("Number of returned frames are too big.");
            }

            Array.Resize(ref identifiers, (int)n_count);

            return identifiers;
        }

        /// <summary>
        /// Returns the names of all existing frames.
        /// </summary>
        public string[] GetFrameNames()
        {
            var list = libcef.string_list_alloc();
            cef_browser_t.get_frame_names(_self, list);
            var result = cef_string_list.ToArray(list);
            libcef.string_list_free(list);
            return result;
        }
    }
}
