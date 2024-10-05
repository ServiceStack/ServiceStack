using System.Data;

namespace ServiceStack.OrmLite.Tests.Benchmarks;

public class TextContents
{
    [AutoIncrement] public int Id { get; set; }

    [StringLength(StringLengthAttribute.MaxText)]
    public string Contents { get; set; }
}

[MemoryDiagnoser]
[ShortRunJob]
public class LargeColumnBenchmark
{
    private OrmLiteConnectionFactory dbFactory;
    private int id;
    private int size = 1000000;
    private int iterations = 5;
    
    [ParamsSource(nameof(DatabaseNames))]
    public string DbName { get; set; }

    public IEnumerable<string> DatabaseNames => ["pgsql", "mssql", "mysql"];
    
    [GlobalSetup]
    public void Setup()
    {
        dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        dbFactory.RegisterConnection("pgsql", 
            "Server=localhost;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200",
            PostgreSqlDialect.Provider);
        dbFactory.RegisterConnection("mssql", 
            "Server=localhost;Database=test;User Id=sa;Password=p@55wOrd;MultipleActiveResultSets=True;TrustServerCertificate=True;",
            SqlServer2012Dialect.Provider);
        dbFactory.RegisterConnection("mysql", 
            "Server=localhost;Database=test;UID=root;Password=p@55wOrd;SslMode=none;AllowLoadLocalInfile=true;Convert Zero Datetime=True;AllowPublicKeyRetrieval=true;",
            MySqlDialect.Provider);

        var text = new string('x', size);
        foreach (var dbName in DatabaseNames)
        {
            using var db = dbFactory.OpenDbConnection(dbName);
            db.DropAndCreateTable<TextContents>();
            id = (int) db.Insert(new TextContents { Contents = text }, selectIdentity: true);
        }
    }

    [Benchmark]
    public void TextContents_Sync()
    {
        using var db = dbFactory.OpenDbConnection(DbName);
        for (var i = 0; i < iterations; i++)
        {
            var result = db.Single<TextContents>(x => x.Id == id);
            if (result.Contents.Length != size)
                Console.WriteLine($"ERROR: {result.Contents.Length} != {size}");
        }
    }

    [Benchmark]
    public async Task TextContents_Async()
    {
        using var db = await dbFactory.OpenDbConnectionAsync(DbName);
        for (var i = 0; i < iterations; i++)
        {
            var result = await db.SingleAsync<TextContents>(x => x.Id == id);
            if (result.Contents.Length != size)
                Console.WriteLine($"ERROR: {result.Contents.Length} != {size}");
        }
    }
}
