namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_key_event_t
    {
        public CefKeyEventType type;
        public CefEventFlags modifiers;
        public int windows_key_code;
        public int native_key_code;
        public int is_system_key;
        public ushort character;
        public ushort unmodified_character;
        public int focus_on_editable_field;
    }
}
