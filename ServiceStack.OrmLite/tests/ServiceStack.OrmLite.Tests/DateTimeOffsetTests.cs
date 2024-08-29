using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class DateTimeOffsetTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    private IDbConnection db;

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        db = base.OpenDbConnection();
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        db.Dispose();
    }

    /// <summary>
    /// Generic way to create our test tables.
    /// </summary>
    /// <typeparam name="TTable"></typeparam>
    /// <typeparam name="TProp"></typeparam>
    /// <param name="db"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private static TTable InsertAndSelectDateTimeOffset<TTable, TProp>(IDbConnection db, TProp value) where TTable : IDateTimeOffsetObject<TProp>, new()
    {
        db.DropAndCreateTable<TTable>();
        db.Insert(new TTable
        {
            Test = value
        });
        var result = db.Select<TTable>().First();
        return result;
    }

    [Test]
    public void EnsureDateTimeOffsetSaves()
    {
        var dateTime = new DateTimeOffset(2012, 1, 30, 1, 1, 1, new TimeSpan(5, 0, 0));
        var x = InsertAndSelectDateTimeOffset<DateTimeOffsetObject, DateTimeOffset>(db, dateTime);
        Assert.That(x.Test, Is.EqualTo(dateTime));
    }

    [Test]
    public void EnsureNullableDateTimeOffsetSaves()
    {
        DateTimeOffset? dateTime = new DateTimeOffset(2012, 1, 30, 1, 1, 1, new TimeSpan(5, 0, 0));
        var x = InsertAndSelectDateTimeOffset<NullableDateTimeOffsetObject, DateTimeOffset?>(db, dateTime);
        Assert.That(x.Test, Is.EqualTo(dateTime));
    }

    private class DateTimeOffsetObject : IDateTimeOffsetObject<DateTimeOffset>
    {
        public int Id { get; set; }
        public DateTimeOffset Test { get; set; }
    }

    private class NullableDateTimeOffsetObject : IDateTimeOffsetObject<DateTimeOffset?>
    {
        public int Id { get; set; }
        public DateTimeOffset? Test { get; set; }
    }

    private interface IDateTimeOffsetObject<T>
    {
        int Id { get; set; }
        T Test { get; set; }
    }


    public class HasDateTimeOffsetMember
    {
        public int Id { get; set; }
        public DateTimeOffset MomentInTime { get; set; }
    }

    public class HasNullableDateTimeOffsetMember
    {
        public int Id { get; set; }
        public DateTimeOffset? MomentInTime { get; set; }
    }

    [Test]
    public void CanPersistAndRetrieveDateTimeOffset()
    {
        var now = DateTimeOffset.Now;

        db.DropAndCreateTable<HasDateTimeOffsetMember>();
        db.Insert(new HasDateTimeOffsetMember { MomentInTime = now });

        List<HasDateTimeOffsetMember> list = db.Select<HasDateTimeOffsetMember>();

        Assert.That(list.Count == 1);
        var actual = list.First().MomentInTime;
        Assert.That(actual.Date, Is.EqualTo(now.Date));
    }

    [Test]
    public void CanPersistAndRetrieveNullableDateTimeOffset()
    {
        var now = DateTimeOffset.Now;

        db.DropAndCreateTable<HasNullableDateTimeOffsetMember>();
        db.Insert(new HasNullableDateTimeOffsetMember { MomentInTime = now });

        List<HasNullableDateTimeOffsetMember> list = db.Select<HasNullableDateTimeOffsetMember>();

        Assert.That(list.Count == 1);
        var actual = list.First().MomentInTime;
        Assert.That(actual.HasValue);
        Assert.That(actual.Value.Date, Is.EqualTo(now.Date));
    }

    [TestCase("2012-08-12 -08:00")]
    [TestCase("2012-08-12 0:10:35 +12:00")]
    [TestCase("2012-08-12 23:10:35.288546 -08:00")]
    public void CanQueryDateTimeOffsetWithWhereExpression(string startTimeString)
    {
        var time = DateTimeOffset.Parse(startTimeString);
        db.DropAndCreateTable<NullableDateTimeOffsetWithStartAndEndTime>();
        db.Insert(new NullableDateTimeOffsetWithStartAndEndTime { Id = 1, StartTime = time.AddHours(-1), EndTime = time });
        var expression = db.From<NullableDateTimeOffsetWithStartAndEndTime>()
            .Where(p => p.StartTime == null || p.StartTime < time);

        var result = db.LoadSelect(expression).FirstOrDefault();
        var diff = time - result.EndTime;

        //MySql doesn't support ms, SqlServer has +/- .03 precision
        Assert.That(diff.Value, 
            Is.LessThan(TimeSpan.FromSeconds(1)).
                Or.GreaterThanOrEqualTo(TimeSpan.FromSeconds(0)));
    }

    [TestCase("2012-08-12")]
    [TestCase("2012-08-12 0:10:35")]
    [TestCase("2012-08-12 23:10:35.288546")]
    public void CanQueryDateTimeWithWhereExpression(string startTimeString)
    {
        var time = DateTime.Parse(startTimeString);
        db.DropAndCreateTable<NullableDateTimeWithStartAndEndTime>();
        db.Insert(new NullableDateTimeWithStartAndEndTime { Id = 1, StartTime = time.AddHours(-1), EndTime = time });
        var expression = db.From<NullableDateTimeWithStartAndEndTime>()
            .Where(p => p.StartTime == null || p.StartTime < time);

        var result = db.LoadSelect(expression).First();
        Assert.That(result.EndTime, Is.EqualTo(time).Within(TimeSpan.FromSeconds(1)));
    }

    public class NullableDateTimeOffsetWithStartAndEndTime
    {
        public int Id { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
    }

    public class NullableDateTimeWithStartAndEndTime
    {
        public int Id { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}