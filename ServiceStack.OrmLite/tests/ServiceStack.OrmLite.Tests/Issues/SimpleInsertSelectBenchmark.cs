using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

public class TableWithStrings
{
    [AutoIncrement]
    public int Id { get; set; }
    public string String01 { get; set; }
    public string String02 { get; set; }
    public string String03 { get; set; }
    public string String04 { get; set; }
    public string String05 { get; set; }
    public string String06 { get; set; }
    public string String07 { get; set; }
    public string String08 { get; set; }
    public string String09 { get; set; }
    public string String10 { get; set; }
    public string String11 { get; set; }
    public string String12 { get; set; }
    public string String13 { get; set; }
    public string String14 { get; set; }
    public string String15 { get; set; }
    public string String16 { get; set; }
    public string String17 { get; set; }
    public string String18 { get; set; }
    public string String19 { get; set; }
    public string String20 { get; set; }

    public static TableWithStrings Create(int i)
    {
        return new TableWithStrings
        {
            String01 = "String: " + i,
            String02 = "String: " + i,
            String03 = "String: " + i,
            String04 = "String: " + i,
            String05 = "String: " + i,
            String06 = "String: " + i,
            String07 = "String: " + i,
            String08 = "String: " + i,
            String09 = "String: " + i,
            String10 = "String: " + i,
            String11 = "String: " + i,
            String12 = "String: " + i,
            String13 = "String: " + i,
            String14 = "String: " + i,
            String15 = "String: " + i,
            String16 = "String: " + i,
            String17 = "String: " + i,
            String18 = "String: " + i,
            String19 = "String: " + i,
            String20 = "String: " + i,
        };
    }
}

[NUnit.Framework.Ignore("Benchmark"), TestFixture]
[Category("Benchmark")]
public class SimpleInsertSelectBenchmark
{
    [Test]
    public void Simple_Perf_test_using_InMemory_Sqlite()
    {
        var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        using var db = dbFactory.Open();
        db.DropAndCreateTable<TableWithStrings>();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            var row = TableWithStrings.Create(i);
            db.Insert(row);
        }
        "[:memory:] Time to INSERT 100 rows: {0}ms".Print(sw.ElapsedMilliseconds);

        sw = Stopwatch.StartNew();
        var rows = db.Select<TableWithStrings>();
        "[:memory:] Time to SELECT {0} rows: {1}ms".Print(rows.Count, sw.ElapsedMilliseconds);
    }

    [Test]
    public void Simple_Perf_test_using_File_Sqlite()
    {
        var dbPath = "~/App_Data/db.sqlite".MapProjectPath();
        var dbFactory = new OrmLiteConnectionFactory(dbPath, SqliteDialect.Provider);
        using var db = dbFactory.Open();
        db.DropAndCreateTable<TableWithStrings>();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            var row = TableWithStrings.Create(i);
            db.Insert(row);
        }
        "[db.sqlite] Time to INSERT 100 rows: {0}ms".Print(sw.ElapsedMilliseconds);

        sw = Stopwatch.StartNew();
        var rows = db.Select<TableWithStrings>();
        "[db.sqlite] Time to SELECT {0} rows: {1}ms".Print(rows.Count, sw.ElapsedMilliseconds);
    }
}