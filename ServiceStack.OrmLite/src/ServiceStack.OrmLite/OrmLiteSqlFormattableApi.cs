using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite;

/// <summary>
/// Raw SQL APIs accepting <see cref="Sql.Fmt(System.FormattableString)"/> interpolated SQL where every
/// interpolated value is sent as a db param, e.g:
/// <para>db.Select&lt;Person&gt;(Sql.Fmt($"Age &gt; {age} AND LastName IN ({names})"))</para>
/// </summary>
public static class OrmLiteSqlFormattableApi
{
    private static string ToSql(this IDbConnection dbConn, SqlFormattable sql, out Dictionary<string, object> dbParams) =>
        sql.ToSql(dbConn.GetDialectProvider(), out dbParams);

    /// <summary>
    /// Returns results from a parameterized query, e.g:
    /// <para>db.Select&lt;Person&gt;(Sql.Fmt($"Age &gt; {age}"))</para>
    /// </summary>
    public static List<T> Select<T>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.Select<T>(dbConn.ToSql(sql, out var dbParams), dbParams);

    /// <summary>
    /// Returns the first result from a parameterized query, e.g:
    /// <para>db.Single&lt;Person&gt;(Sql.Fmt($"Age = {age}"))</para>
    /// </summary>
    public static T Single<T>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.Single<T>(dbConn.ToSql(sql, out var dbParams), (object)dbParams);

    /// <summary>
    /// Returns a single scalar value from a parameterized query, e.g:
    /// <para>db.Scalar&lt;int&gt;(Sql.Fmt($"SELECT COUNT(*) FROM Person WHERE Age &gt; {age}"))</para>
    /// </summary>
    public static T Scalar<T>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.Scalar<T>(dbConn.ToSql(sql, out var dbParams), (object)dbParams);

    /// <summary>
    /// Returns the first column in a List from a parameterized query, e.g:
    /// <para>db.Column&lt;string&gt;(Sql.Fmt($"SELECT LastName FROM Person WHERE Age = {age}"))</para>
    /// </summary>
    public static List<T> Column<T>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.Column<T>(dbConn.ToSql(sql, out var dbParams), (object)dbParams);

    /// <summary>
    /// Returns true if the parameterized query returns any records, e.g:
    /// <para>db.Exists&lt;Person&gt;(Sql.Fmt($"Age = {age}"))</para>
    /// </summary>
    public static bool Exists<T>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.Exists<T>(dbConn.ToSql(sql, out var dbParams), (object)dbParams);

    /// <summary>
    /// Returns results from an arbitrary parameterized raw sql query, e.g:
    /// <para>db.SqlList&lt;Person&gt;(Sql.Fmt($"EXEC GetRockstarsAged {age}"))</para>
    /// </summary>
    public static List<T> SqlList<T>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.SqlList<T>(dbConn.ToSql(sql, out var dbParams), dbParams);

    /// <summary>
    /// Returns the first column from an arbitrary parameterized raw sql query, e.g:
    /// <para>db.SqlColumn&lt;string&gt;(Sql.Fmt($"SELECT LastName FROM Person WHERE Age &lt; {age}"))</para>
    /// </summary>
    public static List<T> SqlColumn<T>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.SqlColumn<T>(dbConn.ToSql(sql, out var dbParams), dbParams);

    /// <summary>
    /// Returns a single scalar value from an arbitrary parameterized raw sql query, e.g:
    /// <para>db.SqlScalar&lt;int&gt;(Sql.Fmt($"SELECT COUNT(*) FROM Person WHERE Age &lt; {age}"))</para>
    /// </summary>
    public static T SqlScalar<T>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.SqlScalar<T>(dbConn.ToSql(sql, out var dbParams), dbParams);

    /// <summary>
    /// Executes a parameterized raw sql non-query, e.g:
    /// <para>db.ExecuteSql(Sql.Fmt($"UPDATE Person SET LastName = {name} WHERE Id = {id}"))</para>
    /// </summary>
    /// <returns>number of rows affected</returns>
    public static int ExecuteSql(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.ExecuteNonQuery(dbConn.ToSql(sql, out var dbParams), dbParams);

    /// <summary>
    /// Executes a parameterized raw sql non-query, e.g:
    /// <para>db.ExecuteNonQuery(Sql.Fmt($"UPDATE Person SET LastName = {name} WHERE Id = {id}"))</para>
    /// </summary>
    /// <returns>number of rows affected</returns>
    public static int ExecuteNonQuery(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.ExecuteNonQuery(dbConn.ToSql(sql, out var dbParams), dbParams);

    /// <summary>
    /// Returns results of the specified table from a parameterized query into a different model, e.g:
    /// <para>db.Select&lt;EntityWithId&gt;(typeof(Person), Sql.Fmt($"Age &gt; {age}"))</para>
    /// </summary>
    public static List<TModel> Select<TModel>(this IDbConnection dbConn, Type fromTableType, SqlFormattable sql) =>
        dbConn.Select<TModel>(fromTableType, dbConn.ToSql(sql, out var dbParams), dbParams);

    /// <summary>
    /// Returns a lazily loaded stream of results from a parameterized query, e.g:
    /// <para>db.SelectLazy&lt;Person&gt;(Sql.Fmt($"Age &gt; {age}"))</para>
    /// </summary>
    public static IEnumerable<T> SelectLazy<T>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.SelectLazy<T>(dbConn.ToSql(sql, out var dbParams), dbParams);

    /// <summary>
    /// Returns a lazily loaded stream of the first column from a parameterized query, e.g:
    /// <para>db.ColumnLazy&lt;string&gt;(Sql.Fmt($"SELECT LastName FROM Person WHERE Age = {age}"))</para>
    /// </summary>
    public static IEnumerable<T> ColumnLazy<T>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.ColumnLazy<T>(dbConn.ToSql(sql, out var dbParams), (object)dbParams);

    /// <summary>
    /// Returns the distinct first column values in a HashSet from a parameterized query, e.g:
    /// <para>db.ColumnDistinct&lt;int&gt;(Sql.Fmt($"SELECT Age FROM Person WHERE Age &lt; {age}"))</para>
    /// </summary>
    public static HashSet<T> ColumnDistinct<T>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.ColumnDistinct<T>(dbConn.ToSql(sql, out var dbParams), (object)dbParams);

    /// <summary>
    /// Returns a Dictionary&lt;K, List&lt;V&gt;&gt; grouping made from the first two columns of a parameterized query, e.g:
    /// <para>db.Lookup&lt;int, string&gt;(Sql.Fmt($"SELECT Age, LastName FROM Person WHERE Age &lt; {age}"))</para>
    /// </summary>
    public static Dictionary<K, List<V>> Lookup<K, V>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.Lookup<K, V>(dbConn.ToSql(sql, out var dbParams), (object)dbParams);

    /// <summary>
    /// Returns a Dictionary from the first two columns of a parameterized query, e.g:
    /// <para>db.Dictionary&lt;int, string&gt;(Sql.Fmt($"SELECT Id, LastName FROM Person WHERE Age &lt; {age}"))</para>
    /// </summary>
    public static Dictionary<K, V> Dictionary<K, V>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.Dictionary<K, V>(dbConn.ToSql(sql, out var dbParams), (object)dbParams);

    /// <summary>
    /// Returns a list of KeyValuePairs from the first two columns of a parameterized query, e.g:
    /// <para>db.KeyValuePairs&lt;int, string&gt;(Sql.Fmt($"SELECT Id, LastName FROM Person WHERE Age &lt; {age}"))</para>
    /// </summary>
    public static List<KeyValuePair<K, V>> KeyValuePairs<K, V>(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.KeyValuePairs<K, V>(dbConn.ToSql(sql, out var dbParams), (object)dbParams);

    /// <summary>
    /// Returns the number of rows a parameterized query returns, e.g:
    /// <para>db.RowCount(Sql.Fmt($"SELECT * FROM Person WHERE Age &gt; {age}"))</para>
    /// </summary>
    public static long RowCount(this IDbConnection dbConn, SqlFormattable sql) =>
        dbConn.RowCount(dbConn.ToSql(sql, out var dbParams), (object)dbParams);

    /// <summary>
    /// Delete rows matching a parameterized filter, e.g:
    /// <para>db.Delete&lt;Person&gt;(Sql.Fmt($"Age &gt; {age}"))</para>
    /// </summary>
    /// <returns>number of rows deleted</returns>
    public static int Delete<T>(this IDbConnection dbConn, SqlFormattable sqlFilter) =>
        dbConn.Delete<T>(dbConn.ToSql(sqlFilter, out var dbParams), (object)dbParams);

    /// <summary>
    /// Delete rows of the specified table matching a parameterized filter, e.g:
    /// <para>db.Delete(typeof(Person), Sql.Fmt($"Age &gt; {age}"))</para>
    /// </summary>
    /// <returns>number of rows deleted</returns>
    public static int Delete(this IDbConnection dbConn, Type tableType, SqlFormattable sqlFilter) =>
        dbConn.Delete(tableType, dbConn.ToSql(sqlFilter, out var dbParams), (object)dbParams);

    public static Task<List<T>> SelectAsync<T>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.SelectAsync<T>(dbConn.ToSql(sql, out var dbParams), dbParams, token);

    public static Task<T> SingleAsync<T>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.SingleAsync<T>(dbConn.ToSql(sql, out var dbParams), (object)dbParams, token);

    public static Task<T> ScalarAsync<T>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.ScalarAsync<T>(dbConn.ToSql(sql, out var dbParams), (object)dbParams, token);

    public static Task<List<T>> ColumnAsync<T>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.ColumnAsync<T>(dbConn.ToSql(sql, out var dbParams), (object)dbParams, token);

    public static Task<bool> ExistsAsync<T>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.ExistsAsync<T>(dbConn.ToSql(sql, out var dbParams), (object)dbParams, token);

    public static Task<List<T>> SqlListAsync<T>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.SqlListAsync<T>(dbConn.ToSql(sql, out var dbParams), dbParams, token);

    public static Task<List<T>> SqlColumnAsync<T>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.SqlColumnAsync<T>(dbConn.ToSql(sql, out var dbParams), dbParams, token);

    public static Task<T> SqlScalarAsync<T>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.SqlScalarAsync<T>(dbConn.ToSql(sql, out var dbParams), dbParams, token);

    public static Task<int> ExecuteSqlAsync(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.ExecuteNonQueryAsync(dbConn.ToSql(sql, out var dbParams), dbParams, token);

    public static Task<int> ExecuteNonQueryAsync(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.ExecuteNonQueryAsync(dbConn.ToSql(sql, out var dbParams), dbParams, token);

    public static Task<List<TModel>> SelectAsync<TModel>(this IDbConnection dbConn, Type fromTableType, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.SelectAsync<TModel>(fromTableType, dbConn.ToSql(sql, out var dbParams), dbParams, token);

    public static Task<HashSet<T>> ColumnDistinctAsync<T>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.ColumnDistinctAsync<T>(dbConn.ToSql(sql, out var dbParams), (object)dbParams, token);

    public static Task<Dictionary<K, List<V>>> LookupAsync<K, V>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.LookupAsync<K, V>(dbConn.ToSql(sql, out var dbParams), (object)dbParams, token);

    public static Task<Dictionary<K, V>> DictionaryAsync<K, V>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.DictionaryAsync<K, V>(dbConn.ToSql(sql, out var dbParams), (object)dbParams, token);

    public static Task<List<KeyValuePair<K, V>>> KeyValuePairsAsync<K, V>(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.KeyValuePairsAsync<K, V>(dbConn.ToSql(sql, out var dbParams), (object)dbParams, token);

    public static Task<long> RowCountAsync(this IDbConnection dbConn, SqlFormattable sql, CancellationToken token = default) =>
        dbConn.RowCountAsync(dbConn.ToSql(sql, out var dbParams), (object)dbParams, token);

    public static Task<int> DeleteAsync<T>(this IDbConnection dbConn, SqlFormattable sqlFilter, CancellationToken token = default) =>
        dbConn.DeleteAsync<T>(dbConn.ToSql(sqlFilter, out var dbParams), (object)dbParams, token);

    public static Task<int> DeleteAsync(this IDbConnection dbConn, Type tableType, SqlFormattable sqlFilter, CancellationToken token = default) =>
        dbConn.DeleteAsync(tableType, dbConn.ToSql(sqlFilter, out var dbParams), (object)dbParams, token);
}
