//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_state_t.
//
namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents the state of a setting.
    /// </summary>
    public enum CefState : int
    {
        /// <summary>
        /// Use the default state for the setting.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Enable or allow the setting.
        /// </summary>
        Enabled,

        /// <summary>
        /// Disable or disallow the setting.
        /// </summary>
        Disabled,
    }
}
