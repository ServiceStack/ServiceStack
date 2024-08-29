using System;
using System.Data;
using System.Linq;
using NUnit.Framework;

#if NET6_0_OR_GREATER
namespace ServiceStack.OrmLite.Tests;

public class Net6TypeTests : OrmLiteTestBase
{
    private class DateOnlyObject
    {
        public DateOnly Test { get; set; }
        public DateOnly? TestNullable { get; set; }
    }

    private static DateOnlyObject InsertAndSelectDateOnly(IDbConnection db, DateOnly dateTime)
    {
        db.DeleteAll<DateOnlyObject>();
        db.Insert(new DateOnlyObject { Test = dateTime, TestNullable = dateTime });
        var x = db.Select<DateOnlyObject>().First();
        return x;
    }

    [Test]
    public void Can_insert_and_query_DateOnly()
    {
        // OrmLiteUtils.PrintSql();
        using var db = OpenDbConnection();
        db.DropAndCreateTable<DateOnlyObject>();

        DateOnly dateTime;
        DateOnlyObject x;

        dateTime = new DateOnly(2013, 1, 13);
        x = InsertAndSelectDateOnly(db, dateTime);
        Assert.That(x.Test, Is.EqualTo(dateTime));
        Assert.That(x.TestNullable!.Value, Is.EqualTo(x.Test));
        x = db.Select<DateOnlyObject>(d => d.Test == dateTime).FirstOrDefault();
        Assert.That(x, Is.Not.Null);
    }

    private class TimeOnlyObject
    {
        public TimeOnly Test { get; set; }
        public TimeOnly? TestNullable { get; set; }
    }

    private static TimeOnlyObject InsertAndSelectTimeOnly(IDbConnection db, TimeOnly timeOnly)
    {
        db.DeleteAll<TimeOnlyObject>();
        db.Insert(new TimeOnlyObject { Test = timeOnly, TestNullable = timeOnly });
        var x = db.Select<TimeOnlyObject>().First();
        return x;
    }

    [Test]
    public void Can_insert_and_query_TimeOnly()
    {
        OrmLiteUtils.PrintSql();
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TimeOnlyObject>();

        TimeOnly timeOnly;
        TimeOnlyObject x;

        timeOnly = new TimeOnly(13, 14, 15);
        x = InsertAndSelectTimeOnly(db, timeOnly);
        Assert.That(x.Test, Is.EqualTo(timeOnly));
        Assert.That(x.TestNullable!.Value, Is.EqualTo(x.Test));
        x = db.Select<TimeOnlyObject>(d => d.Test == timeOnly).FirstOrDefault();
        Assert.That(x, Is.Not.Null);
    }
}
#endif