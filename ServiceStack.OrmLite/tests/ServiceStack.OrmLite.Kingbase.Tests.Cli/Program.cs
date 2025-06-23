using System;
using System.Diagnostics;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Kingbase.Tests.Cli;

internal class Program
{
    static void Main(string[] args)
    {
        var factory = new OrmLiteConnectionFactory(
            "User ID=kingbase;Password=Jnvision_2022_Kb;Host=192.168.110.231;Port=54321;Database=ormlite-test;",
            KingbaseDialect.InstanceForMySqlConnector);

        var db = factory.OpenDbConnection();
        //常规语句测试
        db.Select<string>("select version()").PrintDump();
        //db.Select<string>("select UUID()").PrintDump();
        db.Select<string>("SELECT CONCAT_WS(',', 1, 2)").PrintDump();

        //表测试
        db.CreateTableIfNotExists<TestTable>();
        Debug.Assert(db.TableExists<TestTable>());
        Console.WriteLine(@"Done");
    }
}