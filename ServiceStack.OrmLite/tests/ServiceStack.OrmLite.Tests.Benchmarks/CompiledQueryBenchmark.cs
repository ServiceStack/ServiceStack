using System.Data;
using BenchmarkDotNet.Configs;

namespace ServiceStack.OrmLite.Tests.Benchmarks;

public enum OrderStatus
{
    Pending,
    Shipped,
    Cancelled,
}

public class Customer
{
    [AutoIncrement] public int Id { get; set; }
    public string Name { get; set; }
    public string Country { get; set; }
}

public class Order
{
    [AutoIncrement] public int Id { get; set; }
    [References(typeof(Customer))]
    public int CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public string Reference { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class OrderSummary
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public string Name { get; set; }
}

/// <summary>
/// The time to get the SQL and db params of a query that's ready to run, without running it:
/// a typed query that generates its SQL each time vs a compiled query that only creates its db params.
/// <para>dotnet run -c Release -- --filter *CompiledQuery*</para>
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CompiledQueryBenchmark
{
    static readonly CompiledQuery<Order, int> ById = OrmLiteQuery.Compile<Order, int>(
        (q, id) => q.Where(x => x.Id == id));

    static readonly CompiledQuery<Order, int> RecentByCustomer = OrmLiteQuery.Compile<Order, int>(
        (q, customerId) => q
            .Where(x => x.CustomerId == customerId && x.Status != OrderStatus.Cancelled)
            .OrderByDescending(x => x.Id)
            .Take(20));

    static readonly CompiledQuery<Order, string> Search = OrmLiteQuery.Compile<Order, string>(
        (q, text) => q.Where(x => x.Reference.StartsWith(text)).OrderBy(x => x.Reference));

    static readonly CompiledQuery<Order, int[]> ByIds = OrmLiteQuery.Compile<Order, int[]>(
        (q, ids) => q.Where(x => Sql.In(x.Id, ids)));

    static readonly CompiledQuery<Order, int, DateTime, string> Summaries =
        OrmLiteQuery.Compile<Order, int, DateTime, string>((q, customerId, since, country) => q
            .Join<Customer>((o, c) => o.CustomerId == c.Id)
            .Where(x => x.CustomerId == customerId && x.CreatedDate >= since && x.Status == OrderStatus.Shipped)
            .And<Customer>(c => c.Country == country)
            .OrderByDescending(x => x.CreatedDate)
            .Select<Order, Customer>((o, c) => new { o.Id, o.Total, c.Name }));

    private IDbConnection db;
    private int id = 42;
    private string text = "INV-2026";
    private string country = "US";
    private DateTime since = new(2026, 1, 1);
    private int[] ids = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    [Params("sqlite", "pgsql", "mssql", "mysql")]
    public string Dialect { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        IOrmLiteDialectProvider dialect = Dialect switch {
            "pgsql" => PostgreSqlDialect.Provider,
            "mssql" => SqlServer2022Dialect.Provider,
            "mysql" => MySqlDialect.Provider,
            _ => SqliteDialect.Provider,
        };
        // SQL is generated for the dialect, the connection is never opened
        db = new OrmLiteConnectionFactory("Server=localhost", dialect).CreateDbConnection();

        // Each pair has to return the same SQL, and the compiled queries have to reuse theirs
        (Func<int> typed, Func<int> compiled)[] pairs = [
            (ById_Typed, ById_Compiled),
            (Recent_Typed, Recent_Compiled),
            (In_Typed, In_Compiled),
            (Join_Typed, Join_Compiled),
        ];
        Search_Compiled(); // its LIKE always has an ESCAPE, which the typed query only has for text with wildcards
        foreach (var (typed, compiled) in pairs)
        {
            compiled();
            if (typed() != compiled())
                throw new Exception($"{compiled.Method.Name} doesn't return the SQL of {typed.Method.Name}");
        }
        var reason = ById.NotCachedReason ?? RecentByCustomer.NotCachedReason ?? Search.NotCachedReason
            ?? ByIds.NotCachedReason ?? Summaries.NotCachedReason;
        if (reason != null)
            throw new Exception("A compiled query doesn't reuse its SQL: " + reason);
    }

    static int Ready(string sql, List<IDbDataParameter> dbParams) => sql.Length + dbParams.Count;

    [Benchmark(Baseline = true), BenchmarkCategory("1. By Id")]
    public int ById_Typed()
    {
        var orderId = id;
        var q = db.From<Order>().Where(x => x.Id == orderId);
        return Ready(q.SelectInto<Order>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("1. By Id")]
    public int ById_Compiled()
    {
        var q = ById.Bind(db, id);
        return Ready(q.SelectInto<Order>(QueryType.Select), q.Params);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("2. Filter, order and take")]
    public int Recent_Typed()
    {
        var customerId = id;
        var q = db.From<Order>()
            .Where(x => x.CustomerId == customerId && x.Status != OrderStatus.Cancelled)
            .OrderByDescending(x => x.Id)
            .Take(20);
        return Ready(q.SelectInto<Order>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("2. Filter, order and take")]
    public int Recent_Compiled()
    {
        var q = RecentByCustomer.Bind(db, id);
        return Ready(q.SelectInto<Order>(QueryType.Select), q.Params);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("3. Search text")]
    public int Search_Typed()
    {
        var reference = text;
        var q = db.From<Order>().Where(x => x.Reference.StartsWith(reference)).OrderBy(x => x.Reference);
        return Ready(q.SelectInto<Order>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("3. Search text")]
    public int Search_Compiled()
    {
        var q = Search.Bind(db, text);
        return Ready(q.SelectInto<Order>(QueryType.Select), q.Params);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("4. IN 10 values")]
    public int In_Typed()
    {
        var orderIds = ids;
        var q = db.From<Order>().Where(x => Sql.In(x.Id, orderIds));
        return Ready(q.SelectInto<Order>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("4. IN 10 values")]
    public int In_Compiled()
    {
        var q = ByIds.Bind(db, ids);
        return Ready(q.SelectInto<Order>(QueryType.Select), q.Params);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("5. Join, 4 filters and select")]
    public int Join_Typed()
    {
        var customerId = id;
        var from = since;
        var customerCountry = country;
        var q = db.From<Order>()
            .Join<Customer>((o, c) => o.CustomerId == c.Id)
            .Where(x => x.CustomerId == customerId && x.CreatedDate >= from && x.Status == OrderStatus.Shipped)
            .And<Customer>(c => c.Country == customerCountry)
            .OrderByDescending(x => x.CreatedDate)
            .Select<Order, Customer>((o, c) => new { o.Id, o.Total, c.Name });
        return Ready(q.SelectInto<OrderSummary>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("5. Join, 4 filters and select")]
    public int Join_Compiled()
    {
        var q = Summaries.Bind(db, id, since, country);
        return Ready(q.SelectInto<OrderSummary>(QueryType.Select), q.Params);
    }
}
