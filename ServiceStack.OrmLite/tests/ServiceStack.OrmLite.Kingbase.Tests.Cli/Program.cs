using System;
using System.Diagnostics;
using FluentAssertions;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Kingbase.Tests.Cli;

internal class Program
{
    static void Main(string[] args)
    {
        //OrmLiteConfig.DeoptimizeReader = true;
        var factory = new OrmLiteConnectionFactory(
            "User Id=kingbase;Password=Jnvision_2022_Kb;Server=192.168.110.231;Port=54321;Database=ormlite-test;",
            KingbaseDialect.MySql);

        var db = factory.OpenDbConnection();
        //常规语句测试
        db.Scalar<string>("select version()").PrintDump();
        //db.Select<string>("select UUID()").PrintDump();
        db.Scalar<string>("SELECT CONCAT_WS(',', 1, 2)").PrintDump();
        db.Scalar<string>("SELECT current_setting('TimeZone');").PrintDump();

        //表测试
        db.CreateTableIfNotExists<TestTable>();
        Debug.Assert(db.TableExists<TestTable>());
        var now = DateTime.Now;
        var insert = new TestTable()
        {
            CreatedAt = now,
            IsActive = true,
            Name = Guid.NewGuid().ToString(),
            UpdatedAt = now.AddDays(1)
        };
        var id = db.Insert(insert, selectIdentity: true);
        insert.Id = (int)id;
        var entity = db.SingleById<TestTable>(id);

        insert.Should().BeEquivalentTo(entity, options => options.Using<DateTime>(ctx =>
                ctx.Subject
                    .TrimToSeconds()
                    .Should()
                    .BeCloseTo(ctx.Expectation.TrimToSeconds(),TimeSpan.FromSeconds(3)))
            .WhenTypeIs<DateTime>());

        db.DeleteById<TestTable>(id);
        db.RowCount<TestTable>().Should().BeGreaterThan(0);

        Console.WriteLine(@"Done");
    }
}

public static class DateTimeExtensions
{
    public static DateTime TrimToSeconds(this DateTime dt) =>
        new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
}