#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack;

/// <summary>
/// Returns the query plan of a profiled OrmLite SELECT query, available when both the AdminDatabaseFeature and
/// ProfilingFeature plugins are registered
/// </summary>
[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminExplainQuery : IGet, IReturn<AdminExplainQueryResponse>
{
    /// <summary>
    /// The Id of the profiled OrmLite command
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Run the query to include actual row counts and timings
    /// </summary>
    public bool? Analyze { get; set; }
}

public class AdminExplainQueryResponse : IHasResponseStatus
{
    public string? Plan { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

/// <summary>
/// Re-runs a profiled OrmLite SELECT query with its original params and returns its results
/// </summary>
[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminRunQuery : IPost, IReturn<AdminRunQueryResponse>
{
    /// <summary>
    /// The Id of the profiled OrmLite command
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The max number of rows to return, up to the AdminDatabaseFeature's RunQueryLimit (default 20)
    /// </summary>
    public int? Take { get; set; }
}

public class AdminRunQueryResponse : IHasResponseStatus
{
    public List<string> Columns { get; set; } = [];
    public List<Dictionary<string, object?>> Results { get; set; } = [];

    /// <summary>
    /// Whether the query returned more rows than were requested
    /// </summary>
    public bool Truncated { get; set; }
    public TimeSpan Duration { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

/// <summary>
/// Explains and re-runs queries captured by the ProfilingFeature. Queries are selected by the Id of their profiling
/// entry, so only SQL that was run by the App can be run, and only SELECT queries in a transaction that's rolled back.
/// </summary>
public class AdminQueryService : Service
{
    private async Task<AdminDatabaseFeature> AssertRequiredRole(ProfilingFeature profiling)
    {
        var feature = AssertPlugin<AdminDatabaseFeature>();
        await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AdminRole);
        if (profiling.AccessRole != feature.AdminRole)
            await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, profiling.AccessRole);
        return feature;
    }

    private static DiagnosticEntry AssertQuery(ProfilingFeature profiling, long id)
    {
        var entry = profiling.GetEntry(id)
            ?? throw HttpError.NotFound("Query is no longer in the profiling history");
        if (entry.Source != "OrmLite" || !IsSelect(entry.Command))
            throw new ArgumentException("Only OrmLite SELECT queries can be explained or run", nameof(id));
        return entry;
    }

    public static bool IsSelect(string? sql)
    {
        if (string.IsNullOrEmpty(sql))
            return false;
        sql = sql!.TrimStart();
        return sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || sql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase);
    }

    // DB Params with null values are recorded as "null"
    private static Dictionary<string, object?> GetArgs(DiagnosticEntry entry)
    {
        var to = new Dictionary<string, object?>();
        if (entry.NamedArgs != null)
        {
            foreach (var arg in entry.NamedArgs)
            {
                to[arg.Key] = arg.Value is "null" ? null : arg.Value;
            }
        }
        return to;
    }

    private IDbConnection OpenDb(DiagnosticEntry entry)
    {
        var dbFactory = TryResolve<IDbConnectionFactory>()
            ?? throw new NotSupportedException("IDbConnectionFactory is not registered");
        return entry.NamedConnection != null
            ? dbFactory.Open(entry.NamedConnection, AdminDatabaseFeature.ConfigureDb)
            : dbFactory.Open(AdminDatabaseFeature.ConfigureDb);
    }

    public async Task<object> Any(AdminExplainQuery request)
    {
        var profiling = AssertPlugin<ProfilingFeature>();
        await AssertRequiredRole(profiling).ConfigAwait();
        var entry = AssertQuery(profiling, request.Id);

        using var db = OpenDb(entry);
        if (request.Analyze != true)
        {
            return new AdminExplainQueryResponse {
                Plan = await db.ExplainAsync(entry.Command, GetArgs(entry)).ConfigAwait(),
            };
        }

        // Analyzing runs the query
        using var trans = db.OpenTransaction();
        try
        {
            return new AdminExplainQueryResponse {
                Plan = await db.ExplainAsync(entry.Command, GetArgs(entry), analyze: true).ConfigAwait(),
            };
        }
        finally
        {
            trans.Rollback();
        }
    }

    public async Task<object> Any(AdminRunQuery request)
    {
        var profiling = AssertPlugin<ProfilingFeature>();
        var feature = await AssertRequiredRole(profiling).ConfigAwait();
        var entry = AssertQuery(profiling, request.Id);
        var take = Math.Max(1, Math.Min(request.Take.GetValueOrDefault(feature.RunQueryLimit), feature.RunQueryLimit));

        using var db = OpenDb(entry);
        var dialect = db.GetDialectProvider();
        var to = new AdminRunQueryResponse();

        using var trans = db.OpenTransaction();
        try
        {
            var sw = Stopwatch.StartNew();
            await db.Exec(async dbCmd => {
                dbCmd.CommandText = entry.Command;
                foreach (var arg in GetArgs(entry))
                {
                    var p = dbCmd.CreateParameter();
                    p.ParameterName = arg.Key;
                    p.Value = arg.Value ?? DBNull.Value;
                    if (arg.Value != null)
                        dialect.InitDbParam(p, arg.Value.GetType());
                    dbCmd.Parameters.Add(p);
                }

                using var reader = await dialect.ExecuteReaderAsync(dbCmd).ConfigAwait();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    // Columns can have the same name, e.g. in queries with joins
                    var name = reader.GetName(i);
                    var column = name;
                    for (var n = 2; to.Columns.Contains(column); n++)
                        column = name + n;
                    to.Columns.Add(column);
                }

                while (reader.Read())
                {
                    if (to.Results.Count >= take)
                    {
                        to.Truncated = true;
                        // Don't read the remaining rows when the reader is closed
                        try { dbCmd.Cancel(); } catch (Exception) { /* not supported by all providers */ }
                        break;
                    }
                    var row = new Dictionary<string, object?>();
                    for (var i = 0; i < to.Columns.Count; i++)
                    {
                        row[to.Columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    to.Results.Add(row);
                }
                return to;
            }).ConfigAwait();
            to.Duration = sw.Elapsed;
        }
        finally
        {
            trans.Rollback();
        }
        return to;
    }
}
