using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// Run queries compiled with OrmLiteQuery.Compile(), which generate their SQL once
/// </summary>
public static class OrmLiteCompiledQueryApi
{
    /// <summary>
    /// Returns the results of a compiled query with its arguments. E.g:
    /// <para>db.Select(query.Bind(db, customerId))</para>
    /// </summary>
    public static List<T> Select<T>(this IDbConnection dbConn, BoundQuery<T> query) => dbConn.Exec(dbCmd => {
        var sql = query.SelectInto<T>(QueryType.Select);
        return dbCmd.ExprConvertToList<T>(sql, query.Params, onlyFields: query.OnlyFields);
    });

    /// <summary>
    /// Returns the first result of a compiled query with its arguments
    /// </summary>
    public static T Single<T>(this IDbConnection dbConn, BoundQuery<T> query) => dbConn.Exec(dbCmd => {
        var sql = query.SelectInto<T>(QueryType.Single);
        return dbCmd.ExprConvertTo<T>(sql, query.Params, onlyFields: query.OnlyFields);
    });

    /// <summary>
    /// Returns the number of rows of a compiled query with its arguments
    /// </summary>
    public static long Count<T>(this IDbConnection dbConn, BoundQuery<T> query) => dbConn.Exec(dbCmd => {
        var sql = query.ToCountStatement();
        return dbCmd.GetCount(sql, query.Params);
    });

    /// <summary>
    /// Returns true if a compiled query with its arguments has any rows
    /// </summary>
    public static bool Exists<T>(this IDbConnection dbConn, BoundQuery<T> query) => dbConn.Exec(dbCmd => {
        dbCmd.CommandText = query.ToExistsStatement(); // needs to evaluate SQL before setting params
        dbCmd.SetParameters(query.Params);
        var result = OrmLiteConfig.ResultsFilter != null
            ? OrmLiteConfig.ResultsFilter.GetScalar(dbCmd)
            : dbCmd.ExecuteScalar();
        return result != null;
    });

    /// <summary>
    /// Returns the results of a compiled query with its arguments. E.g:
    /// <para>db.SelectAsync(query.Bind(db, customerId))</para>
    /// </summary>
    public static Task<List<T>> SelectAsync<T>(this IDbConnection dbConn, BoundQuery<T> query, CancellationToken token = default) =>
        dbConn.Exec(dbCmd => {
            var sql = query.SelectInto<T>(QueryType.Select);
            return dbCmd.ExprConvertToListAsync<T>(sql, query.Params, query.OnlyFields, token);
        });

    /// <summary>
    /// Returns the first result of a compiled query with its arguments
    /// </summary>
    public static Task<T> SingleAsync<T>(this IDbConnection dbConn, BoundQuery<T> query, CancellationToken token = default) =>
        dbConn.Exec(dbCmd => {
            var sql = query.SelectInto<T>(QueryType.Single);
            return dbCmd.ExprConvertToAsync<T>(sql, query.Params, token);
        });

    /// <summary>
    /// Returns the number of rows of a compiled query with its arguments
    /// </summary>
    public static Task<long> CountAsync<T>(this IDbConnection dbConn, BoundQuery<T> query, CancellationToken token = default) =>
        dbConn.Exec(dbCmd => {
            var sql = query.ToCountStatement();
            return dbCmd.GetCountAsync(sql, query.Params, token);
        });

    /// <summary>
    /// Returns true if a compiled query with its arguments has any rows
    /// </summary>
    public static Task<bool> ExistsAsync<T>(this IDbConnection dbConn, BoundQuery<T> query, CancellationToken token = default) =>
        dbConn.Exec(async dbCmd => {
            dbCmd.CommandText = query.ToExistsStatement(); // needs to evaluate SQL before setting params
            dbCmd.SetParameters(query.Params);
            var result = OrmLiteConfig.ResultsFilter != null
                ? OrmLiteConfig.ResultsFilter.GetScalar(dbCmd)
                : await dbCmd.GetDialectProvider().ExecuteScalarAsync(dbCmd, token).ConfigAwait();
            return result != null;
        });

    // 0 arguments

    /// <summary>
    /// Returns the results of a compiled query. E.g:
    /// <para>db.Select(query)</para>
    /// </summary>
    public static List<T> Select<T>(this IDbConnection dbConn, CompiledQuery<T> query) =>
        dbConn.Select(query.Bind(dbConn));

    /// <summary>
    /// Returns the first result of a compiled query
    /// </summary>
    public static T Single<T>(this IDbConnection dbConn, CompiledQuery<T> query) =>
        dbConn.Single(query.Bind(dbConn));

    /// <summary>
    /// Returns the number of rows of a compiled query
    /// </summary>
    public static long Count<T>(this IDbConnection dbConn, CompiledQuery<T> query) =>
        dbConn.Count(query.Bind(dbConn));

    /// <summary>
    /// Returns true if a compiled query has any rows
    /// </summary>
    public static bool Exists<T>(this IDbConnection dbConn, CompiledQuery<T> query) =>
        dbConn.Exists(query.Bind(dbConn));

    /// <summary>
    /// Returns the results of a compiled query
    /// </summary>
    public static Task<List<T>> SelectAsync<T>(this IDbConnection dbConn, CompiledQuery<T> query, CancellationToken token = default) =>
        dbConn.SelectAsync(query.Bind(dbConn), token);

    /// <summary>
    /// Returns the first result of a compiled query
    /// </summary>
    public static Task<T> SingleAsync<T>(this IDbConnection dbConn, CompiledQuery<T> query, CancellationToken token = default) =>
        dbConn.SingleAsync(query.Bind(dbConn), token);

    /// <summary>
    /// Returns the number of rows of a compiled query
    /// </summary>
    public static Task<long> CountAsync<T>(this IDbConnection dbConn, CompiledQuery<T> query, CancellationToken token = default) =>
        dbConn.CountAsync(query.Bind(dbConn), token);

    /// <summary>
    /// Returns true if a compiled query has any rows
    /// </summary>
    public static Task<bool> ExistsAsync<T>(this IDbConnection dbConn, CompiledQuery<T> query, CancellationToken token = default) =>
        dbConn.ExistsAsync(query.Bind(dbConn), token);

    // 1 argument

    /// <summary>
    /// Returns the results of a compiled query. E.g:
    /// <para>db.Select(query, customerId)</para>
    /// </summary>
    public static List<T> Select<T, T1>(this IDbConnection dbConn, CompiledQuery<T, T1> query, T1 arg1) =>
        dbConn.Select(query.Bind(dbConn, arg1));

    /// <summary>
    /// Returns the first result of a compiled query
    /// </summary>
    public static T Single<T, T1>(this IDbConnection dbConn, CompiledQuery<T, T1> query, T1 arg1) =>
        dbConn.Single(query.Bind(dbConn, arg1));

    /// <summary>
    /// Returns the number of rows of a compiled query
    /// </summary>
    public static long Count<T, T1>(this IDbConnection dbConn, CompiledQuery<T, T1> query, T1 arg1) =>
        dbConn.Count(query.Bind(dbConn, arg1));

    /// <summary>
    /// Returns true if a compiled query has any rows
    /// </summary>
    public static bool Exists<T, T1>(this IDbConnection dbConn, CompiledQuery<T, T1> query, T1 arg1) =>
        dbConn.Exists(query.Bind(dbConn, arg1));

    /// <summary>
    /// Returns the results of a compiled query
    /// </summary>
    public static Task<List<T>> SelectAsync<T, T1>(this IDbConnection dbConn, CompiledQuery<T, T1> query, T1 arg1, CancellationToken token = default) =>
        dbConn.SelectAsync(query.Bind(dbConn, arg1), token);

    /// <summary>
    /// Returns the first result of a compiled query
    /// </summary>
    public static Task<T> SingleAsync<T, T1>(this IDbConnection dbConn, CompiledQuery<T, T1> query, T1 arg1, CancellationToken token = default) =>
        dbConn.SingleAsync(query.Bind(dbConn, arg1), token);

    /// <summary>
    /// Returns the number of rows of a compiled query
    /// </summary>
    public static Task<long> CountAsync<T, T1>(this IDbConnection dbConn, CompiledQuery<T, T1> query, T1 arg1, CancellationToken token = default) =>
        dbConn.CountAsync(query.Bind(dbConn, arg1), token);

    /// <summary>
    /// Returns true if a compiled query has any rows
    /// </summary>
    public static Task<bool> ExistsAsync<T, T1>(this IDbConnection dbConn, CompiledQuery<T, T1> query, T1 arg1, CancellationToken token = default) =>
        dbConn.ExistsAsync(query.Bind(dbConn, arg1), token);

    // 2 arguments

    /// <summary>
    /// Returns the results of a compiled query. E.g:
    /// <para>db.Select(query, arg1, arg2)</para>
    /// </summary>
    public static List<T> Select<T, T1, T2>(this IDbConnection dbConn, CompiledQuery<T, T1, T2> query, T1 arg1, T2 arg2) =>
        dbConn.Select(query.Bind(dbConn, arg1, arg2));

    /// <summary>
    /// Returns the first result of a compiled query
    /// </summary>
    public static T Single<T, T1, T2>(this IDbConnection dbConn, CompiledQuery<T, T1, T2> query, T1 arg1, T2 arg2) =>
        dbConn.Single(query.Bind(dbConn, arg1, arg2));

    /// <summary>
    /// Returns the number of rows of a compiled query
    /// </summary>
    public static long Count<T, T1, T2>(this IDbConnection dbConn, CompiledQuery<T, T1, T2> query, T1 arg1, T2 arg2) =>
        dbConn.Count(query.Bind(dbConn, arg1, arg2));

    /// <summary>
    /// Returns true if a compiled query has any rows
    /// </summary>
    public static bool Exists<T, T1, T2>(this IDbConnection dbConn, CompiledQuery<T, T1, T2> query, T1 arg1, T2 arg2) =>
        dbConn.Exists(query.Bind(dbConn, arg1, arg2));

    /// <summary>
    /// Returns the results of a compiled query
    /// </summary>
    public static Task<List<T>> SelectAsync<T, T1, T2>(this IDbConnection dbConn, CompiledQuery<T, T1, T2> query, T1 arg1, T2 arg2, CancellationToken token = default) =>
        dbConn.SelectAsync(query.Bind(dbConn, arg1, arg2), token);

    /// <summary>
    /// Returns the first result of a compiled query
    /// </summary>
    public static Task<T> SingleAsync<T, T1, T2>(this IDbConnection dbConn, CompiledQuery<T, T1, T2> query, T1 arg1, T2 arg2, CancellationToken token = default) =>
        dbConn.SingleAsync(query.Bind(dbConn, arg1, arg2), token);

    /// <summary>
    /// Returns the number of rows of a compiled query
    /// </summary>
    public static Task<long> CountAsync<T, T1, T2>(this IDbConnection dbConn, CompiledQuery<T, T1, T2> query, T1 arg1, T2 arg2, CancellationToken token = default) =>
        dbConn.CountAsync(query.Bind(dbConn, arg1, arg2), token);

    /// <summary>
    /// Returns true if a compiled query has any rows
    /// </summary>
    public static Task<bool> ExistsAsync<T, T1, T2>(this IDbConnection dbConn, CompiledQuery<T, T1, T2> query, T1 arg1, T2 arg2, CancellationToken token = default) =>
        dbConn.ExistsAsync(query.Bind(dbConn, arg1, arg2), token);

    // 3 arguments

    /// <summary>
    /// Returns the results of a compiled query. E.g:
    /// <para>db.Select(query, arg1, arg2, arg3)</para>
    /// </summary>
    public static List<T> Select<T, T1, T2, T3>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3> query, T1 arg1, T2 arg2, T3 arg3) =>
        dbConn.Select(query.Bind(dbConn, arg1, arg2, arg3));

    /// <summary>
    /// Returns the first result of a compiled query
    /// </summary>
    public static T Single<T, T1, T2, T3>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3> query, T1 arg1, T2 arg2, T3 arg3) =>
        dbConn.Single(query.Bind(dbConn, arg1, arg2, arg3));

    /// <summary>
    /// Returns the number of rows of a compiled query
    /// </summary>
    public static long Count<T, T1, T2, T3>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3> query, T1 arg1, T2 arg2, T3 arg3) =>
        dbConn.Count(query.Bind(dbConn, arg1, arg2, arg3));

    /// <summary>
    /// Returns true if a compiled query has any rows
    /// </summary>
    public static bool Exists<T, T1, T2, T3>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3> query, T1 arg1, T2 arg2, T3 arg3) =>
        dbConn.Exists(query.Bind(dbConn, arg1, arg2, arg3));

    /// <summary>
    /// Returns the results of a compiled query
    /// </summary>
    public static Task<List<T>> SelectAsync<T, T1, T2, T3>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3> query, T1 arg1, T2 arg2, T3 arg3, CancellationToken token = default) =>
        dbConn.SelectAsync(query.Bind(dbConn, arg1, arg2, arg3), token);

    /// <summary>
    /// Returns the first result of a compiled query
    /// </summary>
    public static Task<T> SingleAsync<T, T1, T2, T3>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3> query, T1 arg1, T2 arg2, T3 arg3, CancellationToken token = default) =>
        dbConn.SingleAsync(query.Bind(dbConn, arg1, arg2, arg3), token);

    /// <summary>
    /// Returns the number of rows of a compiled query
    /// </summary>
    public static Task<long> CountAsync<T, T1, T2, T3>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3> query, T1 arg1, T2 arg2, T3 arg3, CancellationToken token = default) =>
        dbConn.CountAsync(query.Bind(dbConn, arg1, arg2, arg3), token);

    /// <summary>
    /// Returns true if a compiled query has any rows
    /// </summary>
    public static Task<bool> ExistsAsync<T, T1, T2, T3>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3> query, T1 arg1, T2 arg2, T3 arg3, CancellationToken token = default) =>
        dbConn.ExistsAsync(query.Bind(dbConn, arg1, arg2, arg3), token);

    // 4 arguments

    /// <summary>
    /// Returns the results of a compiled query. E.g:
    /// <para>db.Select(query, arg1, arg2, arg3, arg4)</para>
    /// </summary>
    public static List<T> Select<T, T1, T2, T3, T4>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3, T4> query, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
        dbConn.Select(query.Bind(dbConn, arg1, arg2, arg3, arg4));

    /// <summary>
    /// Returns the first result of a compiled query
    /// </summary>
    public static T Single<T, T1, T2, T3, T4>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3, T4> query, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
        dbConn.Single(query.Bind(dbConn, arg1, arg2, arg3, arg4));

    /// <summary>
    /// Returns the number of rows of a compiled query
    /// </summary>
    public static long Count<T, T1, T2, T3, T4>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3, T4> query, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
        dbConn.Count(query.Bind(dbConn, arg1, arg2, arg3, arg4));

    /// <summary>
    /// Returns true if a compiled query has any rows
    /// </summary>
    public static bool Exists<T, T1, T2, T3, T4>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3, T4> query, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
        dbConn.Exists(query.Bind(dbConn, arg1, arg2, arg3, arg4));

    /// <summary>
    /// Returns the results of a compiled query
    /// </summary>
    public static Task<List<T>> SelectAsync<T, T1, T2, T3, T4>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3, T4> query, T1 arg1, T2 arg2, T3 arg3, T4 arg4, CancellationToken token = default) =>
        dbConn.SelectAsync(query.Bind(dbConn, arg1, arg2, arg3, arg4), token);

    /// <summary>
    /// Returns the first result of a compiled query
    /// </summary>
    public static Task<T> SingleAsync<T, T1, T2, T3, T4>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3, T4> query, T1 arg1, T2 arg2, T3 arg3, T4 arg4, CancellationToken token = default) =>
        dbConn.SingleAsync(query.Bind(dbConn, arg1, arg2, arg3, arg4), token);

    /// <summary>
    /// Returns the number of rows of a compiled query
    /// </summary>
    public static Task<long> CountAsync<T, T1, T2, T3, T4>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3, T4> query, T1 arg1, T2 arg2, T3 arg3, T4 arg4, CancellationToken token = default) =>
        dbConn.CountAsync(query.Bind(dbConn, arg1, arg2, arg3, arg4), token);

    /// <summary>
    /// Returns true if a compiled query has any rows
    /// </summary>
    public static Task<bool> ExistsAsync<T, T1, T2, T3, T4>(this IDbConnection dbConn, CompiledQuery<T, T1, T2, T3, T4> query, T1 arg1, T2 arg2, T3 arg3, T4 arg4, CancellationToken token = default) =>
        dbConn.ExistsAsync(query.Bind(dbConn, arg1, arg2, arg3, arg4), token);
}
