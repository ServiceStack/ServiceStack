using System;
using NUnit.Framework;

namespace ServiceStack.Text.Tests;

public class DateTimeExtensionsTests
{
    [Test]
    public void LastMondayTest()
    {
        var monday = new DateTime(2013, 04, 15);

        var lastMonday = monday.LastMonday();
        Assert.That(monday, Is.EqualTo(lastMonday));
    }

    [Test]
    public void Does_display_Human_readable_TimeSpan()
    {
        Assert.That(new TimeSpan(1,2,3,4).Humanize(), Is.EqualTo("1 day, 2 hours, 3 minutes, 4 seconds"));
        Assert.That(new TimeSpan(2,3,4).Humanize(), Is.EqualTo("2 hours, 3 minutes, 4 seconds"));
        Assert.That(new TimeSpan(0,3,4).Humanize(), Is.EqualTo("3 minutes, 4 seconds"));
        Assert.That(new TimeSpan(0,0,4).Humanize(), Is.EqualTo("4 seconds"));
        Assert.That(new TimeSpan(0,0,1).Humanize(), Is.EqualTo("1 second"));
    }

}