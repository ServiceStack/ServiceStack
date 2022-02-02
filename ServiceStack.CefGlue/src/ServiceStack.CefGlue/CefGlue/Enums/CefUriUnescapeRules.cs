//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_uri_unescape_rule_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// URI unescape rules passed to CefURIDecode().
    /// </summary>
    [Flags]
    public enum CefUriUnescapeRules
    {
        /// <summary>
        /// Don't unescape anything at all.
        /// </summary>
        None = 0 << 0,

        /// <summary>
        /// Don't unescape anything special, but all normal unescaping will happen.
        /// This is a placeholder and can't be combined with other flags (since it's
        /// just the absence of them). All other unescape rules imply "normal" in
        /// addition to their special meaning. Things like escaped letters, digits,
        /// and most symbols will get unescaped with this mode.
        /// </summary>
        Normal = 1 << 0,

        /// <summary>
        /// Convert %20 to spaces. In some places where we're showing URLs, we may
        /// want this. In places where the URL may be copied and pasted out, then
        /// you wouldn't want this since it might not be interpreted in one piece
        /// by other applications.
        /// </summary>
        Spaces = 1 << 1,

        /// <summary>
        /// Unescapes '/' and '\\'. If these characters were unescaped, the resulting
        /// URL won't be the same as the source one. Moreover, they are dangerous to
        /// unescape in strings that will be used as file paths or names. This value
        /// should only be used when slashes don't have special meaning, like data
        /// URLs.
        /// </summary>
        PathSeparators = 1 << 2,

        /// <summary>
        /// Unescapes various characters that will change the meaning of URLs,
        /// including '%', '+', '&amp;', '#'. Does not unescape path separators.
        /// If these characters were unescaped, the resulting URL won't be the same
        /// as the source one. This flag is used when generating final output like
        /// filenames for URLs where we won't be interpreting as a URL and want to do
        /// as much unescaping as possible.
        /// </summary>
        UrlSpecialCharsExceptPathSeparators = 1 << 3,

        /// <summary>
        /// URL queries use "+" for space. This flag controls that replacement.
        /// </summary>
        ReplacePlusWithSpace = 1 << 4,
    }
}
