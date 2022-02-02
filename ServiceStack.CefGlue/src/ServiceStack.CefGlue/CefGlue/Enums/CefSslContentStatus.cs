//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_ssl_version_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// Supported SSL content status flags. See content/public/common/ssl_status.h
    /// for more information.
    /// </summary>
    [Flags]
    public enum CefSslContentStatus
    {
        Normal = 0,
        DisplayedInsecure = 1 << 0,
        RanInsecure = 1 << 1,
    }
}
