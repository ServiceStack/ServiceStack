//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_dom_event_category_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// DOM event category flags.
    /// </summary>
    public enum CefDomEventCategory
    {
        Unknown = 0x0,
        UI = 0x1,
        Mouse = 0x2,
        Mutation = 0x4,
        Keyboard = 0x8,
        Text = 0x10,
        Composition = 0x20,
        Drag = 0x40,
        Clipboard = 0x80,
        Message = 0x100,
        Wheel = 0x200,
        BeforeTextInserted = 0x400,
        Overflow = 0x800,
        PageTransition = 0x1000,
        PopState = 0x2000,
        Progress = 0x4000,
        XmlHttpRequestProgress = 0x8000,
    }
}
