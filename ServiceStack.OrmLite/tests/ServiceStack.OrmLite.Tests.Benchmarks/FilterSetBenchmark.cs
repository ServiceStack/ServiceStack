using System.Data;
using BenchmarkDotNet.Configs;

namespace ServiceStack.OrmLite.Tests.Benchmarks;

public interface IHasWorkspaceId
{
    string WorkspaceId { get; set; }
}

public class Project : IHasWorkspaceId
{
    [AutoIncrement] public int Id { get; set; }
    public string WorkspaceId { get; set; }
    public string Name { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ProjectTask : IHasWorkspaceId
{
    [AutoIncrement] public int Id { get; set; }
    public string WorkspaceId { get; set; }
    [References(typeof(Project))]
    public int ProjectId { get; set; }
    public string Title { get; set; }
    public bool IsDone { get; set; }
}

public class ProjectTaskSummary
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Name { get; set; }
}

public class WorkspaceKey
{
    [AutoIncrement] public int Id { get; set; }
    public string RefIdStr { get; set; }
    public string Name { get; set; }
}

/// <summary>
/// The scope of a multi-tenant App's connections, as in the next-saas template's SaasDb.cs
/// </summary>
public sealed class WorkspaceScope(string? workspaceId)
{
    public string? WorkspaceId { get; } = workspaceId;
    public string AssertWorkspaceId() => WorkspaceId ?? throw new InvalidOperationException("No organization");
}

/// <summary>
/// The time a connection's FilterSet adds to each statement, which translates its filters to SQL for every statement:
/// <list type="bullet">
/// <item>Plain: a connection without filters</item>
/// <item>Where: the same condition written in the query, the SQL a filter generates</item>
/// <item>Filtered: a connection using a FilterSet like the next-saas template's</item>
/// </list>
/// Filtered - Where is the overhead of applying filters, Filtered - Plain the most that translating each filter to
/// SQL once, instead of for each statement, could save. The SQLite category runs the queries, to show their share
/// of a whole statement on a database without a network.
/// <para>dotnet run -c Release -- --filter *FilterSet*</para>
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class FilterSetBenchmark
{
    static readonly FilterSet<WorkspaceScope> WorkspaceFilters = FilterSet.Create<WorkspaceScope>(f => {
        f.Ensure<IHasWorkspaceId>(x => x.WorkspaceId, s => s.AssertWorkspaceId());
        f.Filter<WorkspaceKey>((x, s) => s.WorkspaceId == null || x.RefIdStr == s.WorkspaceId);
    });

    static readonly CompiledQuery<Project, int> ById = OrmLiteQuery.Compile<Project, int>(
        (q, id) => q.Where(x => x.Id == id));

    private const string WorkspaceId = "ws_1";
    private IDbConnection plain;
    private IDbConnection filtered;
    private IDbConnection sqlitePlain;
    private IDbConnection sqliteFiltered;
    private string sqlitePath;
    private int id = 42;

    [GlobalSetup]
    public void Setup()
    {
        // SQL is generated for the dialect, these connections are never opened
        var factory = new OrmLiteConnectionFactory("Server=localhost", PostgreSqlDialect.Provider);
        plain = factory.CreateDbConnection();
        filtered = factory.CreateDbConnection();
        filtered.UseFilters(WorkspaceFilters.For(new WorkspaceScope(WorkspaceId)));

        // A file, as connections to SQLite's :memory: share one connection and its filters
        sqlitePath = Path.Combine(Path.GetTempPath(), $"filterset-benchmark-{Guid.NewGuid():N}.sqlite");
        var sqliteFactory = new OrmLiteConnectionFactory(sqlitePath, SqliteDialect.Provider);
        sqlitePlain = sqliteFactory.OpenDbConnection();
        sqlitePlain.CreateTable<Project>();
        for (var i = 1; i <= 100; i++)
            sqlitePlain.Insert(new Project { WorkspaceId = $"ws_{i % 2 + 1}", Name = $"Project {i}", CreatedDate = DateTime.UtcNow });
        sqliteFiltered = sqliteFactory.OpenDbConnection();
        sqliteFiltered.UseFilters(WorkspaceFilters.For(new WorkspaceScope(WorkspaceId)));

        // Each filtered query has to have the filter's condition and param
        (string name, Func<SqlExpression<Project>> q)[] filteredQueries = [
            (nameof(ById_Filtered), () => filtered.From<Project>().Where(x => x.Id == id)),
            (nameof(Recent_Filtered), () => RecentQuery(filtered)),
        ];
        foreach (var (name, q) in filteredQueries)
        {
            var query = q();
            var sql = query.SelectInto<Project>(QueryType.Select);
            if (!sql.Contains("\"workspace_id\"") || !query.Params.Any(p => WorkspaceId.Equals(p.Value)))
                throw new Exception($"{name} isn't filtered: {sql}");
        }
        if (sqliteFiltered.SingleById<Project>(1) != null || sqliteFiltered.SingleById<Project>(2) == null)
            throw new Exception("The SQLite query isn't filtered");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        sqlitePlain.Dispose();
        sqliteFiltered.Dispose();
        File.Delete(sqlitePath);
    }

    static int Ready(string sql, List<IDbDataParameter> dbParams) => sql.Length + dbParams.Count;

    static SqlExpression<Project> RecentQuery(IDbConnection db) => db.From<Project>()
        .Where(x => !x.IsArchived)
        .OrderByDescending(x => x.CreatedDate)
        .Take(20);

    static SqlExpression<ProjectTask> JoinQuery(IDbConnection db) => db.From<ProjectTask>()
        .Join<Project>((t, p) => t.ProjectId == p.Id)
        .Where(x => !x.IsDone)
        .Select<ProjectTask, Project>((t, p) => new { t.Id, t.Title, p.Name });

    // 1. By Id

    [Benchmark(Baseline = true), BenchmarkCategory("1. By Id")]
    public int ById_Plain()
    {
        var projectId = id;
        var q = plain.From<Project>().Where(x => x.Id == projectId);
        return Ready(q.SelectInto<Project>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("1. By Id")]
    public int ById_Where()
    {
        var projectId = id;
        var workspaceId = WorkspaceId;
        var q = plain.From<Project>().Where(x => x.WorkspaceId == workspaceId).And(x => x.Id == projectId);
        return Ready(q.SelectInto<Project>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("1. By Id")]
    public int ById_Filtered()
    {
        var projectId = id;
        var q = filtered.From<Project>().Where(x => x.Id == projectId);
        return Ready(q.SelectInto<Project>(QueryType.Select), q.Params);
    }

    // 2. Filter, order and take

    [Benchmark(Baseline = true), BenchmarkCategory("2. Filter, order and take")]
    public int Recent_Plain()
    {
        var q = RecentQuery(plain);
        return Ready(q.SelectInto<Project>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("2. Filter, order and take")]
    public int Recent_Where()
    {
        var workspaceId = WorkspaceId;
        var q = RecentQuery(plain).And(x => x.WorkspaceId == workspaceId);
        return Ready(q.SelectInto<Project>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("2. Filter, order and take")]
    public int Recent_Filtered()
    {
        var q = RecentQuery(filtered);
        return Ready(q.SelectInto<Project>(QueryType.Select), q.Params);
    }

    // 3. Join of 2 filtered tables

    [Benchmark(Baseline = true), BenchmarkCategory("3. Join 2 filtered tables")]
    public int Join_Plain()
    {
        var q = JoinQuery(plain);
        return Ready(q.SelectInto<ProjectTaskSummary>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("3. Join 2 filtered tables")]
    public int Join_Where()
    {
        var workspaceId = WorkspaceId;
        var q = JoinQuery(plain)
            .And(x => x.WorkspaceId == workspaceId)
            .And<Project>(p => p.WorkspaceId == workspaceId);
        return Ready(q.SelectInto<ProjectTaskSummary>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("3. Join 2 filtered tables")]
    public int Join_Filtered()
    {
        var q = JoinQuery(filtered);
        return Ready(q.SelectInto<ProjectTaskSummary>(QueryType.Select), q.Params);
    }

    // 4. A filter with a condition that only reads the scope

    [Benchmark(Baseline = true), BenchmarkCategory("4. Scope condition")]
    public int Keys_Plain()
    {
        var q = plain.From<WorkspaceKey>();
        return Ready(q.SelectInto<WorkspaceKey>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("4. Scope condition")]
    public int Keys_Where()
    {
        var workspaceId = WorkspaceId;
        var q = plain.From<WorkspaceKey>().Where(x => x.RefIdStr == workspaceId);
        return Ready(q.SelectInto<WorkspaceKey>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("4. Scope condition")]
    public int Keys_Filtered()
    {
        var q = filtered.From<WorkspaceKey>();
        return Ready(q.SelectInto<WorkspaceKey>(QueryType.Select), q.Params);
    }

    // 5. Compiled queries reuse their SQL on connections without filters of their tables

    [Benchmark(Baseline = true), BenchmarkCategory("5. Compiled by Id")]
    public int Compiled_Plain()
    {
        var q = ById.Bind(plain, id);
        return Ready(q.SelectInto<Project>(QueryType.Select), q.Params);
    }

    [Benchmark, BenchmarkCategory("5. Compiled by Id")]
    public int Compiled_Filtered()
    {
        var q = ById.Bind(filtered, id);
        return Ready(q.SelectInto<Project>(QueryType.Select), q.Params);
    }

    // 6. Running the query on SQLite

    [Benchmark(Baseline = true), BenchmarkCategory("6. SQLite SingleById")]
    public Project? Sqlite_Plain() => sqlitePlain.SingleById<Project>(id);

    [Benchmark, BenchmarkCategory("6. SQLite SingleById")]
    public Project? Sqlite_Filtered() => sqliteFiltered.SingleById<Project>(id);
}
