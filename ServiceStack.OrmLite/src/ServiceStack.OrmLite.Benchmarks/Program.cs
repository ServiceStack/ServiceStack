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
        // var summary = BenchmarkRunner.Run<BulkInserts>();
        var instance = new BulkInserts();
        instance.Setup();
        instance.IterationSetup();
        instance.BatchInsertsOptimized(10);
    }
}

/*
public class Md5VsSha256
{
    private const int N = 10000;
    private readonly byte[] data;

    private readonly SHA256 sha256 = SHA256.Create();
    private readonly MD5 md5 = MD5.Create();

    public Md5VsSha256()
    {
        data = new byte[N];
        new Random(42).NextBytes(data);
    }

    [Benchmark]
    public byte[] Sha256() => sha256.ComputeHash(data);

    [Benchmark]
    public byte[] Md5() => md5.ComputeHash(data);
}
*/

public enum Database
{
    Memory,
    Sqlite,
    PostgreSQL,
    MySql,
    SqlServer,
}

public class Contact
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
}

public class BulkInserts
{
    // [Params(Database.Memory, Database.Sqlite, Database.PostgreSQL, Database.MySql, Database.SqlServer)]
    [Params(Database.MySql)]
    public Database Database;

    IDbConnectionFactory dbFactory;

    [GlobalSetup]
    public void Setup()
    {
        dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);

        dbFactory.RegisterConnection($"{Database.Sqlite}", "db.sqlite", SqliteDialect.Provider);

        dbFactory.RegisterConnection($"{Database.PostgreSQL}", 
            "Server=localhost;User Id=postgres;Password=p@55wOrd;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200",
            PostgreSqlDialect.Provider);

        dbFactory.RegisterConnection($"{Database.MySql}", 
            "Server=localhost;User Id=root;Password=p@55wOrd;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200",
            MySqlDialect.Provider);

        dbFactory.RegisterConnection($"{Database.SqlServer}", 
            "Server=localhost;User Id=sa;Password=p@55wOrd;Database=test;MultipleActiveResultSets=True;Encrypt=False;",
            SqlServer2012Dialect.Provider);
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

    Contact CreateContact(int i) => 
        new Contact { Id = i + 1, FirstName = "First" + i, LastName = "Last" + i, Age = i % 100 };

    // [Benchmark]
    // [Arguments(100)]
    // [Arguments(1000)]
    public void SingleInserts(int n)
    {
        using var db = GetConnection(Database);
        for (var i = 0; i<n; i++)
        {
            db.Insert(CreateContact(i));
        }
    }

    // [Benchmark]
    // [Arguments(100)]
    // [Arguments(1000)]
    // [Arguments(10000)]
    // [Arguments(100000)]
    //[Arguments(1000000)]
    public void BatchInserts(int n)
    {
        using var db = GetConnection(Database);
        var contacts = n.Times(CreateContact);
        db.BulkInsert(contacts, new BulkInsertConfig { Mode = BulkInsertMode.Sql });
    }

    [Benchmark]
    // [Arguments(100)]
    [Arguments(1000)]
    // [Arguments(10000)]
    // [Arguments(100000)]
    // [Arguments(1000000)]
    public void BatchInsertsOptimized(int n)
    {
        using var db = GetConnection(Database);
        var contacts = n.Times(CreateContact);
        db.BulkInsert(contacts, new BulkInsertConfig { Mode = BulkInsertMode.Binary });
    }
}
