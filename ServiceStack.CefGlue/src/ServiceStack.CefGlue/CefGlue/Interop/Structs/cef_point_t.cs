//
// This file manually written from cef/include/internal/cef_types.h.
//
namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_point_t
    {
        public int x;
        public int y;

        public cef_point_t(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
