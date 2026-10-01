using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite;

/// <summary>
/// UPDATE and DELETE APIs that return the affected rows in the same statement, using RETURNING in PostgreSQL and
/// SQLite 3.35+, and OUTPUT in SQL Server. Other RDBMS throw a NotSupportedException.
/// These APIs never modify the objects passed to them, they return new rows.
/// </summary>
public static class OrmLiteReturningApi
{
    /// <summary>
    /// Update the fields in the expression of rows matching the where condition (if any) and return the updated rows, e.g:
    /// <para>List&lt;Order&gt; shipped = db.UpdateOnlyReturning(() =&gt; new Order { Status = "Shipped" }, where: x =&gt; x.Status == "Packed");</para>
    /// Only populate selected columns of the returned rows with returning, e.g: returning: x =&gt; new { x.Id, x.Status }
    /// </summary>
    public static List<T> UpdateOnlyReturning<T>(this IDbConnection dbConn,
        Expression<Func<T>> updateFields, Expression<Func<T, bool>> where = null,
        Expression<Func<T, object>> returning = null) =>
        dbConn.Exec(dbCmd => dbCmd.ConvertToList<T>(
            dbCmd.PrepareUpdateOnlyReturning(updateFields, dbCmd.CreateQuery<T>().Where(where), returning)));

    /// <summary>
    /// Update the fields in the expression of rows matching the query and return the updated rows, e.g:
    /// <para>db.UpdateOnlyReturning(() =&gt; new Order { Status = "Shipped" }, db.From&lt;Order&gt;().Where(x =&gt; x.Status == "Packed"));</para>
    /// Only populate selected columns of the returned rows with returning, e.g: returning: x =&gt; new { x.Id, x.Status }
    /// </summary>
    public static List<T> UpdateOnlyReturning<T>(this IDbConnection dbConn,
        Expression<Func<T>> updateFields, SqlExpression<T> q, Expression<Func<T, object>> returning = null) =>
        dbConn.Exec(dbCmd => dbCmd.ConvertToList<T>(dbCmd.PrepareUpdateOnlyReturning(updateFields, q, returning)));

    /// <summary>
    /// Delete rows matching the where condition and return the deleted rows, e.g:
    /// <para>List&lt;Job&gt; claimed = db.DeleteReturning&lt;Job&gt;(x =&gt; x.Queue == "emails");</para>
    /// Only populate selected columns of the returned rows with returning, e.g: returning: x =&gt; x.Id
    /// </summary>
    public static List<T> DeleteReturning<T>(this IDbConnection dbConn, Expression<Func<T, bool>> where,
        Expression<Func<T, object>> returning = null) =>
        dbConn.Exec(dbCmd => dbCmd.ConvertToList<T>(
            dbCmd.PrepareDeleteReturning(dbCmd.CreateQuery<T>().Where(where), returning)));

    /// <summary>
    /// Delete rows matching the query and return the deleted rows, optionally with only selected columns
    /// </summary>
    public static List<T> DeleteReturning<T>(this IDbConnection dbConn, SqlExpression<T> q,
        Expression<Func<T, object>> returning = null) =>
        dbConn.Exec(dbCmd => dbCmd.ConvertToList<T>(dbCmd.PrepareDeleteReturning(q, returning)));

    public static Task<List<T>> UpdateOnlyReturningAsync<T>(this IDbConnection dbConn,
        Expression<Func<T>> updateFields, Expression<Func<T, bool>> where = null,
        Expression<Func<T, object>> returning = null, CancellationToken token = default) =>
        dbConn.Exec(dbCmd => dbCmd.ConvertToListAsync<T>(
            dbCmd.PrepareUpdateOnlyReturning(updateFields, dbCmd.CreateQuery<T>().Where(where), returning), token));

    public static Task<List<T>> UpdateOnlyReturningAsync<T>(this IDbConnection dbConn,
        Expression<Func<T>> updateFields, SqlExpression<T> q,
        Expression<Func<T, object>> returning = null, CancellationToken token = default) =>
        dbConn.Exec(dbCmd => dbCmd.ConvertToListAsync<T>(dbCmd.PrepareUpdateOnlyReturning(updateFields, q, returning), token));

    public static Task<List<T>> DeleteReturningAsync<T>(this IDbConnection dbConn,
        Expression<Func<T, bool>> where, Expression<Func<T, object>> returning = null, CancellationToken token = default) =>
        dbConn.Exec(dbCmd => dbCmd.ConvertToListAsync<T>(
            dbCmd.PrepareDeleteReturning(dbCmd.CreateQuery<T>().Where(where), returning), token));

    public static Task<List<T>> DeleteReturningAsync<T>(this IDbConnection dbConn,
        SqlExpression<T> q, Expression<Func<T, object>> returning = null, CancellationToken token = default) =>
        dbConn.Exec(dbCmd => dbCmd.ConvertToListAsync<T>(dbCmd.PrepareDeleteReturning(q, returning), token));

    /// <summary>
    /// The fields selected by a returning expression, e.g. x =&gt; new { x.Id, x.Status }, or null to return all fields
    /// </summary>
    internal static List<FieldDefinition> GetReturnFields<T>(Expression<Func<T, object>> returning)
    {
        if (returning == null)
            return null;

        var modelDef = ModelDefinition<T>.Definition;
        var fieldNames = returning.GetFieldNames();
        if (fieldNames.Length == 0)
            throw new ArgumentException("Expected columns to return like: x => new { x.Id, x.Status }", nameof(returning));
        return fieldNames.Map(modelDef.AssertFieldDefinition);
    }

    internal static string PrepareUpdateOnlyReturning<T>(this IDbCommand dbCmd, Expression<Func<T>> updateFields, SqlExpression<T> q,
        Expression<Func<T, object>> returning = null)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
        if (q == null)
            throw new ArgumentNullException(nameof(q));

        var returnFields = GetReturnFields(returning);
        dbCmd.InitUpdateOnly(updateFields, q); // sets the UPDATE statement and its params
        return dbCmd.GetDialectProvider().ToReturningStatement(dbCmd.CommandText, ModelDefinition<T>.Definition, isDelete: false, returnFields);
    }

    internal static string PrepareDeleteReturning<T>(this IDbCommand dbCmd, SqlExpression<T> q,
        Expression<Func<T, object>> returning = null)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
        if (q == null)
            throw new ArgumentNullException(nameof(q));

        var returnFields = GetReturnFields(returning);
        var sql = q.ToDeleteRowStatement();
        dbCmd.SetParameters(q.Params);
        return dbCmd.GetDialectProvider().ToReturningStatement(sql, ModelDefinition<T>.Definition, isDelete: true, returnFields);
    }
}
