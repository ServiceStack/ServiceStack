#nullable enable
using System;
using System.Collections.Generic;
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
    /// The mandatory filters of the command's connection
    /// </summary>
    public static OrmLiteConnectionFilters GetFilters(this IDbCommand dbCmd)
    {
        for (var i = 0; dbCmd != null && i < 10; i++)
        {
            if (dbCmd is OrmLiteCommand ormLiteCmd)
                return ormLiteCmd.OrmLiteConnection.Filters;
            dbCmd = (dbCmd as IHasDbCommand)?.DbCommand is { } inner && inner != dbCmd ? inner : null;
        }
        return OrmLiteConnectionFilters.Empty;
    }

    /// <summary>
    /// Apply the connection's mandatory filters for the query's table
    /// </summary>
    internal static SqlExpression<T> WithFilters<T>(this SqlExpression<T> q, IDbConnection db) =>
        q.WithFilters(db.GetFilters());

    internal static SqlExpression<T> WithFilters<T>(this SqlExpression<T> q, IDbCommand dbCmd) =>
        q.WithFilters(dbCmd.GetFilters());

    internal static SqlExpression<T> WithFilters<T>(this SqlExpression<T> q, OrmLiteConnectionFilters filters)
    {
        if (filters.IsEmpty)
            return q;
        q.ConnectionFilters = filters; // for filters of joined tables
        foreach (var filter in filters.GetEnsureFilters<T>())
            q.EnsureConnectionFilter(filter);
        return q;
    }

    /// <summary>
    /// A query for the table with the connection's mandatory filters applied
    /// </summary>
    internal static SqlExpression<T> CreateQuery<T>(this IDbCommand dbCmd) =>
        dbCmd.GetDialectProvider().SqlExpression<T>().WithFilters(dbCmd);

    internal static SqlExpression<T> CreateQuery<T>(this IDbConnection dbConn) =>
        dbConn.GetDialectProvider().SqlExpression<T>().WithFilters(dbConn);

    /// <summary>
    /// Params of filter conditions added to commands, e.g. @_f0, so they don't clash with the command's params
    /// </summary>
    internal const string FilterParamPrefix = "_f";

    /// <summary>
    /// The table's filters as a SQL condition with columns prefixed by the table, or alias, with params named with
    /// paramPrefix, or null if no filters apply to the table
    /// </summary>
    internal static string ToFilterCondition(this OrmLiteConnectionFilters filters, IOrmLiteDialectProvider dialect,
        Type tableType, string? alias, string paramPrefix, out List<IDbDataParameter> filterParams)
    {
        filterParams = [];
        if (filters.IsEmpty)
            return null!;
        var fn = FilterConditionFns.GetOrAdd(tableType, type =>
            (FilterConditionFn)Delegate.CreateDelegate(typeof(FilterConditionFn),
                typeof(OrmLiteConnectionFiltersApi).GetMethod(nameof(ToFilterConditionFor),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(type)));
        return fn(filters, dialect, alias, paramPrefix, out filterParams);
    }

    private delegate string FilterConditionFn(OrmLiteConnectionFilters filters, IOrmLiteDialectProvider dialect,
        string? alias, string paramPrefix, out List<IDbDataParameter> filterParams);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, FilterConditionFn> FilterConditionFns = new();

    private static string ToFilterConditionFor<T>(OrmLiteConnectionFilters filters, IOrmLiteDialectProvider dialect,
        string? alias, string paramPrefix, out List<IDbDataParameter> filterParams)
    {
        filterParams = [];
        var ensureFilters = filters.GetEnsureFilters<T>();
        if (ensureFilters.Length == 0)
            return null!;

        var q = dialect.SqlExpression<T>();
        q.ParamPrefix = paramPrefix;
        if (alias != null)
            q.SetTableAlias(alias);
        foreach (var filter in ensureFilters)
            q.EnsureConnectionFilter(filter);
        filterParams = q.Params;
        return q.EnsureExpression;
    }

    /// <summary>
    /// The table's filter condition for SQL OrmLite constructs on the command, with its params added to the command,
    /// or null if no filters apply to the table
    /// </summary>
    internal static string? GetFilterCondition(this IDbCommand dbCmd, Type tableType)
    {
        var filters = dbCmd.GetFilters();
        if (filters.IsEmpty)
            return null;
        var condition = filters.ToFilterCondition(dbCmd.GetDialectProvider(), tableType, alias: null,
            FilterParamPrefix, out var filterParams);
        if (condition == null)
            return null;

        // Replace params of a previous filter condition, e.g. when loading multiple references with the same command
        for (var i = dbCmd.Parameters.Count - 1; i >= 0; i--)
        {
            if (dbCmd.Parameters[i] is IDbDataParameter p
                && p.ParameterName.TrimStart('@', ':', '?').StartsWith(FilterParamPrefix, StringComparison.Ordinal))
                dbCmd.Parameters.RemoveAt(i);
        }
        foreach (var p in filterParams)
            dbCmd.Parameters.Add(p);
        return condition;
    }

    /// <summary>
    /// SELECT statement for the table from a WHERE filter, e.g. from db.Select&lt;T&gt;("Age &gt; @age"), with the
    /// table's filter condition. Complete SELECT statements are raw SQL which isn't changed.
    /// </summary>
    internal static string ToFilteredSelectStatement(this IDbCommand dbCmd, Type tableType, string? sqlFilter,
        params object[] filterParams)
    {
        var dialect = dbCmd.GetDialectProvider();
        if (!dialect.IsFullSelectStatement(sqlFilter))
        {
            var condition = dbCmd.GetFilterCondition(tableType);
            if (condition != null)
                sqlFilter = CombineFilter(condition, sqlFilter);
        }
        return dialect.ToSelectStatement(tableType, sqlFilter, filterParams);
    }

    /// <summary>
    /// Combine a filter condition with a WHERE filter, which can also be just an ORDER BY or LIMIT
    /// </summary>
    internal static string CombineFilter(string condition, string? sqlFilter)
    {
        if (string.IsNullOrWhiteSpace(sqlFilter))
            return condition;
        var trimmed = sqlFilter!.TrimStart();
        if (trimmed.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed.Substring("WHERE ".Length);
        return trimmed.StartsWith("ORDER ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("LIMIT ", StringComparison.OrdinalIgnoreCase)
            ? condition + " " + trimmed
            : condition + " AND (" + trimmed + ")";
    }

    /// <summary>
    /// Add the table's filter condition to a SELECT statement OrmLite constructed, after its first WHERE
    /// </summary>
    internal static string AddFilterCondition(this IDbCommand dbCmd, Type tableType, string sql)
    {
        var condition = dbCmd.GetFilterCondition(tableType);
        if (condition == null)
            return sql;
        var pos = sql.IndexOf(" WHERE ", StringComparison.Ordinal);
        return pos >= 0
            ? sql.Substring(0, pos + " WHERE ".Length) + condition + " AND " + sql.Substring(pos + " WHERE ".Length)
            : sql + " WHERE " + condition;
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
