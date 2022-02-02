namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_cursor_info_t
    {
        public cef_point_t hotspot;
        public float image_scale_factor;
        public void* buffer;
        public cef_size_t size;
    }
}
