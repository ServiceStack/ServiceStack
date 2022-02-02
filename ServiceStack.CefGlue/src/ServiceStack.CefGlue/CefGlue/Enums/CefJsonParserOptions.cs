//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_json_parser_options_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// Options that can be passed to CefParseJSON.
    /// </summary>
    [Flags]
    public enum CefJsonParserOptions
    {
        /// <summary>
        /// Parses the input strictly according to RFC 4627. See comments in Chromium's
        /// base/json/json_reader.h file for known limitations/deviations from the RFC.
        /// </summary>
        Rfc = 0,

        /// <summary>
        /// Allows commas to exist after the last element in structures.
        /// </summary>
        AllowTrailingCommas = 1 << 0,
    }
}
