// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite;

public static class OrmLiteReadExpressionsApiAsync
{
    /// <summary>
    /// Returns results from using a LINQ Expression. E.g:
    /// <para>db.Select&lt;Person&gt;(x =&gt; x.Age &gt; 40)</para>
    /// </summary>
    public static Task<List<T>> SelectAsync<T>(this IDbConnection dbConn, Expression<Func<T, bool>> predicate, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectAsync(predicate, token));
    }

    /// <summary>
    /// Returns results from using an SqlExpression lambda. E.g:
    /// <para>db.Select(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40))</para>
    /// </summary>
    public static Task<List<T>> SelectAsync<T>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectAsync(expression, token));
    }

    /// <summary>
    /// Project results from a number of joined tables into a different model
    /// </summary>
    public static Task<List<Into>> SelectAsync<Into, From>(this IDbConnection dbConn, SqlExpression<From> expression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectAsync<Into, From>(expression, token));
    }

    /// <summary>
    /// Returns results from using an SqlExpression lambda. E.g:
    /// <para>db.SelectAsync(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40))</para>
    /// </summary>
    public static Task<List<T>> SelectAsync<T>(this IDbConnection dbConn, ISqlExpression expression, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SqlListAsync<T>(expression.SelectInto<T>(QueryType.Select), expression.Params, token));
    public static Task<List<Tuple<T, T2>>> SelectMultiAsync<T, T2>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2>(expression, token));
    public static Task<List<Tuple<T, T2, T3>>> SelectMultiAsync<T, T2, T3>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3>(expression, token));
    public static Task<List<Tuple<T, T2, T3, T4>>> SelectMultiAsync<T, T2, T3, T4>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3, T4>(expression, token));
    public static Task<List<Tuple<T, T2, T3, T4, T5>>> SelectMultiAsync<T, T2, T3, T4, T5>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3, T4, T5>(expression, token));
    public static Task<List<Tuple<T, T2, T3, T4, T5, T6>>> SelectMultiAsync<T, T2, T3, T4, T5, T6>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3, T4, T5, T6>(expression, token));
    public static Task<List<Tuple<T, T2, T3, T4, T5, T6, T7>>> SelectMultiAsync<T, T2, T3, T4, T5, T6, T7>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3, T4, T5, T6, T7>(expression, token));
    public static Task<List<Tuple<T, T2, T3, T4, T5, T6, T7, T8>>> SelectMultiAsync<T, T2, T3, T4, T5, T6, T7, T8>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3, T4, T5, T6, T7, T8>(expression, token));


    public static Task<List<Tuple<T, T2>>> SelectMultiAsync<T, T2>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2>(expression, tableSelects, token));

    public static Task<List<Tuple<T, T2, T3>>> SelectMultiAsync<T, T2, T3>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3>(expression, tableSelects, token));

    public static Task<List<Tuple<T, T2, T3, T4>>> SelectMultiAsync<T, T2, T3, T4>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3, T4>(expression, tableSelects, token));

    public static Task<List<Tuple<T, T2, T3, T4, T5>>> SelectMultiAsync<T, T2, T3, T4, T5>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3, T4, T5>(expression, tableSelects, token));

    public static Task<List<Tuple<T, T2, T3, T4, T5, T6>>> SelectMultiAsync<T, T2, T3, T4, T5, T6>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3, T4, T5, T6>(expression, tableSelects, token));

    public static Task<List<Tuple<T, T2, T3, T4, T5, T6, T7>>> SelectMultiAsync<T, T2, T3, T4, T5, T6, T7>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3, T4, T5, T6, T7>(expression, tableSelects, token));
    public static Task<List<Tuple<T, T2, T3, T4, T5, T6, T7, T8>>> SelectMultiAsync<T, T2, T3, T4, T5, T6, T7, T8>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects, CancellationToken token = default) => dbConn.Exec(dbCmd => dbCmd.SelectMultiAsync<T, T2, T3, T4, T5, T6, T7, T8>(expression, tableSelects, token));

    /// <summary>
    /// Returns a single result from using a LINQ Expression. E.g:
    /// <para>db.Single&lt;Person&gt;(x =&gt; x.Age == 42)</para>
    /// </summary>
    public static Task<T> SingleAsync<T>(this IDbConnection dbConn, Expression<Func<T, bool>> predicate, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SingleAsync(predicate, token));
    }

    /// <summary>
    /// Returns results from using an SqlExpression lambda. E.g:
    /// <para>db.SingleAsync&lt;Person&gt;(x =&gt; x.Age &gt; 40)</para>
    /// </summary>
    public static Task<T> SingleAsync<T>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SingleAsync(expression, token));
    }

    /// <summary>
    /// Returns results from using an SqlExpression lambda. E.g:
    /// <para>db.SingleAsync(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40))</para>
    /// </summary>
    public static Task<T> SingleAsync<T>(this IDbConnection dbConn, ISqlExpression expression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SingleAsync<T>(expression.SelectInto<T>(QueryType.Single), expression.Params, token));
    }

    /// <summary>
    /// Returns a scalar result from using an SqlExpression lambda. E.g:
    /// <para>db.Scalar&lt;Person, int&gt;(x =&gt; Sql.Max(x.Age))</para>
    /// </summary>
    public static Task<TKey> ScalarAsync<T, TKey>(this IDbConnection dbConn, Expression<Func<T, object>> field, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ScalarAsync<T, TKey>(field, token));
    }

    /// <summary>
    /// Returns a scalar result from using an SqlExpression lambda. E.g:
    /// <para>db.Scalar&lt;Person, int&gt;(x =&gt; Sql.Max(x.Age), , x =&gt; x.Age &lt; 50)</para>
    /// </summary>        
    public static Task<TKey> ScalarAsync<T, TKey>(this IDbConnection dbConn,
        Expression<Func<T, object>> field, Expression<Func<T, bool>> predicate, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ScalarAsync<T, TKey>(field, predicate, token));
    }

    /// <summary>
    /// Returns the count of rows that match the LINQ expression, E.g:
    /// <para>db.Count&lt;Person&gt;(x =&gt; x.Age &lt; 50)</para>
    /// </summary>
    public static Task<long> CountAsync<T>(this IDbConnection dbConn, Expression<Func<T, bool>> expression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.CountAsync(expression, token));
    }

    /// <summary>
    /// Returns the count of rows that match the supplied SqlExpression, E.g:
    /// <para>db.Count(db.From&lt;Person&gt;().Where(x =&gt; x.Age &lt; 50))</para>
    /// </summary>
    public static Task<long> CountAsync<T>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.CountAsync(expression, token));
    }

    public static Task<long> CountAsync<T>(this IDbConnection dbConn, CancellationToken token = default)
    {
        var expression = dbConn.GetDialectProvider().SqlExpression<T>();
        return dbConn.Exec(dbCmd => dbCmd.CountAsync(expression, token));
    }

    /// <summary>
    /// Return the number of rows returned by the supplied expression
    /// </summary>
    public static Task<long> RowCountAsync<T>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.RowCountAsync(expression, token));
    }

    /// <summary>
    /// Return the number of rows returned by the supplied sql
    /// </summary>
    public static Task<long> RowCountAsync(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.RowCountAsync(sql, anonType, token));
    }

    /// <summary>
    /// Returns results with references from using a LINQ Expression. E.g:
    /// <para>db.LoadSelectAsync&lt;Person&gt;(x =&gt; x.Age &gt; 40)</para>
    /// </summary>
    public static Task<List<T>> LoadSelectAsync<T>(this IDbConnection dbConn, Expression<Func<T, bool>> predicate, string[] include = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelectAsync(predicate, include, token));
    }

    /// <summary>
    /// Returns results with references from using a LINQ Expression. E.g:
    /// <para>db.LoadSelectAsync&lt;Person&gt;(x =&gt; x.Age &gt; 40, include: x => new { x.PrimaryAddress })</para>
    /// </summary>
    public static Task<List<T>> LoadSelectAsync<T>(this IDbConnection dbConn, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> include)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelectAsync(predicate, include.GetFieldNames()));
    }

    /// <summary>
    /// Returns results with references from using an SqlExpression lambda. E.g:
    /// <para>db.LoadSelectAsync(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40))</para>
    /// </summary>
    public static Task<List<T>> LoadSelectAsync<T>(this IDbConnection dbConn, SqlExpression<T> expression, string[] include = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelectAsync(expression, include, token));
    }

    /// <summary>
    /// Returns results with references from using an SqlExpression lambda. E.g:
    /// <para>db.LoadSelectAsync(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40), include:q.OnlyFields)</para>
    /// </summary>
    public static Task<List<T>> LoadSelectAsync<T>(this IDbConnection dbConn, SqlExpression<T> expression, IEnumerable<string> include, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelectAsync(expression, include, token));
    }

    /// <summary>
    /// Project results with references from a number of joined tables into a different model
    /// </summary>
    public static Task<List<Into>> LoadSelectAsync<Into, From>(this IDbConnection dbConn, SqlExpression<From> expression, string[] include = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelectAsync<Into, From>(expression, include, token));
    }

    /// <summary>
    /// Project results with references from a number of joined tables into a different model
    /// </summary>
    public static Task<List<Into>> LoadSelectAsync<Into, From>(this IDbConnection dbConn, SqlExpression<From> expression, IEnumerable<string> include, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelectAsync<Into, From>(expression, include, token));
    }

    /// <summary>
    /// Project results with references from a number of joined tables into a different model
    /// </summary>
    public static Task<List<Into>> LoadSelectAsync<Into, From>(this IDbConnection dbConn, SqlExpression<From> expression, Expression<Func<Into, object>> include)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelectAsync<Into, From>(expression, include.GetFieldNames()));
    }

    /// <summary>
    /// Return ADO.NET reader.GetSchemaTable() in a DataTable
    /// </summary>
    public static Task<DataTable> GetSchemaTableAsync(this IDbConnection dbConn, string sql, CancellationToken token=default) => 
        dbConn.Exec(dbCmd => dbCmd.GetSchemaTableAsync(sql, token));
        
    /// <summary>
    /// Get Table Column Schemas for specified table
    /// </summary>
    public static Task<ColumnSchema[]> GetTableColumnsAsync<T>(this IDbConnection dbConn, CancellationToken token=default) => 
        dbConn.Exec(dbCmd => dbCmd.GetTableColumnsAsync(typeof(T), token));
    /// <summary>
    /// Get Table Column Schemas for specified table
    /// </summary>
    public static Task<ColumnSchema[]> GetTableColumnsAsync(this IDbConnection dbConn, Type type, CancellationToken token=default) => 
        dbConn.Exec(dbCmd => dbCmd.GetTableColumnsAsync(type, token));
    /// <summary>
    /// Get Table Column Schemas for result-set return from specified sql
    /// </summary>
    public static Task<ColumnSchema[]> GetTableColumnsAsync(this IDbConnection dbConn, string sql, CancellationToken token=default) => 
        dbConn.Exec(dbCmd => dbCmd.GetTableColumnsAsync(sql, token));

    public static Task EnableForeignKeysCheckAsync(this IDbConnection dbConn, CancellationToken token=default) => 
        dbConn.Exec(dbCmd => dbConn.GetDialectProvider().EnableForeignKeysCheckAsync(dbCmd, token));
    public static Task DisableForeignKeysCheckAsync(this IDbConnection dbConn, CancellationToken token=default) => 
        dbConn.Exec(dbCmd => dbConn.GetDialectProvider().DisableForeignKeysCheckAsync(dbCmd, token));
}
