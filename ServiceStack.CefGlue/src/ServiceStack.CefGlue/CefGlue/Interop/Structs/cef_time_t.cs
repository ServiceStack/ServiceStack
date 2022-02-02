//
// This file manually written from cef/include/internal/cef_time.h.
//
namespace Xilium.CefGlue.Interop
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = libcef.ALIGN)]
    internal unsafe struct cef_time_t
    {
        private static readonly DateTime s_maxDateTime = new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Utc);

        public int year;
        public int month;
        public int day_of_week;
        public int day_of_month;
        public int hour;
        public int minute;
        public int second;
        public int millisecond;

        public cef_time_t(DateTime value)
        {
            value = value.ToUniversalTime();

            year = value.Year;
            month = value.Month;
            day_of_week = (int)value.DayOfWeek;
            day_of_month = value.Day;
            hour = value.Hour;
            minute = value.Minute;
            second = value.Second;
            millisecond = value.Millisecond;
        }

        public DateTime ToDateTime()
        {
            if (year > 9999) return s_maxDateTime;
            return new DateTime(
                year,
                month,
                day_of_month,
                hour,
                minute,
                second != 60 ? second : 59,
                millisecond,
                DateTimeKind.Utc
                );
        }

        public static DateTime ToDateTime(cef_time_t* ptr)
        {
            var year = ptr->year;
            if (year > 9999) return s_maxDateTime;
            return new DateTime(
                year,
                ptr->month,
                ptr->day_of_month,
                ptr->hour,
                ptr->minute,
                ptr->second != 60 ? ptr->second : 59,
                ptr->millisecond,
                DateTimeKind.Utc
                );
        }
    }
}
