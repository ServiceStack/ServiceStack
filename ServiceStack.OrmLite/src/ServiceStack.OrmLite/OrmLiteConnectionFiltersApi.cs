#nullable enable
using System;
using System.Data;
using System.Linq.Expressions;
using ServiceStack.Data;

namespace ServiceStack.OrmLite;

/// <summary>
/// Connection-scoped mandatory filters, e.g. for multi-tenancy or soft deletes, applied to every query OrmLite creates
/// on the connection for a table they apply to
/// </summary>
public static class OrmLiteConnectionFiltersApi
{
    /// <summary>
    /// Only return rows matching the filter on this connection, for the table type or every table implementing the
    /// interface, e.g:
    /// <para>db.EnsureFilter&lt;IHasTenantId&gt;(x =&gt; x.TenantId == tenantId);</para>
    /// The filter is added to queries with Ensure(), so other conditions can narrow results but never widen them.
    /// </summary>
    public static IDbConnection EnsureFilter<T>(this IDbConnection db, Expression<Func<T, bool>> filter)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));
        var dbConn = db.ToOrmLiteConnection()
            ?? throw new NotSupportedException("Filters can only be added to connections opened by OrmLite");
        dbConn.Filters = dbConn.Filters.AddEnsureFilter(typeof(T), filter);
        return db;
    }

    /// <summary>
    /// The mandatory filters of the connection
    /// </summary>
    public static OrmLiteConnectionFilters GetFilters(this IDbConnection db) =>
        db.ToOrmLiteConnection()?.Filters ?? OrmLiteConnectionFilters.Empty;

    /// <summary>
    /// Apply the connection's mandatory filters for the query's table
    /// </summary>
    internal static SqlExpression<T> WithFilters<T>(this SqlExpression<T> q, IDbConnection db)
    {
        var filters = db.GetFilters();
        if (filters.IsEmpty)
            return q;
        foreach (var filter in filters.GetEnsureFilters<T>())
            q.EnsureConnectionFilter(filter);
        return q;
    }

    internal static OrmLiteConnection? ToOrmLiteConnection(this IDbConnection? db)
    {
        // Unwrap connection wrappers, e.g. profilers, to find the OrmLiteConnection
        for (var i = 0; db != null && i < 10; i++)
        {
            if (db is OrmLiteConnection dbConn)
                return dbConn;
            db = (db as IHasDbConnection)?.DbConnection is { } inner && inner != db ? inner : null;
        }
        return null;
    }
}
