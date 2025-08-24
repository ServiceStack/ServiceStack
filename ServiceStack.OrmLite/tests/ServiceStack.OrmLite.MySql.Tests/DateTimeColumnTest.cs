using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.OrmLite.Legacy;
using ServiceStack.OrmLite.MySql.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.MySql.Tests;

[TestFixture]
public class DateTimeColumnTest
    : OrmLiteTestBase
{
    [Test]
    public void Can_create_table_containing_DateTime_column()
    {
        using var db = OpenDbConnection();
        db.CreateTable<Analyze>(true);
    }

    [Test]
    public void Can_store_DateTime_Value()
    {
        using var db = OpenDbConnection();
        db.CreateTable<Analyze>(true);

        var obj = new Analyze {
            Id = 1,
            Date = DateTime.Now,
            Url = "http://www.google.com"
        };

        db.Save(obj);
    }

    [Test]
    public void Can_store_and_retrieve_DateTime_Value()
    {
        using var db = OpenDbConnection();
        db.CreateTable<Analyze>(true);

        var obj = new Analyze {
            Id = 1,
            Date = DateTime.Now,
            Url = "http://www.google.com"
        };

        db.Save(obj);

        var id = (int)db.LastInsertId();
        var target = db.SingleById<Analyze>(id);

        Assert.That(target, Is.Not.Null);
        Assert.That(target.Id, Is.EqualTo(id));
        Assert.That(target.Date, Is.EqualTo(obj.Date).Within(TimeSpan.FromSeconds(1)));
        Assert.That(target.Url, Is.EqualTo(obj.Url));
    }

    [Test]
    public void Can_change_DateTime_precision()
    {
        using var db = OpenDbConnection();
#if MYSQLCONNECTOR
            if (MySqlConnectorDialect.Provider.GetConverter(typeof(DateTime)) is MySqlDateTimeConverterBase dateConverter)
            {
                dateConverter.Precision = 3;
            }
#else
        if (MySqlDialect.Provider.GetConverter(typeof(DateTime)) is MySqlDateTimeConverterBase dateConverter)
        {
            dateConverter.Precision = 3;
        }
#endif

        OrmLiteUtils.PrintSql();
        db.CreateTable<Analyze>(true);

        var obj = new Analyze {
            Id = 1,
            Date = DateTime.Now,
            Url = "http://www.google.com"
        };

        db.Save(obj);

        var id = (int)db.LastInsertId();
        var target = db.SingleById<Analyze>(id);
        target.PrintDump();

        var q = db.From<Analyze>()
            .Where(x => x.Date >= DateTime.Now.Date);
        var results = db.Select(q);
        Assert.That(results.Count, Is.EqualTo(1));
        results.PrintDump();
            
        results = db.SelectFmt<Analyze>("AnalyzeDate >= {0}", DateTime.Now.AddDays(-1));
        results.PrintDump();
    }

    [Test]
    public void Can_query_DateTime_column_in_select_without_any_format_parameter()
    {
        using var db = OpenDbConnection();
        db.CreateTable<Analyze>(true);
        var obj = new Analyze
        {
            Id = 1,
            Date = DateTime.Now,
            Url = "http://www.google.com"
        };
        db.Save(obj);

        var q=db.From<Analyze>().Select(i => i.Date);
        var sql = q.ToMergedParamsSelectStatement();
        var val= db.Select<DateTime>(q).First();
        Assert.That(obj.Date,Is.EqualTo(val).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void Can_query_DateTime_column_in_select_with_format_parameter()
    {
        using var db = OpenDbConnection();
        db.CreateTable<Analyze>(true);
        var obj = new Analyze
        {
            Id = 1,
            Date = DateTime.Now,
            Url = "http://www.google.com"
        };
        db.Save(obj);
        var q = db.From<Analyze>().Select(i => i.Date.ToString("%Y-%m-%d %H:%i:%s"));
        var sql = q.ToMergedParamsSelectStatement();
        var val = db.Select<string>(q).First();
        Assert.That(obj.Date, Is.EqualTo(DateTime.Parse(val)).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void Can_query_DateTime_column_in_where_with_format_parameter()
    {
        using var db = OpenDbConnection();
        db.CreateTable<Analyze>(true);
        var obj = new Analyze
        {
            Id = 1,
            Date = DateTime.Now,
            Url = "http://www.google.com"
        };
        var yesterdayString = obj.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var todayString = obj.Date.ToString("yyyy-MM-dd");
        db.Save(obj);
        var q = db.From<Analyze>().Where(i=>i.Date.ToString("%Y-%m-%d")== yesterdayString);
        var results = db.Select<Analyze>(q);
        var sql = q.ToMergedParamsSelectStatement();
        Assert.That(results.Count, Is.EqualTo(0));

        q = db.From<Analyze>().Where(i => i.Date.ToString("%Y-%m-%d") == todayString);
        results = db.Select<Analyze>(q);
        sql = q.ToMergedParamsSelectStatement();
        Assert.That(results.Count, Is.EqualTo(1));

        var id = (int)db.LastInsertId();
        var target = db.SingleById<Analyze>(id);
        Assert.That(target, Is.Not.Null);
        Assert.That(target.Id, Is.EqualTo(id));
        Assert.That(target.Date, Is.EqualTo(obj.Date).Within(TimeSpan.FromSeconds(1)));
        Assert.That(target.Url, Is.EqualTo(obj.Url));
    }

    [Test]
    public void Can_query_DateTime_column_in_select_and_where_with_format_parameter()
    {
        using var db = OpenDbConnection();
        db.CreateTable<Analyze>(true);
        var obj = new Analyze
        {
            Id = 1,
            Date = DateTime.Now,
            Url = "http://www.google.com"
        };
        var yesterdayString = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        var todayString = DateTime.Now.ToString("yyyy-MM-dd");
        db.Save(obj);
        var q = db.From<Analyze>().Where(i => i.Date.ToString("%Y-%m-%d") == yesterdayString).Select(i=>i.Date.ToString("%Y-%m-%d"));
        var results = db.Select<string>(q);
        var sql = q.ToMergedParamsSelectStatement();
        Assert.That(results.Count, Is.EqualTo(0));

        q = db.From<Analyze>().Where(i => i.Date.ToString() == yesterdayString).Select(i => i.Date.ToString("%Y-%m-%d"));
        results = db.Select<string>(q);
        sql = q.ToMergedParamsSelectStatement();
        Assert.That(results.Count, Is.EqualTo(0));

        q = db.From<Analyze>().Where(i => i.Date.ToString("%Y-%m-%d") == todayString).Select(i => i.Date.ToString("%Y-%m-%d"));
        results = db.Select<string>(q);
        sql = q.ToMergedParamsSelectStatement();
        Assert.That(results.Count, Is.EqualTo(1));

        var target = results.First();
        Assert.That(target, Is.Not.Null);
        Assert.That(target, Is.EqualTo(todayString));
    }

    /// <summary>
    /// Provided by RyogoNA in issue #38 https://github.com/ServiceStack/ServiceStack.OrmLite/issues/38#issuecomment-4625178
    /// </summary>
    [Alias("Analyzes")]
    public class Analyze : IHasId<int>
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id
        {
            get;
            set;
        }
        [Alias("AnalyzeDate")]
        public DateTime Date
        {
            get;
            set;
        }
        public string Url
        {
            get;
            set;
        }
    }
}