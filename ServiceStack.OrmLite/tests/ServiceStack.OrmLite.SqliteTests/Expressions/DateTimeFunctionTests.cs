using System;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Expressions;

namespace ServiceStack.OrmLite.SqliteTests.Expressions;

public class DateTimeFunctionTests:ExpressionsTestBase
{
    [Test]
    public void Can_query_DateTime_column_in_select_with_format_parameter()
    {
        var now = DateTime.Now;
        var nowString = now.ToString("yyyy-MM-dd HH:mm:ss");
        var expected = new TestType()
        {
            DateTimeColumn = DateTime.Now
        };

        EstablishContext(1, expected);
        using var db = OpenDbConnection();
        var q=db.From<TestType>().Where(x => x.DateTimeColumn.ToString("%Y-%m-%d %H:%M:%S") == nowString);
        var sql = q.ToMergedParamsSelectStatement();
        var actual = db.Single(q);
        Assert.IsNotNull(actual);

        Assert.AreEqual(expected,actual);
    }
}