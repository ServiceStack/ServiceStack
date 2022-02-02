//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_xml_encoding_type_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Supported XML encoding types. The parser supports ASCII, ISO-8859-1, and
    /// UTF16 (LE and BE) by default. All other types must be translated to UTF8
    /// before being passed to the parser. If a BOM is detected and the correct
    /// decoder is available then that decoder will be used automatically.
    /// </summary>
    public enum CefXmlEncoding
    {
       None = 0,
       Utf8,
       Utf16LE,
       Utf16BE,
       Ascii,
    }
}
