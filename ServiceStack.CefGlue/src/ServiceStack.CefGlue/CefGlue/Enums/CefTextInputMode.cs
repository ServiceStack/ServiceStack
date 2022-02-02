//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_text_input_mode_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Input mode of a virtual keyboard. These constants match their equivalents
    /// in Chromium's text_input_mode.h and should not be renumbered.
    /// See https://html.spec.whatwg.org/#input-modalities:-the-inputmode-attribute
    /// </summary>
    public enum CefTextInputMode
    {
        Default,
        None,
        Text,
        Tel,
        Url,
        Email,
        Numeric,
        Decimal,
        Search,

        Max = Search,
    }
}
