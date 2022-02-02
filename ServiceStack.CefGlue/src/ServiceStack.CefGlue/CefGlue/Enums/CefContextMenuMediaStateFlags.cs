//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_context_menu_media_state_flags_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// Supported context menu media state bit flags.
    /// </summary>
    [Flags]
    public enum CefContextMenuMediaStateFlags
    {
        None = 0,
        Error = 1 << 0,
        Paused = 1 << 1,
        Muted = 1 << 2,
        Loop = 1 << 3,
        CanSave = 1 << 4,
        HasAudio = 1 << 5,
        HasVideo = 1 << 6,
        ControlRootElement = 1 << 7,
        CanPrint = 1 << 8,
        CanRotate = 1 << 9,
    }
}
