using System.Data;
using System.Data.Common;
using BenchmarkDotNet.Configs;

namespace ServiceStack.OrmLite.Tests.Benchmarks;

public class BatchRow
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public decimal Total { get; set; }
}

/// <summary>
/// The time to write rows with InsertAll() and UpdateAll() when they send a statement for each row (UseDbBatch
/// disabled), vs sending them together with an ADO.NET DbBatch. Commands and DbBatch use ADO.NET directly, to
/// compare the round trips on their own.
/// <para>dotnet run -c Release -- --filter *DbBatch*</para>
/// </summary>
[SimpleJob(warmupCount: 3, iterationCount: 15)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DbBatchBenchmark
{
    [Params("pgsql", "mssql", "mysql")]
    public string Database { get; set; }

    [Params(10, 100, 1000)]
    public int Rows { get; set; }

    private IDbConnection db;
    private DbConnection dbConn;
    private List<BatchRow> existing;
    private List<BatchRow> added;
    private string insertSql;
    private string updateSql;

    static string Env(string name, string defaultValue) => Environment.GetEnvironmentVariable(name) ?? defaultValue;

    [GlobalSetup]
    public void Setup()
    {
        var dbFactory = Database switch {
            "pgsql" => new OrmLiteConnectionFactory(Env("PGSQL_CONNECTION",
                "Server=localhost;User Id=test;Password=p@55wOrd;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200"),
                PostgreSqlDialect.Provider),
            "mssql" => new OrmLiteConnectionFactory(Env("MSSQL_CONNECTION",
                "Server=localhost;Database=test;User Id=sa;Password=p@55wOrd;MultipleActiveResultSets=True;TrustServerCertificate=True;"),
                SqlServer2022Dialect.Provider),
            _ => new OrmLiteConnectionFactory(Env("MYSQL_CONNECTION",
                "Server=localhost;Database=test;UID=root;Password=p@55wOrd;SslMode=none;AllowPublicKeyRetrieval=true;"),
                MySqlConnectorDialect.Provider),
        };
        db = dbFactory.OpenDbConnection();
        dbConn = (DbConnection)db.ToDbConnection();
        if (!dbConn.CanCreateBatch)
            throw new NotSupportedException($"{dbConn.GetType().Name} doesn't support DbBatch");

        BatchRow Row(int id) => new() {
            Id = id, Name = "Name " + id, Email = $"user{id}@example.org", Age = 20 + id % 50, Total = id * 1.5m };

        existing = Enumerable.Range(1, Rows).Map(Row);
        added = Enumerable.Range(Rows + 1, Rows).Map(Row);
        db.DropAndCreateTable<BatchRow>();
        db.InsertAll(existing);

        var dialect = db.GetDialectProvider();
        var table = dialect.GetQuotedTableName(typeof(BatchRow));
        string Q(string column) => dialect.GetQuotedColumnName(column);
        insertSql = $"INSERT INTO {table} ({Q("Id")},{Q("Name")},{Q("Email")},{Q("Age")},{Q("Total")}) " +
                    "VALUES (@Id,@Name,@Email,@Age,@Total)";
        updateSql = $"UPDATE {table} SET {Q("Name")}=@Name, {Q("Email")}=@Email, {Q("Age")}=@Age, {Q("Total")}=@Total " +
                    $"WHERE {Q("Id")}=@Id";
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        db.DropTable<BatchRow>();
        db.Dispose();
    }

    static readonly (string name, DbType type, Func<BatchRow, object> get)[] Fields = [
        ("@Id", DbType.Int32, x => x.Id),
        ("@Name", DbType.String, x => x.Name),
        ("@Email", DbType.String, x => x.Email),
        ("@Age", DbType.Int32, x => x.Age),
        ("@Total", DbType.Decimal, x => x.Total),
    ];

    // One command that's run for each row, with a round trip for each
    private int ExecCommands(string sql, List<BatchRow> rows, bool commit)
    {
        using var trans = dbConn.BeginTransaction();
        using var cmd = dbConn.CreateCommand();
        cmd.Transaction = trans;
        cmd.CommandText = sql;
        foreach (var (name, type, _) in Fields)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.DbType = type;
            cmd.Parameters.Add(p);
        }

        var count = 0;
        foreach (var row in rows)
        {
            for (var i = 0; i < Fields.Length; i++)
                cmd.Parameters[i].Value = Fields[i].get(row);
            count += cmd.ExecuteNonQuery();
        }
        if (commit) trans.Commit(); else trans.Rollback();
        return count;
    }

    // A command for each row that are sent together
    private int ExecBatch(string sql, List<BatchRow> rows, bool commit)
    {
        using var trans = dbConn.BeginTransaction();
        using var batch = dbConn.CreateBatch();
        batch.Transaction = trans;
        foreach (var row in rows)
        {
            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = sql;
            foreach (var (name, type, get) in Fields)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = name;
                p.DbType = type;
                p.Value = get(row);
                cmd.Parameters.Add(p);
            }
            batch.BatchCommands.Add(cmd);
        }
        var count = batch.ExecuteNonQuery();
        if (commit) trans.Commit(); else trans.Rollback();
        return count;
    }

    private int UpdateAll(bool useDbBatch)
    {
        db.GetDialectProvider().UseDbBatch = useDbBatch;
        using var trans = db.OpenTransaction();
        var count = db.UpdateAll(existing);
        trans.Commit();
        return count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Update")]
    public int UpdateAll_OneAtATime() => UpdateAll(useDbBatch: false);

    [Benchmark, BenchmarkCategory("Update")]
    public int UpdateAll() => UpdateAll(useDbBatch: true);

    [Benchmark, BenchmarkCategory("Update")]
    public int Update_Commands() => ExecCommands(updateSql, existing, commit: true);

    [Benchmark, BenchmarkCategory("Update")]
    public int Update_DbBatch() => ExecBatch(updateSql, existing, commit: true);

    // Inserts are rolled back so each run inserts into the same table

    private int InsertAll(bool useDbBatch)
    {
        db.GetDialectProvider().UseDbBatch = useDbBatch;
        using var trans = db.OpenTransaction();
        db.InsertAll(added);
        trans.Rollback();
        return added.Count;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Insert")]
    public int InsertAll_OneAtATime() => InsertAll(useDbBatch: false);

    [Benchmark, BenchmarkCategory("Insert")]
    public int InsertAll() => InsertAll(useDbBatch: true);

    [Benchmark, BenchmarkCategory("Insert")]
    public int Insert_Commands() => ExecCommands(insertSql, added, commit: false);

    [Benchmark, BenchmarkCategory("Insert")]
    public int Insert_DbBatch() => ExecBatch(insertSql, added, commit: false);
}
