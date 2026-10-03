#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using ServiceStack.Data;

namespace ServiceStack.OrmLite;

/// <summary>
/// Connection-scoped mandatory filters, e.g. for multi-tenancy or soft deletes, and write rules, e.g. for auditing,
/// applied to every statement OrmLite creates on the connection for a table they apply to
/// </summary>
public static class OrmLiteConnectionFiltersApi
{
    /// <summary>
    /// Use the filters and rules of a FilterSet, with the scope they read their values from, e.g:
    /// <para>db.UseFilters(WorkspaceFilters.For(scope));</para>
    /// Using a set again with the same scope is ignored, and with a different scope throws.
    /// </summary>
    public static IDbConnection UseFilters(this IDbConnection db, BoundFilterSet filters)
    {
        if (filters == null)
            throw new ArgumentNullException(nameof(filters));
        var dbConn = db.ToOrmLiteConnection()
            ?? throw new NotSupportedException("Filters can only be used by connections opened by OrmLite");
        dbConn.Filters = dbConn.Filters.Add(filters);
        return db;
    }

    /// <summary>
    /// Use the filters and rules of a FilterSet without a scope, e.g:
    /// <para>db.UseFilters(SoftDeletes);</para>
    /// </summary>
    public static IDbConnection UseFilters(this IDbConnection db, FilterSet filters) => filters == null
        ? throw new ArgumentNullException(nameof(filters))
        : db.UseFilters(new BoundFilterSet(filters, null, filters.Rules));

    /// <summary>
    /// The same connection and transaction without any of its filters or rules, e.g. for admin tasks:
    /// <para>var adminDb = db.WithoutFilters();</para>
    /// Disposing it doesn't close the connection.
    /// </summary>
    public static IDbConnection WithoutFilters(this IDbConnection db)
    {
        var dbConn = db.ToOrmLiteConnection()
            ?? throw new NotSupportedException("WithoutFilters() can only be used with connections opened by OrmLite");
        return dbConn.CreateWithoutFilters();
    }

    /// <summary>
    /// Whether the connection was returned by WithoutFilters()
    /// </summary>
    public static bool IsWithoutFilters(this IDbConnection db) => db.ToOrmLiteConnection()?.IsWithoutFilters == true;

    /// <summary>
    /// Keep a value with the connection for as long as it's open, e.g. the tenant its filters confine it to:
    /// <para>db.SetItem("TenantId", tenantId);</para>
    /// Items are shared with the connections returned by WithoutFilters(), which are the same connection.
    /// </summary>
    public static IDbConnection SetItem(this IDbConnection db, string key, object? value)
    {
        var dbConn = db.ToOrmLiteConnection()
            ?? throw new NotSupportedException("Items can only be kept with connections opened by OrmLite");
        dbConn.Items[key] = value;
        return db;
    }

    /// <summary>
    /// The value kept with the connection, or the default value if there isn't one, e.g:
    /// <para>var tenantId = db.GetItem&lt;string&gt;("TenantId");</para>
    /// </summary>
    public static T? GetItem<T>(this IDbConnection db, string key) =>
        db.ToOrmLiteConnection() is { } dbConn && dbConn.Items.TryGetValue(key, out var value) && value is T item
            ? item
            : default;

    /// <summary>
    /// The value kept with the connection, adding the value returned by the function if there isn't one
    /// </summary>
    public static T GetOrAddItem<T>(this IDbConnection db, string key, Func<T> create)
    {
        var dbConn = db.ToOrmLiteConnection()
            ?? throw new NotSupportedException("Items can only be kept with connections opened by OrmLite");
        if (dbConn.Items.TryGetValue(key, out var value) && value is T item)
            return item;
        item = create();
        dbConn.Items[key] = item;
        return item;
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
        CompiledQueryBuild.FilterTable(typeof(T));
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
    /// Whether the param is from a filter condition, e.g. @_f0, and not from a column
    /// </summary>
    public static bool IsFilterParam(string? paramName)
    {
        if (paramName == null)
            return false;
        var name = paramName.TrimStart('@', ':', '?');
        if (name.Length <= FilterParamPrefix.Length || !name.StartsWith(FilterParamPrefix, StringComparison.Ordinal))
            return false;
        for (var i = FilterParamPrefix.Length; i < name.Length; i++)
        {
            if (!char.IsDigit(name[i]))
                return false;
        }
        return true;
    }

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
            if (dbCmd.Parameters[i] is IDbDataParameter p && IsFilterParam(p.ParameterName))
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

    /// <summary>
    /// Add the table's filter condition to the UPDATE or DELETE statement OrmLite constructed on the command, whose
    /// WHERE (if any) only has AND conditions, so rows that don't match the filter aren't changed
    /// </summary>
    internal static void AddFilterToWhere(this IDbCommand dbCmd, Type tableType)
    {
        if (string.IsNullOrEmpty(dbCmd.CommandText))
            return;
        dbCmd.CommandText = dbCmd.AddFilterToWhere(tableType, dbCmd.CommandText);
    }

    internal static string AddFilterToWhere(this IDbCommand dbCmd, Type tableType, string sql)
    {
        var condition = dbCmd.GetFilterCondition(tableType);
        if (condition == null)
            return sql;
        var hasWhere = sql.IndexOf(" WHERE ", StringComparison.Ordinal) >= 0;
        return sql.TrimEnd() + (hasWhere ? " AND " : " WHERE ") + condition;
    }

    /// <summary>
    /// Add the table's filter condition to a WHERE expression, e.g. "WHERE Age &gt; @age", which can have OR conditions
    /// </summary>
    internal static string? AddFilterToWhereExpression(this IDbCommand dbCmd, Type tableType, string? whereExpression)
    {
        var condition = dbCmd.GetFilterCondition(tableType);
        return condition != null
            ? "WHERE " + CombineFilter(condition, whereExpression)
            : whereExpression;
    }

    /// <summary>
    /// DELETE statement for the table from a WHERE filter, e.g. from db.Delete&lt;T&gt;("Age &gt; @age"), with the
    /// table's filter condition. Complete DELETE statements are raw SQL which isn't changed.
    /// </summary>
    internal static string ToFilteredDeleteStatement(this IDbCommand dbCmd, Type tableType, string? sqlFilter,
        params object[] filterParams)
    {
        const string deleteStatement = "DELETE ";
        var isFullDeleteStatement = sqlFilter != null
            && sqlFilter.Length > deleteStatement.Length
            && sqlFilter.StartsWith(deleteStatement, StringComparison.OrdinalIgnoreCase);
        if (!isFullDeleteStatement)
        {
            var condition = dbCmd.GetFilterCondition(tableType);
            if (condition != null)
                sqlFilter = CombineFilter(condition, sqlFilter);
        }
        return dbCmd.GetDialectProvider().ToDeleteStatement(tableType, sqlFilter, filterParams);
    }

    /// <summary>
    /// Whether any of the connection's filters currently apply to the table
    /// </summary>
    internal static bool HasFilters<T>(this IDbCommand dbCmd)
    {
        var filters = dbCmd.GetFilters();
        return !filters.IsEmpty && filters.HasEnsureFilters<T>();
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
