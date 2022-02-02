//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_v8_accesscontrol_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// V8 access control values.
    /// </summary>
    [Flags]
    public enum CefV8AccessControl
    {
        Default = 0,
        AllCanRead = 1,
        AllCanWrite = 1 << 1,
        ProhibitsOverwriting = 1 << 2,
    }
}
