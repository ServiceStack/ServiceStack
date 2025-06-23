using System;
using System.Diagnostics;
using FluentAssertions;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Kingbase.Tests.Cli;

internal class Program
{
    static void Main(string[] args)
    {
        OrmLiteConfig.DeoptimizeReader = true;
        var factory = new OrmLiteConnectionFactory(
            "User Id=kingbase;Password=Jnvision_2022_Kb;Server=192.168.110.231;Port=54321;Database=ormlite-test;",
            KingbaseDialect.InstanceForMySql);

        var db = factory.OpenDbConnection();
        //常规语句测试
        db.Scalar<string>("select version()").PrintDump();
        //db.Select<string>("select UUID()").PrintDump();
        db.Scalar<string>("SELECT CONCAT_WS(',', 1, 2)").PrintDump();

        //表测试
        db.CreateTableIfNotExists<TestTable>();
        Debug.Assert(db.TableExists<TestTable>());
        var insert = new TestTable()
        {
            CreatedAt = DateTime.Now,
            IsActive = true,
            Name = Guid.NewGuid().ToString(),
            UpdatedAt = DateTime.Now.AddDays(1)
        };
        var id = db.Insert(insert, selectIdentity: true);
        var entity = db.Single<TestTable>(id);

        insert.Should().Be(entity);

        db.Delete<TestTable>(id);
        db.RowCount<TestTable>().Should().Be(0);

        Console.WriteLine(@"Done");
    }
}