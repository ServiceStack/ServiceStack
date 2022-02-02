namespace Xilium.CefGlue
{
    using System;
    using Xilium.CefGlue.Interop;

    [Serializable]
    public sealed class CefPdfPrintSettings
    {
        /// <summary>
        /// Page title to display in the header. Only used if |header_footer_enabled|
        /// is set to true (1).
        /// </summary>
        public string HeaderFooterTitle { get; set; }

        /// <summary>
        /// URL to display in the footer. Only used if |header_footer_enabled| is set
        /// to true (1).
        /// </summary>
        public string HeaderFooterUrl { get; set; }

        /// <summary>
        /// Output page size in microns. If either of these values is less than or
        /// equal to zero then the default paper size (A4) will be used.
        /// </summary>
        public int PageWidth { get; set; }
        public int PageHeight { get; set; }

        /// <summary>
        /// The percentage to scale the PDF by before printing (e.g. 50 is 50%).
        /// If this value is less than or equal to zero the default value of 100
        /// will be used.
        /// </summary>
        public int ScaleFactor { get; set; }

        /// <summary>
        /// Margins in points. Only used if |margin_type| is set to PDF_PRINT_MARGIN_CUSTOM.
        /// PDF_PRINT_MARGIN_CUSTOM.
        /// </summary>
        public int MarginTop { get; set; }
        public int MarginRight { get; set; }
        public int MarginBottom { get; set; }
        public int MarginLeft { get; set; }

        /// <summary>
        /// Margin type.
        /// </summary>
        public CefPdfPrintMarginType MarginType { get; set; }

        /// <summary>
        /// Set to true (1) to print headers and footers or false (0) to not print
        /// headers and footers.
        /// </summary>
        public bool HeaderFooterEnabled { get; set; }

        /// <summary>
        /// Set to true (1) to print the selection only or false (0) to print all.
        /// </summary>
        public bool SelectionOnly { get; set; }

        /// <summary>
        /// Set to true (1) for landscape mode or false (0) for portrait mode.
        /// </summary>
        public bool Landscape { get; set; }

        /// <summary>
        /// Set to true (1) to print background graphics or false (0) to not print
        /// background graphics.
        /// </summary>
        public bool BackgroundsEnabled { get; set; }

        internal unsafe cef_pdf_print_settings_t* ToNative()
        {
            var ptr = cef_pdf_print_settings_t.Alloc();

            cef_string_t.Copy(HeaderFooterTitle, &ptr->header_footer_title);
            cef_string_t.Copy(HeaderFooterUrl, &ptr->header_footer_url);
            ptr->page_width = PageWidth;
            ptr->page_height = PageHeight;
            ptr->scale_factor = ScaleFactor;
            ptr->margin_top = MarginTop;
            ptr->margin_right = MarginRight;
            ptr->margin_bottom = MarginBottom;
            ptr->margin_left = MarginLeft;
            ptr->margin_type = MarginType;
            ptr->header_footer_enabled = HeaderFooterEnabled ? 1 : 0;
            ptr->selection_only = SelectionOnly ? 1 : 0;
            ptr->landscape = Landscape ? 1 : 0;
            ptr->backgrounds_enabled = BackgroundsEnabled ? 1 : 0;

            return ptr;
        }
    }
}
