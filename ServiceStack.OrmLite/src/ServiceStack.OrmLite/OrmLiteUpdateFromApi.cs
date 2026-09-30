using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite;

/// <summary>
/// Update rows with values from joined tables in a single statement, without reading them into .NET, e.g:
/// <para>db.UpdateFrom&lt;Order, Customer&gt;((o, c) =&gt; new Order { Region = c.Region },
///     db.From&lt;Order&gt;().Join&lt;Customer&gt;((o, c) =&gt; o.CustomerId == c.Id))</para>
/// Uses UPDATE ... FROM in PostgreSQL, SQLite 3.33+ and SQL Server, and UPDATE ... JOIN in MySQL.
/// Other RDBMS throw a NotSupportedException.
/// </summary>
public static class OrmLiteUpdateFromApi
{
    /// <summary>
    /// Update the columns in the set expression of the rows matching the query, e.g. with a filter on a joined table:
    /// <para>db.UpdateFrom(o =&gt; new Order { Total = o.Total * 0.9m }, q)</para>
    /// </summary>
    /// <returns>The number of rows updated</returns>
    public static int UpdateFrom<T>(this IDbConnection dbConn,
        Expression<Func<T, T>> set, SqlExpression<T> q) =>
        dbConn.Exec(dbCmd => dbCmd.UpdateFrom(q, set));

    /// <summary>
    /// Update the columns in the set expression of the rows matching the query, with values from a joined table, e.g:
    /// <para>db.UpdateFrom&lt;Order, Customer&gt;((o, c) =&gt; new Order { Region = c.Region }, q)</para>
    /// </summary>
    /// <returns>The number of rows updated</returns>
    public static int UpdateFrom<T, TJoin>(this IDbConnection dbConn,
        Expression<Func<T, TJoin, T>> set, SqlExpression<T> q) =>
        dbConn.Exec(dbCmd => dbCmd.UpdateFrom(q, set));

    /// <summary>
    /// Update the columns in the set expression of the rows matching the query, with values from 2 joined tables
    /// </summary>
    /// <returns>The number of rows updated</returns>
    public static int UpdateFrom<T, TJoin1, TJoin2>(this IDbConnection dbConn,
        Expression<Func<T, TJoin1, TJoin2, T>> set, SqlExpression<T> q) =>
        dbConn.Exec(dbCmd => dbCmd.UpdateFrom(q, set));

    /// <summary>
    /// Update the columns in the set expression of the rows matching the query, with values from 3 joined tables
    /// </summary>
    /// <returns>The number of rows updated</returns>
    public static int UpdateFrom<T, TJoin1, TJoin2, TJoin3>(this IDbConnection dbConn,
        Expression<Func<T, TJoin1, TJoin2, TJoin3, T>> set, SqlExpression<T> q) =>
        dbConn.Exec(dbCmd => dbCmd.UpdateFrom(q, set));

    public static Task<int> UpdateFromAsync<T>(this IDbConnection dbConn,
        Expression<Func<T, T>> set, SqlExpression<T> q, CancellationToken token = default) =>
        dbConn.Exec(dbCmd => dbCmd.UpdateFromAsync(q, set, token));

    public static Task<int> UpdateFromAsync<T, TJoin>(this IDbConnection dbConn,
        Expression<Func<T, TJoin, T>> set, SqlExpression<T> q, CancellationToken token = default) =>
        dbConn.Exec(dbCmd => dbCmd.UpdateFromAsync(q, set, token));

    public static Task<int> UpdateFromAsync<T, TJoin1, TJoin2>(this IDbConnection dbConn,
        Expression<Func<T, TJoin1, TJoin2, T>> set, SqlExpression<T> q, CancellationToken token = default) =>
        dbConn.Exec(dbCmd => dbCmd.UpdateFromAsync(q, set, token));

    public static Task<int> UpdateFromAsync<T, TJoin1, TJoin2, TJoin3>(this IDbConnection dbConn,
        Expression<Func<T, TJoin1, TJoin2, TJoin3, T>> set, SqlExpression<T> q, CancellationToken token = default) =>
        dbConn.Exec(dbCmd => dbCmd.UpdateFromAsync(q, set, token));

    /// <summary>
    /// The UPDATE statement from a copy of the query, so the query and its params can be reused
    /// </summary>
    private static string PrepareUpdateFrom<T>(SqlExpression<T> q, LambdaExpression set, out SqlExpression<T> stmt)
    {
        if (q == null)
            throw new ArgumentNullException(nameof(q));
        stmt = q.Clone();
        return stmt.ToUpdateFromStatement(set);
    }

    internal static int UpdateFrom<T>(this IDbCommand dbCmd, SqlExpression<T> q, LambdaExpression set)
    {
        var sql = PrepareUpdateFrom(q, set, out var stmt);
        return dbCmd.ExecuteSql(sql, stmt.Params);
    }

    internal static Task<int> UpdateFromAsync<T>(this IDbCommand dbCmd, SqlExpression<T> q, LambdaExpression set,
        CancellationToken token)
    {
        var sql = PrepareUpdateFrom(q, set, out var stmt);
        return dbCmd.ExecuteSqlAsync(sql, stmt.Params, null, token);
    }
}
