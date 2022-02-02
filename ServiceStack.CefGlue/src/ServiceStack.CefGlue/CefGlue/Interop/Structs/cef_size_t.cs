//
// This file manually written from cef/include/internal/cef_types.h.
//
namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_size_t
    {
        public int width;
        public int height;

        public cef_size_t(int width, int height)
        {
            this.width = width;
            this.height = height;
        }
    }
}
