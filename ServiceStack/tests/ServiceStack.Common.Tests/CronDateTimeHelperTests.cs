#if NET6_0_OR_GREATER
using System;
using System.Globalization;
using NUnit.Framework;
using ServiceStack.Cronos;

namespace ServiceStack.Common.Tests;

public class CronDateTimeHelperTests
{
    [TestCase("2017-03-30 23:59:59.0000000 +02:00", "2017-03-30 23:59:59.0000000 +02:00")]
    [TestCase("2017-03-30 23:59:59.9000000 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
    [TestCase("2017-03-30 23:59:59.9900000 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
    [TestCase("2017-03-30 23:59:59.9990000 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
    [TestCase("2017-03-30 23:59:59.9999000 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
    [TestCase("2017-03-30 23:59:59.9999900 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
    [TestCase("2017-03-30 23:59:59.9999990 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
    [TestCase("2017-03-30 23:59:59.9999999 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
    [TestCase("2017-03-30 23:59:59.0000001 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
    [TestCase("2017-03-30 23:59:59.0000010 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
    [TestCase("2017-03-30 23:59:59.0000100 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
    [TestCase("2017-03-30 23:59:59.0001000 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
    [TestCase("2017-03-30 23:59:59.0010000 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
    [TestCase("2017-03-30 23:59:59.0100000 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
    [TestCase("2017-03-30 23:59:59.1000000 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
    public void FloorToSeconds_WorksCorrectlyWithDateTimeOffset(string dateTime, string expected)
    {
        var dateTimeOffset = GetDateTimeOffsetInstant(dateTime);
        var expectedDateTimeOffset = GetDateTimeOffsetInstant(expected);

        var flooredDateTimeOffset = DateTimeHelper.FloorToSeconds(dateTimeOffset);

        Assert.AreEqual(expectedDateTimeOffset, flooredDateTimeOffset);
        Assert.AreEqual(expectedDateTimeOffset.Offset, flooredDateTimeOffset.Offset);
    }

    private static DateTimeOffset GetDateTimeOffsetInstant(string dateTimeOffsetString)
    {
        dateTimeOffsetString = dateTimeOffsetString.Trim();

        var dateTime = DateTimeOffset.ParseExact(
            dateTimeOffsetString,
            new[]
            {
                "yyyy-MM-dd HH:mm:ss.fffffff zzz",
            },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);

        return dateTime;
    }

    private static DateTime GetDateTimeInstant(string dateTimeString, DateTimeKind kind)
    {
        dateTimeString = dateTimeString.Trim();

        var dateTime = DateTime.ParseExact(
            dateTimeString,
            new[]
            {
                "yyyy-MM-dd HH:mm:ss.fffffff",
            },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);

        dateTime = DateTime.SpecifyKind(dateTime, kind);

        return dateTime;
    }    
}
#endif