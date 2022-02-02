//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_event_flags_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// Supported event bit flags.
    /// </summary>
    [Flags]
    public enum CefEventFlags : uint
    {
        None              = 0,

        CapsLockOn        = 1 << 0,

        ShiftDown         = 1 << 1,
        ControlDown       = 1 << 2,
        AltDown           = 1 << 3,

        LeftMouseButton   = 1 << 4,
        MiddleMouseButton = 1 << 5,
        RightMouseButton  = 1 << 6,

        /// <summary>
        /// Mac OS-X command key.
        /// </summary>
        CommandDown       = 1 << 7,

        NumLockOn         = 1 << 8,
        IsKeyPad          = 1 << 9,
        IsLeft            = 1 << 10,
        IsRight           = 1 << 11,
        AltGrDown         = 1 << 12,
    }
}
