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
}
