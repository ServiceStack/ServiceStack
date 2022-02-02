//
// This file manually written from cef/include/internal/cef_types.h.
//
namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_cookie_t
    {
        public cef_string_t name;
        public cef_string_t value;
        public cef_string_t domain;
        public cef_string_t path;
        public int secure;
        public int httponly;
        public cef_time_t creation;
        public cef_time_t last_access;
        public int has_expires;
        public cef_time_t expires;
        public CefCookieSameSite same_site;
        public CefCookiePriority priority;

        internal static void Clear(cef_cookie_t* ptr)
        {
            libcef.string_clear(&ptr->name);
            libcef.string_clear(&ptr->value);
            libcef.string_clear(&ptr->domain);
            libcef.string_clear(&ptr->path);
        }

        #region Alloc & Free
        private static int _sizeof;

        static cef_cookie_t()
        {
            _sizeof = Marshal.SizeOf(typeof(cef_cookie_t));
        }

        public static cef_cookie_t* Alloc()
        {
            var ptr = (cef_cookie_t*)Marshal.AllocHGlobal(_sizeof);
            *ptr = new cef_cookie_t();
            return ptr;
        }

        public static void Free(cef_cookie_t* ptr)
        {
            Marshal.FreeHGlobal((IntPtr)ptr);
        }
        #endregion
    }
}
