namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class representing print settings.
    /// </summary>
    public sealed unsafe partial class CefPrintSettings
    {
        /// <summary>
        /// Create a new CefPrintSettings object.
        /// </summary>
        public static CefPrintSettings Create()
        {
            return CefPrintSettings.FromNative(
                cef_print_settings_t.create()
                );
        }

        /// <summary>
        /// Returns true if this object is valid. Do not call any other methods if this
        /// function returns false.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return cef_print_settings_t.is_valid(_self) != 0;
            }
        }

        /// <summary>
        /// Returns true if the values of this object are read-only. Some APIs may
        /// expose read-only objects.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return cef_print_settings_t.is_read_only(_self) != 0;
            }
        }

        /// <summary>
        /// Set the page orientation.
        /// </summary>
        public void SetOrientation(bool landscape)
        {
            cef_print_settings_t.set_orientation(_self, landscape ? 1 : 0);
        }

        /// <summary>
        /// Returns true if the orientation is landscape.
        /// </summary>
        public bool IsLandscape()
        {
            return cef_print_settings_t.is_landscape(_self) != 0;
        }

        /// <summary>
        /// Set the printer printable area in device units.
        /// Some platforms already provide flipped area. Set |landscape_needs_flip|
        /// to false on those platforms to avoid double flipping.
        /// </summary>
        public void SetPrinterPrintableArea(CefSize physicalSizeDeviceUnits, CefRectangle printableAreaDeviceUnits, bool landscapeNeedsFlip)
        {
            var n_physicalSizeDeviceUnits = new cef_size_t(
                physicalSizeDeviceUnits.Width,
                physicalSizeDeviceUnits.Height
                );

            var n_printableAreaDeviceUnits = new cef_rect_t(
                printableAreaDeviceUnits.X,
                printableAreaDeviceUnits.Y,
                printableAreaDeviceUnits.Width,
                printableAreaDeviceUnits.Height
                );

            cef_print_settings_t.set_printer_printable_area(
                _self,
                &n_physicalSizeDeviceUnits,
                &n_printableAreaDeviceUnits,
                landscapeNeedsFlip ? 1 : 0
                );
        }

        /// <summary>
        /// Set the device name.
        /// </summary>
        public void SetDeviceName(string name)
        {
            fixed (char* name_str = name)
            {
                var n_name = new cef_string_t(name_str, name != null ? name.Length : 0);
                cef_print_settings_t.set_device_name(_self, &n_name);
            }
        }

        /// <summary>
        /// Get the device name.
        /// </summary>
        public string DeviceName
        {
            get
            {
                var n_result = cef_print_settings_t.get_device_name(_self);
                if (n_result == null) return null;
                return cef_string_userfree.ToString(n_result);
            }
        }

        /// <summary>
        /// Set the DPI (dots per inch).
        /// </summary>
        public void SetDpi(int dpi)
        {
            cef_print_settings_t.set_dpi(_self, dpi);
        }

        /// <summary>
        /// Get the DPI (dots per inch).
        /// </summary>
        public int GetDpi()
        {
            return cef_print_settings_t.get_dpi(_self);
        }

        /// <summary>
        /// Set the page ranges.
        /// </summary>
        public void SetPageRanges(CefRange[] ranges)
        {
            var count = ranges != null ? ranges.Length : 0;
            var n_ranges = new cef_range_t[count];

            for (var i = 0; i < count; i++)
            {
                n_ranges[i].from = ranges[i].From;
                n_ranges[i].to = ranges[i].To;
            }

            fixed (cef_range_t* n_ranges_ptr = n_ranges)
            {
                cef_print_settings_t.set_page_ranges(_self, (UIntPtr)count, n_ranges_ptr);
            }
        }

        /// <summary>
        /// Returns the number of page ranges that currently exist.
        /// </summary>
        public int GetPageRangesCount()
        {
            return (int)cef_print_settings_t.get_page_ranges_count(_self);
        }

        /// <summary>
        /// Retrieve the page ranges.
        /// </summary>
        public CefRange[] GetPageRanges()
        {
            var count = GetPageRangesCount();
            if (count == 0) return new CefRange[0];

            var n_ranges = new cef_range_t[count];
            UIntPtr n_count = (UIntPtr)count;
            fixed (cef_range_t* n_ranges_ptr = n_ranges)
            {
                cef_print_settings_t.get_page_ranges(_self, &n_count, n_ranges_ptr);
            }

            count = (int)n_count;
            if (count == 0) return new CefRange[0];

            var ranges = new CefRange[count];

            for (var i = 0; i < count; i++)
            {
                ranges[i].From = n_ranges[i].from;
                ranges[i].To = n_ranges[i].to;
            }

            return ranges;
        }

        /// <summary>
        /// Set whether only the selection will be printed.
        /// </summary>
        public void SetSelectionOnly(bool selectionOnly)
        {
            cef_print_settings_t.set_selection_only(_self, selectionOnly ? 1 : 0);
        }

        /// <summary>
        /// Returns true if only the selection will be printed.
        /// </summary>
        public bool IsSelectionOnly
        {
            get
            {
                return cef_print_settings_t.is_selection_only(_self) != 0;
            }
        }

        /// <summary>
        /// Set whether pages will be collated.
        /// </summary>
        public void SetCollate(bool collate)
        {
            cef_print_settings_t.set_collate(_self, collate ? 1 : 0);
        }

        /// <summary>
        /// Returns true if pages will be collated.
        /// </summary>
        public bool WillCollate
        {
            get
            {
                return cef_print_settings_t.will_collate(_self) != 0;
            }
        }

        /// <summary>
        /// Set the color model.
        /// </summary>
        public void SetColorModel(CefColorModel colorModel)
        {
            cef_print_settings_t.set_color_model(_self, colorModel);
        }

        /// <summary>
        /// Get the color model.
        /// </summary>
        public CefColorModel GetColorModel()
        {
            return cef_print_settings_t.get_color_model(_self);
        }

        /// <summary>
        /// Set the number of copies.
        /// </summary>
        public void SetCopies(int copies)
        {
            cef_print_settings_t.set_copies(_self, copies);
        }

        /// <summary>
        /// Get the number of copies.
        /// </summary>
        public int GetCopies()
        {
            return cef_print_settings_t.get_copies(_self);
        }

        /// <summary>
        /// Set the duplex mode.
        /// </summary>
        public void SetDuplexMode(CefDuplexMode mode)
        {
            cef_print_settings_t.set_duplex_mode(_self, mode);
        }

        /// <summary>
        /// Get the duplex mode.
        /// </summary>
        public CefDuplexMode GetDuplexMode()
        {
            return cef_print_settings_t.get_duplex_mode(_self);
        }
    }
}
