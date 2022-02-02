//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_context_menu_type_flags_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// Supported context menu type flags.
    /// </summary>
    [Flags]
    public enum CefContextMenuTypeFlags
    {
        /// <summary>
        /// No node is selected.
        /// </summary>
        None = 0,

        /// <summary>
        /// The top page is selected.
        /// </summary>
        Page = 1 << 0,

        /// <summary>
        /// A subframe page is selected.
        /// </summary>
        Frame = 1 << 1,

        /// <summary>
        /// A link is selected.
        /// </summary>
        Link = 1 << 2,

        /// <summary>
        /// A media node is selected.
        /// </summary>
        Media = 1 << 3,

        /// <summary>
        /// There is a textual or mixed selection that is selected.
        /// </summary>
        Selection = 1 << 4,

        /// <summary>
        /// An editable element is selected.
        /// </summary>
        Editable = 1 << 5
    }
}
