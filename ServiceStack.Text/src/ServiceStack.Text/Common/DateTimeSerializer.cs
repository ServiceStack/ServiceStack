//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Globalization;
using System.IO;
using ServiceStack.Text.Json;
using ServiceStack.Text.Support;
using System.Text.RegularExpressions;

namespace ServiceStack.Text.Common;

public static class DateTimeSerializer
{
    public const string CondensedDateTimeFormat = "yyyyMMdd";                             //8
    public const string ShortDateTimeFormat = "yyyy-MM-dd";                               //11
    public const string DefaultDateTimeFormat = "dd/MM/yyyy HH:mm:ss";                    //20
    public const string DefaultDateTimeFormatWithFraction = "dd/MM/yyyy HH:mm:ss.fff";    //24
    public const string XsdDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";               //29
    public const string XsdDateTimeFormat3F = "yyyy-MM-ddTHH:mm:ss.fffZ";                 //25
    public const string XsdDateTimeFormatSeconds = "yyyy-MM-ddTHH:mm:ssZ";                //21
    public const string DateTimeFormatSecondsUtcOffset = "yyyy-MM-ddTHH:mm:sszzz";        //22
    public const string DateTimeFormatSecondsNoOffset = "yyyy-MM-ddTHH:mm:ss";
    public const string DateTimeFormatTicksUtcOffset = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";  //30
    public const string DateTimeFormatTicksNoUtcOffset = "yyyy-MM-ddTHH:mm:ss.fffffff";

    public const string EscapedWcfJsonPrefix = "\\/Date(";
    public const string EscapedWcfJsonSuffix = ")\\/";
    public const string WcfJsonPrefix = "/Date(";
    public const char WcfJsonSuffix = ')';
    public const string UnspecifiedOffset = "-0000";
    public const string UtcOffset = "+0000";

    private const char XsdTimeSeparator = 'T';
    private static readonly int XsdTimeSeparatorIndex = XsdDateTimeFormat.IndexOf(XsdTimeSeparator);
    private const string XsdUtcSuffix = "Z";
    private static readonly char[] DateTimeSeparators = ['-', '/'];
    private static readonly Regex UtcOffsetInfoRegex = new("([+-](?:2[0-3]|[0-1][0-9]):[0-5][0-9])", RegexOptions.Compiled);
    public static Func<string, Exception, DateTime> OnParseErrorFn { get; set; }

    /// <summary>
    /// If AlwaysUseUtc is set to true then convert all DateTime to UTC. If PreserveUtc is set to true then UTC dates will not convert to local
    /// </summary>
    public static DateTime Prepare(this DateTime dateTime, bool parsedAsUtc = false)
    {
        var config = JsConfig.GetConfig();
        if (config.SkipDateTimeConversion)
            return dateTime;

        if (config.AlwaysUseUtc)
            return dateTime.Kind != DateTimeKind.Utc ? dateTime.ToStableUniversalTime() : dateTime;

        return parsedAsUtc ? dateTime.ToLocalTime() : dateTime;
    }

    public static DateTime? ParseShortestNullableXsdDateTime(string dateTimeStr)
    {
        if (string.IsNullOrEmpty(dateTimeStr))
            return null;

        return ParseShortestXsdDateTime(dateTimeStr);
    }

    public static DateTime ParseRFC1123DateTime(string dateTimeStr)
    {
        return DateTime.ParseExact(dateTimeStr, "r", CultureInfo.InvariantCulture);
    }

    public static DateTime ParseShortestXsdDateTime(string dateTimeStr)
    {
        if (string.IsNullOrEmpty(dateTimeStr))
            return DateTime.MinValue;

        if (dateTimeStr.StartsWith(EscapedWcfJsonPrefix, StringComparison.Ordinal) || dateTimeStr.StartsWith(WcfJsonPrefix, StringComparison.Ordinal))
            return ParseWcfJsonDate(dateTimeStr).Prepare();

        var config = JsConfig.GetConfig();

#if NET6_0_OR_GREATER
        if (config.SystemJsonCompatible)
        {
            try
            {
                var value = System.Text.Json.JsonSerializer.Deserialize<DateTime>('"' + dateTimeStr + '"');
                return value;
            }
            catch (Exception e)
            {
                Tracer.Instance.WriteWarning("Could not parse System.Text.Json DateTime '{0}': {1}", dateTimeStr, e);
            }
        }
#endif

        try
        {
            if (dateTimeStr.Length == DefaultDateTimeFormat.Length)
            {
                var unspecifiedDate = DateTime.Parse(dateTimeStr, CultureInfo.InvariantCulture);

                if (config.AssumeUtc)
                    unspecifiedDate = DateTime.SpecifyKind(unspecifiedDate, DateTimeKind.Utc);

                return unspecifiedDate.Prepare();
            }

            var hasUtcSuffix = dateTimeStr.EndsWith(XsdUtcSuffix);
            if (!hasUtcSuffix && dateTimeStr.Length == DefaultDateTimeFormatWithFraction.Length)
            {
                var unspecifiedDate = config.AssumeUtc
                    ? DateTime.Parse(dateTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                    : DateTime.Parse(dateTimeStr, CultureInfo.InvariantCulture);

                return unspecifiedDate.Prepare();
            }

            var kind = hasUtcSuffix
                ? DateTimeKind.Utc
                : DateTimeKind.Unspecified;
            switch (config.DateHandler)
            {
                case DateHandler.UnixTime:
                    if (int.TryParse(dateTimeStr, out var unixTime))
                        return unixTime.FromUnixTime();
                    break;
                case DateHandler.UnixTimeMs:
                    if (long.TryParse(dateTimeStr, out var unixTimeMs))
                        return unixTimeMs.FromUnixTimeMs();
                    break;
                case DateHandler.ISO8601:
                case DateHandler.ISO8601DateOnly:
                case DateHandler.ISO8601DateTime:
                    if (config.SkipDateTimeConversion)
                        dateTimeStr = RemoveUtcOffsets(dateTimeStr, out kind);
                    break;
            }

            dateTimeStr = RepairXsdTimeSeparator(dateTimeStr);

            if (dateTimeStr.Length == XsdDateTimeFormatSeconds.Length)
                return DateTime.ParseExact(dateTimeStr, XsdDateTimeFormatSeconds, null, DateTimeStyles.AdjustToUniversal).Prepare(parsedAsUtc: true);

            if (dateTimeStr.Length >= XsdDateTimeFormat3F.Length
                && dateTimeStr.Length <= XsdDateTimeFormat.Length
                && hasUtcSuffix)
            {
                var dateTime = Env.IsMono ? ParseManual(dateTimeStr) : null;
                if (dateTime != null)
                    return dateTime.Value;

                return PclExport.Instance.ParseXsdDateTimeAsUtc(dateTimeStr);
            }

            if (dateTimeStr.Length == CondensedDateTimeFormat.Length && dateTimeStr.IndexOfAny(DateTimeSeparators) == -1)
            {
                return DateTime.ParseExact(dateTimeStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);
            }

            if (dateTimeStr.Length == ShortDateTimeFormat.Length)
            {
                try
                {
                    var manualDate = ParseManual(dateTimeStr);
                    if (manualDate != null)
                        return manualDate.Value;
                }
                catch { }
            }

            try
            {
                if (config.SkipDateTimeConversion)
                {
                    var dateTimeStyle = kind == DateTimeKind.Unspecified
                        ? DateTimeStyles.None
                        : kind == DateTimeKind.Local
                            ? DateTimeStyles.AssumeLocal
                            : DateTimeStyles.AssumeUniversal;
                    if (config.AlwaysUseUtc)
                        dateTimeStyle |= DateTimeStyles.AdjustToUniversal;
                    return DateTime.Parse(dateTimeStr, null, dateTimeStyle);
                }

                var assumeKind = config.AssumeUtc ? DateTimeStyles.AssumeUniversal : DateTimeStyles.AssumeLocal;
                var dateTime = DateTime.Parse(dateTimeStr, CultureInfo.InvariantCulture, assumeKind);
                return dateTime.Prepare();
            }
            catch (FormatException)
            {
                var manualDate = ParseManual(dateTimeStr);
                if (manualDate != null)
                    return manualDate.Value;

                throw;
            }
        }
        catch (Exception ex)
        {
            if (OnParseErrorFn != null)
                return OnParseErrorFn(dateTimeStr, ex);

            throw;
        }
    }

    private static string RemoveUtcOffsets(string dateTimeStr, out DateTimeKind kind)
    {
        var startOfTz = UtcOffsetInfoRegex.Match(dateTimeStr);
        if (startOfTz.Index > 0)
        {
            kind = DateTimeKind.Local;
            return dateTimeStr.Substring(0, startOfTz.Index);
        }
        kind = dateTimeStr.Contains("Z") ? DateTimeKind.Utc : DateTimeKind.Unspecified;
        return dateTimeStr;
    }

    /// <summary>
    /// Repairs an out-of-spec XML date/time string which incorrectly uses a space instead of a 'T' to separate the date from the time.
    /// These string are occasionally generated by SQLite and can cause errors in OrmLite when reading these columns from the DB.
    /// </summary>
    /// <param name="dateTimeStr">The XML date/time string to repair</param>
    /// <returns>The repaired string. If no repairs were made, the original string is returned.</returns>
    private static string RepairXsdTimeSeparator(string dateTimeStr)
    {
        if ((dateTimeStr.Length > XsdTimeSeparatorIndex) && (dateTimeStr[XsdTimeSeparatorIndex] == ' ') && dateTimeStr.EndsWith(XsdUtcSuffix))
        {
            dateTimeStr = dateTimeStr.Substring(0, XsdTimeSeparatorIndex) + XsdTimeSeparator +
                          dateTimeStr.Substring(XsdTimeSeparatorIndex + 1);
        }

        return dateTimeStr;
    }

    public static DateTime? ParseManual(string dateTimeStr)
    {
        var config = JsConfig.GetConfig();
        var dateKind = config.AssumeUtc || config.AlwaysUseUtc
            ? DateTimeKind.Utc
            : DateTimeKind.Local;

        var date = ParseManual(dateTimeStr, dateKind);
        if (date == null)
            return null;

        return dateKind == DateTimeKind.Local
            ? date.Value.ToLocalTime().Prepare()
            : date;
    }

    public static DateTime? ParseManual(string dateTimeStr, DateTimeKind dateKind)
    {
        if (dateTimeStr == null || dateTimeStr.Length < ShortDateTimeFormat.Length)
            return null;

        if (dateTimeStr.EndsWith(XsdUtcSuffix))
        {
            dateTimeStr = dateTimeStr.Substring(0, dateTimeStr.Length - 1);
            dateKind = JsConfig.SkipDateTimeConversion ? DateTimeKind.Utc : dateKind;
        }

        var parts = dateTimeStr.Split('T');
        if (parts.Length == 1)
            parts = dateTimeStr.SplitOnFirst(' ');

        var dateParts = parts[0].Split('-', '/');
        int hh = 0, min = 0, ss = 0, ms = 0;
        double subMs = 0;
        int offsetMultiplier = 0;

        if (parts.Length == 1)
        {
            return dateParts.Length == 3 && dateParts[2].Length == "YYYY".Length
                ? new DateTime(int.Parse(dateParts[2]), int.Parse(dateParts[1]), int.Parse(dateParts[0]), 0, 0, 0, 0,
                    dateKind)
                : new DateTime(int.Parse(dateParts[0]), int.Parse(dateParts[1]), int.Parse(dateParts[2]), 0, 0, 0, 0,
                    dateKind);
        }
        else if (parts.Length == 2)
        {
            var timeStringParts = parts[1].Split('+');
            if (timeStringParts.Length == 2)
            {
                offsetMultiplier = -1;
            }
            else
            {
                timeStringParts = parts[1].Split('-');
                if (timeStringParts.Length == 2)
                {
                    offsetMultiplier = 1;
                }
            }

            var timeOffset = timeStringParts.Length == 2 ? timeStringParts[1] : null;
            var timeParts = timeStringParts[0].Split(':');

            if (timeParts.Length == 3)
            {
                int.TryParse(timeParts[0], out hh);
                int.TryParse(timeParts[1], out min);

                var secParts = timeParts[2].Split('.');
                int.TryParse(secParts[0], out ss);
                if (secParts.Length == 2)
                {
                    var msStr = secParts[1].PadRight(3, '0');
                    ms = int.Parse(msStr.Substring(0, 3));

                    if (msStr.Length > 3)
                    {
                        var subMsStr = msStr.Substring(3);
                        subMs = double.Parse(subMsStr) / Math.Pow(10, subMsStr.Length);
                    }
                }
            }

            var dateTime = new DateTime(int.Parse(dateParts[0]), int.Parse(dateParts[1]), int.Parse(dateParts[2]), hh, min,
                ss, ms, dateKind);
            if (subMs != 0)
            {
                dateTime = dateTime.AddMilliseconds(subMs);
            }

            if (offsetMultiplier != 0 && timeOffset != null)
            {
                timeParts = timeOffset.Split(':');
                if (timeParts.Length == 2)
                {
                    hh = int.Parse(timeParts[0]);
                    min = int.Parse(timeParts[1]);
                }
                else
                {
                    hh = int.Parse(timeOffset.Substring(0, 2));
                    min = int.Parse(timeOffset.Substring(2));
                }

                dateTime = dateTime.AddHours(offsetMultiplier * hh);
                dateTime = dateTime.AddMinutes(offsetMultiplier * min);
            }

            return dateTime;
        }

        return null;
    }

    public static string ToDateTimeString(DateTime dateTime)
    {
        return dateTime.ToStableUniversalTime().ToString(XsdDateTimeFormat, CultureInfo.InvariantCulture);
    }

    public static DateTime ParseDateTime(string dateTimeStr)
    {
        return DateTime.ParseExact(dateTimeStr, XsdDateTimeFormat, null);
    }

    public static DateTimeOffset ParseDateTimeOffset(string dateTimeOffsetStr)
    {
        if (string.IsNullOrEmpty(dateTimeOffsetStr)) 
            return default;
        
#if NET6_0_OR_GREATER
        var config = JsConfig.GetConfig();
        if (config.SystemJsonCompatible)
        {
            try
            {
                var value = System.Text.Json.JsonSerializer.Deserialize<DateTimeOffset>('"' + dateTimeOffsetStr + '"');
                return value;
            }
            catch (Exception e)
            {
                Tracer.Instance.WriteError("Could not parse System.Text.Json DateTimeOffset '{0}': {1}", dateTimeOffsetStr, e);
            }
        }
#endif

        // for interop, do not assume format based on config
        // format: prefer TimestampOffset, DCJSCompatible
        if (dateTimeOffsetStr.StartsWith(EscapedWcfJsonPrefix, StringComparison.Ordinal) ||
            dateTimeOffsetStr.StartsWith(WcfJsonPrefix, StringComparison.Ordinal))
        {
            return ParseWcfJsonDateOffset(dateTimeOffsetStr);
        }

        // format: next preference ISO8601
        // assume utc when no offset specified
        if (dateTimeOffsetStr.LastIndexOfAny(TimeZoneChars) < 10)
        {
            if (!dateTimeOffsetStr.EndsWith(XsdUtcSuffix)) dateTimeOffsetStr += XsdUtcSuffix;
            if (Env.IsMono)
            {
                // Without that Mono uses a Local timezone))
                dateTimeOffsetStr = dateTimeOffsetStr.Substring(0, dateTimeOffsetStr.Length - 1) + "+00:00";
            }
        }

        return DateTimeOffset.Parse(dateTimeOffsetStr, CultureInfo.InvariantCulture);
    }

    public static DateTimeOffset? ParseNullableDateTimeOffset(string dateTimeOffsetStr)
    {
        if (string.IsNullOrEmpty(dateTimeOffsetStr)) return null;

        return ParseDateTimeOffset(dateTimeOffsetStr);
    }

    public static string ToXsdDateTimeString(DateTime dateTime)
    {
        return PclExport.Instance.ToXsdDateTimeString(dateTime);
    }

    public static string ToLocalXsdDateTimeString(DateTime dateTime)
    {
        return PclExport.Instance.ToLocalXsdDateTimeString(dateTime);
    }

    public static string ToXsdTimeSpanString(TimeSpan timeSpan)
    {
        return TimeSpanConverter.ToXsdDuration(timeSpan);
    }

    public static string ToXsdTimeSpanString(TimeSpan? timeSpan)
    {
        return (timeSpan != null) ? ToXsdTimeSpanString(timeSpan.Value) : null;
    }

    public static DateTime ParseXsdDateTime(string dateTimeStr)
    {
        dateTimeStr = RepairXsdTimeSeparator(dateTimeStr);
        return PclExport.Instance.ParseXsdDateTime(dateTimeStr);
    }

    public static TimeSpan ParseTimeSpan(string dateTimeStr)
    {
        return dateTimeStr.StartsWith("P", StringComparison.Ordinal) || dateTimeStr.StartsWith("-P", StringComparison.Ordinal)
            ? ParseXsdTimeSpan(dateTimeStr)
            : dateTimeStr.Contains(":")
                ? TimeSpan.Parse(dateTimeStr)
                : ParseNSTimeInterval(dateTimeStr);
    }

    public static TimeSpan ParseNSTimeInterval(string doubleInSecs)
    {
        var secs = double.Parse(doubleInSecs, CultureInfo.InvariantCulture);
        return TimeSpan.FromSeconds(secs);
    }

    public static TimeSpan? ParseNullableTimeSpan(string dateTimeStr)
    {
        return string.IsNullOrEmpty(dateTimeStr)
            ? (TimeSpan?)null
            : ParseTimeSpan(dateTimeStr);
    }

    public static TimeSpan ParseXsdTimeSpan(string dateTimeStr)
    {
        return TimeSpanConverter.FromXsdDuration(dateTimeStr);
    }

    public static TimeSpan? ParseXsdNullableTimeSpan(string dateTimeStr)
    {
        return string.IsNullOrEmpty(dateTimeStr) ?
            null :
            new TimeSpan?(ParseXsdTimeSpan(dateTimeStr));
    }

    public static string ToShortestXsdDateTimeString(DateTime dateTime)
    {
        var config = JsConfig.GetConfig();

        dateTime = dateTime.UseConfigSpecifiedSetting();
        if (!string.IsNullOrEmpty(config.DateTimeFormat))
        {
            return dateTime.ToString(config.DateTimeFormat, CultureInfo.InvariantCulture);
        }

        var timeOfDay = dateTime.TimeOfDay;
        var isStartOfDay = timeOfDay.Ticks == 0;
        if (isStartOfDay && !config.SkipDateTimeConversion)
            return dateTime.ToString(ShortDateTimeFormat, CultureInfo.InvariantCulture);

        var hasFractionalSecs = (timeOfDay.Milliseconds != 0)
                                || (timeOfDay.Ticks % TimeSpan.TicksPerMillisecond != 0);

        if (config.SkipDateTimeConversion)
        {
            if (!hasFractionalSecs)
                return dateTime.Kind == DateTimeKind.Local
                    ? dateTime.ToString(DateTimeFormatSecondsUtcOffset, CultureInfo.InvariantCulture)
                    : dateTime.Kind == DateTimeKind.Unspecified
                        ? dateTime.ToString(DateTimeFormatSecondsNoOffset, CultureInfo.InvariantCulture)
                        : dateTime.ToStableUniversalTime().ToString(XsdDateTimeFormatSeconds, CultureInfo.InvariantCulture);

            return dateTime.Kind == DateTimeKind.Local
                ? dateTime.ToString(DateTimeFormatTicksUtcOffset, CultureInfo.InvariantCulture)
                : dateTime.Kind == DateTimeKind.Unspecified
                    ? dateTime.ToString(DateTimeFormatTicksNoUtcOffset, CultureInfo.InvariantCulture)
                    : PclExport.Instance.ToXsdDateTimeString(dateTime);
        }

        if (!hasFractionalSecs)
            return dateTime.Kind != DateTimeKind.Utc
                ? dateTime.ToString(DateTimeFormatSecondsUtcOffset, CultureInfo.InvariantCulture)
                : dateTime.ToStableUniversalTime().ToString(XsdDateTimeFormatSeconds, CultureInfo.InvariantCulture);

        return dateTime.Kind != DateTimeKind.Utc
            ? dateTime.ToString(DateTimeFormatTicksUtcOffset, CultureInfo.InvariantCulture)
            : PclExport.Instance.ToXsdDateTimeString(dateTime);
    }

    static readonly char[] TimeZoneChars = { '+', '-' };

    private const string MinDateTimeOffsetWcfValue = "\\/Date(-62135596800000)\\/";
    private const string MaxDateTimeOffsetWcfValue = "\\/Date(253402300799999)\\/";

    /// <summary>
    /// WCF Json format: /Date(unixts+0000)/
    /// </summary>
    /// <param name="wcfJsonDate"></param>
    /// <returns></returns>
    public static DateTimeOffset ParseWcfJsonDateOffset(string wcfJsonDate)
    {
        if (wcfJsonDate == MinDateTimeOffsetWcfValue)
            return DateTimeOffset.MinValue;
        if (wcfJsonDate == MaxDateTimeOffsetWcfValue)
            return DateTimeOffset.MaxValue;
            
        if (wcfJsonDate[0] == '\\')
        {
            wcfJsonDate = wcfJsonDate.Substring(1);
        }

        var suffixPos = wcfJsonDate.IndexOf(WcfJsonSuffix);
        var timeString = (suffixPos < 0) ? wcfJsonDate : wcfJsonDate.Substring(WcfJsonPrefix.Length, suffixPos - WcfJsonPrefix.Length);

        // for interop, do not assume format based on config
        if (!wcfJsonDate.StartsWith(WcfJsonPrefix, StringComparison.Ordinal))
        {
            return DateTimeOffset.Parse(timeString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        var timeZonePos = timeString.LastIndexOfAny(TimeZoneChars);
        var timeZone = timeZonePos <= 0 ? string.Empty : timeString.Substring(timeZonePos);
        var unixTimeString = timeString.Substring(0, timeString.Length - timeZone.Length);

        var unixTime = long.Parse(unixTimeString);

        if (timeZone == string.Empty)
        {
            // when no timezone offset is supplied, then treat the time as UTC
            return unixTime.FromUnixTimeMs();
        }

        // DCJS ignores the offset and considers it local time if any offset exists
        // REVIEW: DCJS shoves offset in a separate field 'offsetMinutes', we have the offset in the format, so shouldn't we use it?
        if (JsConfig.DateHandler == DateHandler.DCJSCompatible || timeZone == UnspecifiedOffset)
        {
            return unixTime.FromUnixTimeMs().ToLocalTime();
        }

        var offset = timeZone.FromTimeOffsetString();
        var date = unixTime.FromUnixTimeMs();
        var dateWithOffset = new DateTimeOffset(date).ToOffset(offset);
        return dateWithOffset;
    }

    /// <summary>
    /// WCF Json format: /Date(unixts+0000)/
    /// </summary>
    /// <param name="wcfJsonDate"></param>
    /// <returns></returns>
    public static DateTime ParseWcfJsonDate(string wcfJsonDate)
    {
        if (wcfJsonDate[0] == JsonUtils.EscapeChar)
        {
            wcfJsonDate = wcfJsonDate.Substring(1);
        }

        var suffixPos = wcfJsonDate.IndexOf(WcfJsonSuffix);
        var timeString = wcfJsonDate.Substring(WcfJsonPrefix.Length, suffixPos - WcfJsonPrefix.Length);

        // for interop, do not assume format based on config
        if (!wcfJsonDate.StartsWith(WcfJsonPrefix, StringComparison.Ordinal))
        {
            return DateTime.Parse(timeString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        var timeZonePos = timeString.LastIndexOfAny(TimeZoneChars);
        var timeZone = timeZonePos <= 0 ? string.Empty : timeString.Substring(timeZonePos);
        var unixTimeString = timeString.Substring(0, timeString.Length - timeZone.Length);

        var unixTime = long.Parse(unixTimeString);

        if (timeZone == string.Empty)
        {
            // when no timezone offset is supplied, then treat the time as UTC
            return unixTime.FromUnixTimeMs();
        }

        // DCJS ignores the offset and considers it local time if any offset exists
        if (JsConfig.DateHandler == DateHandler.DCJSCompatible || timeZone == UnspecifiedOffset)
        {
            return unixTime.FromUnixTimeMs().ToLocalTime();
        }

        var offset = timeZone.FromTimeOffsetString();
        var date = unixTime.FromUnixTimeMs(offset);
        return date;
    }

    public static TimeZoneInfo GetLocalTimeZoneInfo()
    {
        try
        {
            return TimeZoneInfo.Local;
        }
        catch (Exception)
        {
            return TimeZoneInfo.Utc; //Fallback for Mono on Windows.
        }
    }

    internal static TimeZoneInfo LocalTimeZone = GetLocalTimeZoneInfo();

    private static DateTime UseConfigSpecifiedSetting(this DateTime dateTime)
    {
        if (JsConfig.AssumeUtc && dateTime.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
        return dateTime;
    }

    public static void WriteWcfJsonDate(TextWriter writer, DateTime dateTime)
    {
        var config = JsConfig.GetConfig();
            
        dateTime = dateTime.UseConfigSpecifiedSetting();
        switch (config.DateHandler)
        {
            case DateHandler.ISO8601:
                writer.Write(dateTime.ToString("o", CultureInfo.InvariantCulture));
                return;
            case DateHandler.ISO8601DateOnly:
                writer.Write(dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                return;
            case DateHandler.ISO8601DateTime:
                writer.Write(dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                return;
            case DateHandler.RFC1123:
                writer.Write(dateTime.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture));
                return;
        }

        var timestamp = dateTime.ToUnixTimeMs();
        string offset = null;
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            if (config.DateHandler == DateHandler.TimestampOffset && dateTime.Kind == DateTimeKind.Unspecified)
                offset = UnspecifiedOffset;
            else
                offset = LocalTimeZone.GetUtcOffset(dateTime).ToTimeOffsetString();
        }
        else
        {
            // Normally the JsonDateHandler.TimestampOffset doesn't append an offset for Utc dates, but if
            // the config.AppendUtcOffset is set then we will
            if (config.DateHandler == DateHandler.TimestampOffset && config.AppendUtcOffset)
                offset = UtcOffset;
        }

        writer.Write(EscapedWcfJsonPrefix);
        writer.Write(timestamp);
        if (offset != null)
        {
            writer.Write(offset);
        }
        writer.Write(EscapedWcfJsonSuffix);
    }

    public static string ToWcfJsonDate(DateTime dateTime)
    {
        var sb = StringBuilderThreadStatic.Allocate();
        using var writer = new StringWriter(sb);
        WriteWcfJsonDate(writer, dateTime);
        return StringBuilderThreadStatic.ReturnAndFree(sb);
    }
        
    public static void WriteWcfJsonDateTimeOffset(TextWriter writer, DateTimeOffset dateTimeOffset)
    {
        if (JsConfig.DateHandler == DateHandler.ISO8601)
        {
            writer.Write(dateTimeOffset.ToString("o", CultureInfo.InvariantCulture));
            return;
        }

        var offset = dateTimeOffset.Offset == TimeSpan.Zero
            ? null
            : dateTimeOffset.Offset.ToTimeOffsetString();

        var timestamp = dateTimeOffset.ToUniversalTime().Ticks.ToUnixTimeMs();
        writer.Write(EscapedWcfJsonPrefix);
        writer.Write(timestamp);
        if (offset != null)
        {
            writer.Write(offset);
        }
        writer.Write(EscapedWcfJsonSuffix);
    }

    public static string ToWcfJsonDateTimeOffset(DateTimeOffset dateTimeOffset)
    {
        var sb = StringBuilderThreadStatic.Allocate();
        using var writer = new StringWriter(sb);
        WriteWcfJsonDateTimeOffset(writer, dateTimeOffset);
        return StringBuilderThreadStatic.ReturnAndFree(sb);
    }
}