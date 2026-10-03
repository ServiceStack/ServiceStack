#nullable enable
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// How a connection searches vector indexes, for the RDBMS that has each setting
/// </summary>
public class VectorSearchOptions
{
    /// <summary>
    /// How many candidates an HNSW index search compares (PostgreSQL's hnsw.ef_search, MariaDB's mhnsw_ef_search).
    /// More are more accurate and slower, and at least as many as the rows a query takes.
    /// </summary>
    public int? EfSearch { get; set; }

    /// <summary>
    /// How many lists an IVFFlat index search reads (PostgreSQL's ivfflat.probes). More are more accurate and slower.
    /// </summary>
    public int? Probes { get; set; }

    /// <summary>
    /// Whether an index search continues until it has enough rows that match the query's other conditions
    /// (PostgreSQL's hnsw.iterative_scan and ivfflat.iterative_scan, pgvector 0.8+). Without it, an index search
    /// returns ef_search rows before the other conditions are applied, so a query filtered by e.g. a tenant can
    /// return fewer rows than it takes.
    /// </summary>
    public bool? IterativeScan { get; set; }
}

public static class OrmLiteVectorApi
{
    /// <summary>
    /// Set how this connection searches vector indexes, for the rest of its session, e.g:
    /// <para>db.SetVectorSearch(new() { EfSearch = 100, IterativeScan = true });</para>
    /// Settings the RDBMS doesn't have are ignored.
    /// </summary>
    public static IDbConnection SetVectorSearch(this IDbConnection db, VectorSearchOptions options)
    {
        foreach (var sql in db.GetDialectProvider().ToVectorSearchStatements(options))
            db.ExecuteSql(sql);
        return db;
    }

    /// <summary>
    /// Set how this connection searches vector indexes, for the rest of its session
    /// </summary>
    public static async Task<IDbConnection> SetVectorSearchAsync(this IDbConnection db, VectorSearchOptions options,
        CancellationToken token = default)
    {
        foreach (var sql in db.GetDialectProvider().ToVectorSearchStatements(options))
            await db.ExecuteSqlAsync(sql, token).ConfigAwait();
        return db;
    }
}
