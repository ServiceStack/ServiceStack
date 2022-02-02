//
// This file manually written from cef/include/internal/cef_time.h.
//
// See also:
//   /Interop/Structs/cef_time_t.cs
//
namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;

    internal static unsafe partial class libcef
    {
        /*
        ///
        // Converts cef_time_t to/from time_t. Returns true (1) on success and false (0)
        // on failure.
        ///
        CEF_EXPORT int cef_time_to_timet(const cef_time_t* cef_time, time_t* time);
        CEF_EXPORT int cef_time_from_timet(time_t time, cef_time_t* cef_time);

        ///
        // Converts cef_time_t to/from a double which is the number of seconds since
        // epoch (Jan 1, 1970). Webkit uses this format to represent time. A value of 0
        // means "not initialized". Returns true (1) on success and false (0) on
        // failure.
        ///
        CEF_EXPORT int cef_time_to_doublet(const cef_time_t* cef_time, double* time);
        CEF_EXPORT int cef_time_from_doublet(double time, cef_time_t* cef_time);

        ///
        // Retrieve the current system time.
        //
        CEF_EXPORT int cef_time_now(cef_time_t* cef_time);

        ///
        // Retrieve the delta in milliseconds between two time values.
        //
        CEF_EXPORT int cef_time_delta(const cef_time_t* cef_time1,
                                      const cef_time_t* cef_time2,
                                      long long* delta);
        */
    }
}
