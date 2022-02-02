//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_alpha_type_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// Describes how to interpret the alpha component of a pixel.
    /// </summary>
    public enum CefAlphaType
    {
        /// <summary>
        /// No transparency. The alpha component is ignored.
        /// </summary>
        Opaque,

        /// <summary>
        /// Transparency with pre-multiplied alpha component.
        /// </summary>
        Premultiplied,

        /// <summary>
        /// Transparency with post-multiplied alpha component.
        /// </summary>
        Postmultiplied,
    }
}
