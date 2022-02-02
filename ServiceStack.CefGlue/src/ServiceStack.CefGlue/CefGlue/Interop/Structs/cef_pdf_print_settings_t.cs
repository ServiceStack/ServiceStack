//
// This file manually written from cef/include/internal/cef_types.h.
//
namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_pdf_print_settings_t
    {
        public cef_string_t header_footer_title;
        public cef_string_t header_footer_url;
        public int page_width;
        public int page_height;
        public int scale_factor;
        public int margin_top;
        public int margin_right;
        public int margin_bottom;
        public int margin_left;
        public CefPdfPrintMarginType margin_type;
        public int header_footer_enabled;
        public int selection_only;
        public int landscape;
        public int backgrounds_enabled;

        internal static void Clear(cef_pdf_print_settings_t* ptr)
        {
            libcef.string_clear(&ptr->header_footer_title);
            libcef.string_clear(&ptr->header_footer_url);
        }

        #region Alloc & Free
        private static int _sizeof;

        static cef_pdf_print_settings_t()
        {
            _sizeof = Marshal.SizeOf(typeof(cef_pdf_print_settings_t));
        }

        public static cef_pdf_print_settings_t* Alloc()
        {
            var ptr = (cef_pdf_print_settings_t*)Marshal.AllocHGlobal(_sizeof);
            *ptr = new cef_pdf_print_settings_t();
            return ptr;
        }

        public static void Free(cef_pdf_print_settings_t* ptr)
        {
            Marshal.FreeHGlobal((IntPtr)ptr);
        }
        #endregion
    }
}
