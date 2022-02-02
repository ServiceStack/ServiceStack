//
// This file manually written from cef/include/internal/cef_types.h.
//
namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_request_context_settings_t
    {
        public UIntPtr size;
        public cef_string_t cache_path;
        public int persist_session_cookies;
        public int persist_user_preferences;
        public int ignore_certificate_errors;
        public cef_string_t accept_language_list;

        #region Alloc & Free
        private static int _sizeof;

        static cef_request_context_settings_t()
        {
            _sizeof = Marshal.SizeOf(typeof(cef_request_context_settings_t));
        }

        public static cef_request_context_settings_t* Alloc()
        {
            var ptr = (cef_request_context_settings_t*)Marshal.AllocHGlobal(_sizeof);
            *ptr = new cef_request_context_settings_t();
            ptr->size = (UIntPtr)_sizeof;
            return ptr;
        }

        public static void Free(cef_request_context_settings_t* ptr)
        {
            Marshal.FreeHGlobal((IntPtr)ptr);
        }
        #endregion
    }
}
