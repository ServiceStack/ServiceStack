namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_mouse_event_t
    {
        public int x;
        public int y;
        public CefEventFlags modifiers;

        public cef_mouse_event_t(int x, int y, CefEventFlags modifiers)
        {
            this.x = x;
            this.y = y;
            this.modifiers = modifiers;
        }
    }
}
