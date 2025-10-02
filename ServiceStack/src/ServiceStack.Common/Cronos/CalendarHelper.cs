#nullable enable
#if NET6_0_OR_GREATER
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace ServiceStack.Cronos;

internal static class CalendarHelper
{
    private const int DaysPerWeekCount = 7;

    private const long TicksPerMillisecond = 10000;
    private const long TicksPerSecond = TicksPerMillisecond * 1000;
    private const long TicksPerMinute = TicksPerSecond * 60;
    private const long TicksPerHour = TicksPerMinute * 60;
    private const long TicksPerDay = TicksPerHour * 24;

    // Number of days in a non-leap year
    private const int DaysPerYear = 365;
    // Number of days in 4 years
    private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
    // Number of days in 100 years
    private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
    // Number of days in 400 years
    private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

    private static readonly int[] DaysToMonth365 =
    {
        0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365
    };

    private static readonly int[] DaysToMonth366 =
    {
        0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366
    };

    private static readonly int[] DaysInMonth =
    {
        -1, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
    };

    public static bool IsGreaterThan(int year1, int month1, int day1, int year2, int month2, int day2)
    {
        if (year1 != year2) return year1 > year2;
        if (month1 != month2) return month1 > month2;
        if (day2 != day1) return day1 > day2;
        return false;
    }

    public static long DateTimeToTicks(int year, int month, int day, int hour, int minute, int second)
    {
        int[] days = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0) ? DaysToMonth366 : DaysToMonth365;
        int y = year - 1;
        int n = y * 365 + y / 4 - y / 100 + y / 400 + days[month - 1] + day - 1;
        return n * TicksPerDay + (hour * 3600L + minute * 60L + second) * TicksPerSecond;
    }

    // Returns a given date part of this DateTime. This method is used
    // to compute the year, day-of-year, month, or day part.
    public static void FillDateTimeParts(long ticks, out int second, out int minute, out int hour,
        out int day, out int month, out int year)
    {
        second = (int) (ticks / TicksPerSecond % 60);
        if (ticks % TicksPerSecond != 0) second++;
        minute = (int) (ticks / TicksPerMinute % 60);
        hour = (int) (ticks / TicksPerHour % 24);

        // n = number of days since 1/1/0001
        int n = (int) (ticks / TicksPerDay);
        // y400 = number of whole 400-year periods since 1/1/0001
        int y400 = n / DaysPer400Years;
        // n = day number within 400-year period
        n -= y400 * DaysPer400Years;
        // y100 = number of whole 100-year periods within 400-year period
        int y100 = n / DaysPer100Years;
        // Last 100-year period has an extra day, so decrement result if 4
        if (y100 == 4) y100 = 3;
        // n = day number within 100-year period
        n -= y100 * DaysPer100Years;
        // y4 = number of whole 4-year periods within 100-year period
        int y4 = n / DaysPer4Years;
        // n = day number within 4-year period
        n -= y4 * DaysPer4Years;
        // y1 = number of whole years within 4-year period
        int y1 = n / DaysPerYear;
        // Last year has an extra day, so decrement result if 4
        if (y1 == 4) y1 = 3;
        // If year was requested, compute and return it
        year = y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;
        // n = day number within year
        n -= y1 * DaysPerYear;
        // Leap year calculation looks different from IsLeapYear since y1, y4,
        // and y100 are relative to year 1, not year 0
        bool leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
        int[] days = leapYear ? DaysToMonth366 : DaysToMonth365;
        // All months have less than 32 days, so n >> 5 is a good conservative
        // estimate for the month
        month = (n >> 5) + 1;
        // m = 1-based month number

        // day = 1-based day-of-month
        while (n >= days[month]) month++;
        day = n - days[month - 1] + 1;
    }

    public static DayOfWeek GetDayOfWeek(int year, int month, int day)
    {
        var isLeapYear = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        int[] days = isLeapYear ? DaysToMonth366 : DaysToMonth365;
        int y = year - 1;
        int n = y * 365 + y / 4 - y / 100 + y / 400 + days[month - 1] + day - 1;
        var ticks = n * TicksPerDay;

        return ((DayOfWeek)((int)(ticks / TicksPerDay + 1) % 7));
    }

    public static int GetDaysInMonth(int year, int month)
    {
        if (month != 2 || year % 4 != 0) return DaysInMonth[month];

        return year % 100 != 0 || year % 400 == 0 ? 29 : 28;
    }

    public static int MoveToNearestWeekDay(int year, int month, int day)
    {
        var dayOfWeek = GetDayOfWeek(year, month, day);

        if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday) return day;

        return dayOfWeek == DayOfWeek.Sunday
            ? day == GetDaysInMonth(year, month)
                ? day - 2
                : day + 1
            : day == CronField.DaysOfMonth.First
                ? day + 2
                : day - 1;
    }

    public static bool IsNthDayOfWeek(int day, int n)
    {
        return day - DaysPerWeekCount * n < CronField.DaysOfMonth.First &&
               day - DaysPerWeekCount * (n - 1) >= CronField.DaysOfMonth.First;
    }

    public static bool IsLastDayOfWeek(int year, int month, int day)
    {
        return day + DaysPerWeekCount > GetDaysInMonth(year, month);
    }
}
#endif