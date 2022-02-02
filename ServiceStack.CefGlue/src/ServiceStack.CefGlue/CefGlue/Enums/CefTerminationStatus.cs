//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_termination_status_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Process termination status values.
    /// </summary>
    public enum CefTerminationStatus
    {
        /// <summary>
        /// Non-zero exit status.
        /// </summary>
        Termination,

        /// <summary>
        /// SIGKILL or task manager kill.
        /// </summary>
        WasKilled,

        /// <summary>
        /// Segmentation fault.
        /// </summary>
        ProcessCrashed,

        /// <summary>
        /// Out of memory. Some platforms may use TS_PROCESS_CRASHED instead.
        /// </summary>
        OutOfMemory,
    }
}
