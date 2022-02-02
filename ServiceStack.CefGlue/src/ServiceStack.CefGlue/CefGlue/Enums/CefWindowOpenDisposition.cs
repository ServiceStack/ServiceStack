//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_window_open_disposition_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// The manner in which a link click should be opened. These constants match
    /// their equivalents in Chromium's window_open_disposition.h and should not be
    /// renumbered.
    /// </summary>
    public enum CefWindowOpenDisposition
    {
        Unknown = 0,
        CurrentTab,
        SingletonTab,
        NewForegroundTab,
        NewBackgroundTab,
        NewPopup,
        NewWindow,
        SaveToDisk,
        OffTheRecord,
        IgnoreAction,
    }
}
