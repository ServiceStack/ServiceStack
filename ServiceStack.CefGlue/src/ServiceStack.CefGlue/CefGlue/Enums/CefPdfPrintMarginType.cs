//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_pdf_print_margin_type_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Margin type for PDF printing.
    /// </summary>
    public enum CefPdfPrintMarginType
    {
        /// <summary>
        /// Default margins.
        /// </summary>
        Default,

        /// <summary>
        /// No margins.
        /// </summary>
        None,

        /// <summary>
        /// Minimum margins.
        /// </summary>
        Minimum,

        /// <summary>
        /// Custom margins using the |margin_*| values from cef_pdf_print_settings_t.
        /// </summary>
        Custom,
    }
}
