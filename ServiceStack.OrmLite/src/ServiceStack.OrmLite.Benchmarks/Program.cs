using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ServiceStack;
using ServiceStack.Text;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using System.Data;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<BulkInserts>();
        // var instance = new BulkInserts {
        //     Database = Database.SqlServer
        // };
        // instance.Setup();
        // instance.IterationSetup();
        // instance.BatchInsertsOptimized(10);
    }
}

public enum Database
{
    Memory,
    Sqlite,
    PostgreSQL,
    MySql,
    MySqlConnector,
    SqlServer,
}

public class Contact
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
}

[CsvMeasurementsExporter]
[RPlotExporter]
public class BulkInserts
{
    [Params(Database.Memory, Database.Sqlite, Database.PostgreSQL, Database.MySql, Database.MySqlConnector, Database.SqlServer)]
    // [Params(Database.SqlServer)]
    public Database Database;

    IDbConnectionFactory dbFactory;
    const int MaxContacts = 100000;
    private Contact[] Contacts; 

    [GlobalSetup]
    public void Setup()
    {
        dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);

        dbFactory.RegisterConnection($"{Database.Sqlite}", "db.sqlite", SqliteDialect.Provider);

        dbFactory.RegisterConnection($"{Database.PostgreSQL}", Environment.GetEnvironmentVariable("PGSQL_CONNECTION") ??
            "Server=localhost;User Id=postgres;Password=p@55wOrd;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200",
            PostgreSqlDialect.Provider);

        dbFactory.RegisterConnection($"{Database.MySql}", Environment.GetEnvironmentVariable("MYSQL_CONNECTION") ??
            "Server=localhost;User Id=root;Password=p@55wOrd;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200;AllowLoadLocalInfile=true",
            MySqlDialect.Provider);
        MySqlDialect.Instance.AllowLoadLocalInfile = true;

        dbFactory.RegisterConnection($"{Database.MySqlConnector}", Environment.GetEnvironmentVariable("MYSQL_CONNECTION") ??
            "Server=localhost;User Id=root;Password=p@55wOrd;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200;AllowLoadLocalInfile=true",
            MySqlConnectorDialect.Provider);

        dbFactory.RegisterConnection($"{Database.SqlServer}", Environment.GetEnvironmentVariable("MSSQL_CONNECTION") ??
            "Server=localhost;User Id=sa;Password=p@55wOrd;Database=test;MultipleActiveResultSets=True;Encrypt=False;TrustServerCertificate=True;",
            SqlServer2012Dialect.Provider);

        Contacts = new Contact[MaxContacts];
        for (var i = 0; i < MaxContacts; i++)
        {
            Contacts[i] = CreateContact(i);
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        void CreateContact(IDbConnection db)
        {
            using (db) {
                db.DropAndCreateTable<Contact>();
            }
        }
        CreateContact(GetConnection(Database.Memory));
        CreateContact(GetConnection(Database.Sqlite));
        CreateContact(GetConnection(Database.PostgreSQL));
        CreateContact(GetConnection(Database.MySql));
        CreateContact(GetConnection(Database.SqlServer));
    }

    IDbConnection GetConnection(Database database) => database switch {
        Database.Memory => dbFactory.OpenDbConnection(),
        _ => dbFactory.OpenDbConnection($"{database}"),
    };

    Contact CreateContact(int i) => new() { Id = i + 1, FirstName = "First" + i, LastName = "Last" + i, Age = i % 100 };

    // [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    public void SingleInserts(int n)
    {
        using var db = GetConnection(Database);
        for (var i = 0; i<n; i++)
        {
            db.Insert(Contacts[i]);
        }
    }

    [Benchmark]
    [Arguments(1000)]
    [Arguments(10000)]
    [Arguments(100000)]
    [Arguments(1000000)]
    public void BatchInserts(int n)
    {
        using var db = GetConnection(Database);
        var contacts = Contacts.Take(n);
        db.BulkInsert(contacts, new BulkInsertConfig { Mode = BulkInsertMode.Sql });
    }

    // [Benchmark]
    [Arguments(1000)]
    [Arguments(10000)]
    [Arguments(100000)]
    [Arguments(1000000)]
    public void BatchInsertsOptimized(int n)
    {
        using var db = GetConnection(Database);
        var contacts = Contacts.Take(n);
        db.BulkInsert(contacts, new BulkInsertConfig { Mode = BulkInsertMode.Optimized });
    }
}
