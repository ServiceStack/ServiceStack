//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_context_menu_edit_state_flags_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// Supported context menu edit state bit flags.
    /// </summary>
    [Flags]
    public enum CefContextMenuEditStateFlags
    {
        None = 0,
        CanUndo = 1 << 0,
        CanRedo = 1 << 1,
        CanCut = 1 << 2,
        CanCopy = 1 << 3,
        CanPaste = 1 << 4,
        CanDelete = 1 << 5,
        CanSelectAll = 1 << 6,
        CanTranslate = 1 << 7,
    }
}
