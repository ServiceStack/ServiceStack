//
// This file manually written from cef/include/internal/cef_types.h.
//
namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_browser_settings_t
    {
        public UIntPtr size;

        public int windowless_frame_rate;

        public cef_string_t standard_font_family;
        public cef_string_t fixed_font_family;
        public cef_string_t serif_font_family;
        public cef_string_t sans_serif_font_family;
        public cef_string_t cursive_font_family;
        public cef_string_t fantasy_font_family;
        public int default_font_size;
        public int default_fixed_font_size;
        public int minimum_font_size;
        public int minimum_logical_font_size;

        public cef_string_t default_encoding;

        public CefState remote_fonts;
        public CefState javascript;
        public CefState javascript_close_windows;
        public CefState javascript_access_clipboard;
        public CefState javascript_dom_paste;
        public CefState plugins;
        public CefState universal_access_from_file_urls;
        public CefState file_access_from_file_urls;
        public CefState web_security;
        public CefState image_loading;
        public CefState image_shrink_standalone_to_fit;
        public CefState text_area_resize;
        public CefState tab_to_links;
        public CefState local_storage;
        public CefState databases;
        public CefState application_cache;
        public CefState webgl;

        public uint background_color;

        public cef_string_t accept_language_list;

        #region Alloc & Free
        private static int _sizeof;

        static cef_browser_settings_t()
        {
            _sizeof = Marshal.SizeOf(typeof(cef_browser_settings_t));
        }

        public static cef_browser_settings_t* Alloc()
        {
            var ptr = (cef_browser_settings_t*)Marshal.AllocHGlobal(_sizeof);
            *ptr = new cef_browser_settings_t();
            ptr->size = (UIntPtr)_sizeof;
            return ptr;
        }

        public static void Free(cef_browser_settings_t* ptr)
        {
            Marshal.FreeHGlobal((IntPtr)ptr);
        }
        #endregion
    }
}
