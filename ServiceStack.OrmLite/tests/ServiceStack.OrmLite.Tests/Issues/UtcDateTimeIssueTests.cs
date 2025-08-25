using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

public class TestDate
{
    public string Name { get; set; }
    public DateTime ExpiryDate { get; set; }
}

[TestFixtureOrmLite]
public class UtcDateTimeIssueTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Test_DateTime_Select()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TestDate>();

        DateTime.UtcNow.ToJson().Print();

        db.Insert(new TestDate {
            Name = "Test name", 
            ExpiryDate = DateTime.UtcNow.AddHours(1)
        });

        //db.GetLastSql().Print();

        var result = db.Select<TestDate>(q => q.ExpiryDate > DateTime.UtcNow);
        db.GetLastSql().Print();

        Assert.That(result.Count, Is.EqualTo(1));

        //db.Select<TestDate>(q => q.ExpiryDate > DateTime.Now);
        //db.GetLastSql().Print();

        //db.Select<TestDate>().PrintDump();
    }

    [Test]
    public void Can_select_DateTime_by_Range()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TestDate>();

        db.Insert(new TestDate {
            Name = "Now",
            ExpiryDate = DateTime.Now,
        });
        db.Insert(new TestDate {
            Name = "Today",
            ExpiryDate = DateTime.Now.Date,
        });
        db.Insert(new TestDate {
            Name = "Tomorrow",
            ExpiryDate = DateTime.Now.Date.AddDays(1),
        });

        var results = db.Select<TestDate>()
            .Where(x => x.ExpiryDate >= DateTime.Now.Date && 
                        x.ExpiryDate < DateTime.Now.Date.AddDays(1));
                
        Assert.That(results.Map(x => x.Name), Is.EquivalentTo(new[]{"Now","Today"}));
    }

    [Test]
    public void Can_Select_DateTime_with_SelectFmt()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TestDate>();

        db.Insert(new TestDate
        {
            Name = "1999",
            ExpiryDate = new DateTime(1999, 01, 01)
        });
        db.Insert(new TestDate
        {
            Name = "2000",
            ExpiryDate = new DateTime(2000, 01, 01)
        });
        db.Insert(new TestDate
        {
            Name = "Test name",
            ExpiryDate = DateTime.UtcNow.AddHours(1)
        });

        var result = db.Select<TestDate>("ExpiryDate".SqlColumn(DialectProvider) + " > @theDate".PreNormalizeSql(db),
            new { theDate = DateTime.UtcNow });
        db.GetLastSql().Print();
        Assert.That(result.Count, Is.EqualTo(1));

        result = db.Select<TestDate>("ExpiryDate".SqlColumn(DialectProvider) + " > @theDate".PreNormalizeSql(db),
            new { theDate = new DateTime(1999, 01, 02) });
        db.GetLastSql().Print();
        Assert.That(result.Count, Is.EqualTo(2));
    }
}