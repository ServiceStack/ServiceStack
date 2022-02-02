//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_log_severity_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Log severity levels.
    /// </summary>
    public enum CefLogSeverity
    {
        /// <summary>
        /// Default logging (currently INFO logging).
        /// </summary>
        Default,

        /// <summary>
        /// Verbose logging.
        /// </summary>
        Verbose,

        /// <summary>
        /// DEBUG logging.
        /// </summary>
        Debug = Verbose,

        /// <summary>
        /// INFO logging.
        /// </summary>
        Info,

        /// <summary>
        /// WARNING logging.
        /// </summary>
        Warning,

        /// <summary>
        /// ERROR logging.
        /// </summary>
        Error,

        /// <summary>
        /// FATAL logging.
        /// </summary>
        Fatal,

        /// <summary>
        /// Disable logging to file for all messages, and to stderr for messages with
        /// severity less than FATAL.
        /// </summary>
        Disable = 99,
    }
}
