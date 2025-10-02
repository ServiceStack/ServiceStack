#nullable enable
#if NET6_0_OR_GREATER
// The MIT License(MIT)
// 
// Copyright (c) 2017 Hangfire OÜ
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;

namespace ServiceStack.Cronos;

internal static class TimeZoneHelper
{
    // This method is here because .NET Framework, .NET Core and Mono works different when transition from standard time (ST) to
    // daylight saving time (DST) happens.
    // When DST ends you set the clocks backward. If you are in USA it happens on first sunday of November at 2:00 am. 
    // So duration from 1:00 am to 2:00 am repeats twice.
    // .NET Framework and .NET Core consider backward DST transition as [1:00 DST ->> 2:00 DST)--[1:00 ST --> 2:00 ST]. So 2:00 is not ambiguous, but 1:00 is ambiguous.
    // Mono consider backward DST transition as [1:00 DST ->> 2:00 DST]--(1:00 ST --> 2:00 ST]. So 2:00 is ambiguous, but 1:00 is not ambiguous.
    // We have to add 1 tick to ambiguousTime to have the same behavior for all frameworks. Thus 1:00 is ambiguous and 2:00 is not ambiguous. 
    public static bool IsAmbiguousTime(TimeZoneInfo zone, DateTime ambiguousTime)
    {
        return zone.IsAmbiguousTime(ambiguousTime.AddTicks(1));
    }

    public static TimeSpan GetDaylightOffset(TimeZoneInfo zone, DateTime ambiguousDateTime)
    {
        var offsets = GetAmbiguousOffsets(zone, ambiguousDateTime);
        var baseOffset = zone.GetUtcOffset(ambiguousDateTime);

        if (offsets[0] != baseOffset) return offsets[0];
        return offsets[1];
    }

    public static DateTimeOffset GetDaylightTimeStart(TimeZoneInfo zone, DateTime invalidDateTime)
    {
        var dstTransitionDateTime = new DateTime(invalidDateTime.Year, invalidDateTime.Month, invalidDateTime.Day,
            invalidDateTime.Hour, invalidDateTime.Minute, 0, 0, invalidDateTime.Kind);

        while (zone.IsInvalidTime(dstTransitionDateTime))
        {
            dstTransitionDateTime = dstTransitionDateTime.AddMinutes(1);
        }

        var dstOffset = zone.GetUtcOffset(dstTransitionDateTime);

        return new DateTimeOffset(dstTransitionDateTime, dstOffset);
    }

    public static DateTimeOffset GetStandardTimeStart(TimeZoneInfo zone, DateTime ambiguousTime, TimeSpan daylightOffset)
    {
        var dstTransitionEnd = GetDstTransitionEndDateTime(zone, ambiguousTime);
        var baseOffset = zone.GetUtcOffset(ambiguousTime);

        return new DateTimeOffset(dstTransitionEnd, daylightOffset).ToOffset(baseOffset);
    }

    public static DateTimeOffset GetAmbiguousIntervalEnd(TimeZoneInfo zone, DateTime ambiguousTime)
    {
        var dstTransitionEnd = GetDstTransitionEndDateTime(zone, ambiguousTime);
        var baseOffset = zone.GetUtcOffset(ambiguousTime);

        return new DateTimeOffset(dstTransitionEnd, baseOffset);
    }

    public static DateTimeOffset GetDaylightTimeEnd(TimeZoneInfo zone, DateTime ambiguousTime, TimeSpan daylightOffset)
    {
        var daylightTransitionEnd = GetDstTransitionEndDateTime(zone, ambiguousTime);

        return new DateTimeOffset(daylightTransitionEnd.AddTicks(-1), daylightOffset);
    }

    private static TimeSpan[] GetAmbiguousOffsets(TimeZoneInfo zone, DateTime ambiguousTime)
    {
        return zone.GetAmbiguousTimeOffsets(ambiguousTime.AddTicks(1));
    }

    private static DateTime GetDstTransitionEndDateTime(TimeZoneInfo zone, DateTime ambiguousDateTime)
    {
        var dstTransitionDateTime = new DateTime(ambiguousDateTime.Year, ambiguousDateTime.Month, ambiguousDateTime.Day,
            ambiguousDateTime.Hour, ambiguousDateTime.Minute, 0, 0, ambiguousDateTime.Kind);

        while (zone.IsAmbiguousTime(dstTransitionDateTime))
        {
            dstTransitionDateTime = dstTransitionDateTime.AddMinutes(1);
        }

        return dstTransitionDateTime;
    }
}
#endif
