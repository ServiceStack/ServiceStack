// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

internal static class ReadExpressionCommandExtensionsAsync
{
    internal static Task<List<Into>> SelectAsync<Into, From>(this IDbCommand dbCmd, SqlExpression<From> q, CancellationToken token)
    {
        string sql = q.SelectInto<Into>(QueryType.Select);
        return dbCmd.ExprConvertToListAsync<Into>(sql, q.Params, q.OnlyFields, token);
    }

    internal static Task<List<T>> SelectAsync<T>(this IDbCommand dbCmd, SqlExpression<T> q, CancellationToken token)
    {
        string sql = q.SelectInto<T>(QueryType.Select);
        return dbCmd.ExprConvertToListAsync<T>(sql, q.Params, q.OnlyFields, token);
    }

    internal static Task<List<T>> SelectAsync<T>(this IDbCommand dbCmd, Expression<Func<T, bool>> predicate, CancellationToken token)
    {
        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        string sql = q.Where(predicate).SelectInto<T>(QueryType.Select);

        return dbCmd.ExprConvertToListAsync<T>(sql, q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2>>> SelectMultiAsync<T, T2>(this IDbCommand dbCmd, SqlExpression<T> q, CancellationToken token)
    {
        q.Select(q.CreateMultiSelect<T, T2, EOT, EOT, EOT, EOT, EOT, EOT>(dbCmd.GetDialectProvider()));
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2>>(q.ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3>>> SelectMultiAsync<T, T2, T3>(this IDbCommand dbCmd, SqlExpression<T> q, CancellationToken token)
    {
        q.Select(q.CreateMultiSelect<T, T2, T3, EOT, EOT, EOT, EOT, EOT>(dbCmd.GetDialectProvider()));
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3>>(q.ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3, T4>>> SelectMultiAsync<T, T2, T3, T4>(this IDbCommand dbCmd, SqlExpression<T> q, CancellationToken token)
    {
        q.Select(q.CreateMultiSelect<T, T2, T3, T4, EOT, EOT, EOT, EOT>(dbCmd.GetDialectProvider()));
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3, T4>>(q.ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3, T4, T5>>> SelectMultiAsync<T, T2, T3, T4, T5>(this IDbCommand dbCmd, SqlExpression<T> q, CancellationToken token)
    {
        q.Select(q.CreateMultiSelect<T, T2, T3, T4, T5, EOT, EOT, EOT>(dbCmd.GetDialectProvider()));
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3, T4, T5>>(q.ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3, T4, T5, T6>>> SelectMultiAsync<T, T2, T3, T4, T5, T6>(this IDbCommand dbCmd, SqlExpression<T> q, CancellationToken token)
    {
        q.Select(q.CreateMultiSelect<T, T2, T3, T4, T5, T6, EOT, EOT>(dbCmd.GetDialectProvider()));
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3, T4, T5, T6>>(q.ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3, T4, T5, T6, T7>>> SelectMultiAsync<T, T2, T3, T4, T5, T6, T7>(this IDbCommand dbCmd, SqlExpression<T> q, CancellationToken token)
    {
        q.Select(q.CreateMultiSelect<T, T2, T3, T4, T5, T6, T7, EOT>(dbCmd.GetDialectProvider()));
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3, T4, T5, T6, T7>>(q.ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3, T4, T5, T6, T7, T8>>> SelectMultiAsync<T, T2, T3, T4, T5, T6, T7, T8>(this IDbCommand dbCmd, SqlExpression<T> q, CancellationToken token)
    {
        q.Select(q.CreateMultiSelect<T, T2, T3, T4, T5, T6, T7, T8>(dbCmd.GetDialectProvider()));
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3, T4, T5, T6, T7, T8>>(q.ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }


    internal static Task<List<Tuple<T, T2>>> SelectMultiAsync<T, T2>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects, CancellationToken token)
    {
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3>>> SelectMultiAsync<T, T2, T3>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects, CancellationToken token)
    {
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3, T4>>> SelectMultiAsync<T, T2, T3, T4>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects, CancellationToken token)
    {
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3, T4>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3, T4, T5>>> SelectMultiAsync<T, T2, T3, T4, T5>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects, CancellationToken token)
    {
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3, T4, T5>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3, T4, T5, T6>>> SelectMultiAsync<T, T2, T3, T4, T5, T6>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects, CancellationToken token)
    {
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3, T4, T5, T6>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3, T4, T5, T6, T7>>> SelectMultiAsync<T, T2, T3, T4, T5, T6, T7>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects, CancellationToken token)
    {
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3, T4, T5, T6, T7>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }

    internal static Task<List<Tuple<T, T2, T3, T4, T5, T6, T7, T8>>> SelectMultiAsync<T, T2, T3, T4, T5, T6, T7, T8>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects, CancellationToken token)
    {
        return dbCmd.ExprConvertToListAsync<Tuple<T, T2, T3, T4, T5, T6, T7, T8>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, q.OnlyFields, token);
    }


    internal static Task<T> SingleAsync<T>(this IDbCommand dbCmd, Expression<Func<T, bool>> predicate, CancellationToken token)
    {
        var q = dbCmd.GetDialectProvider().SqlExpression<T>();

        return SingleAsync(dbCmd, q.Where(predicate), token);
    }

    internal static Task<T> SingleAsync<T>(this IDbCommand dbCmd, SqlExpression<T> expression, CancellationToken token)
    {
        string sql = expression.Limit(1).SelectInto<T>(QueryType.Single);

        return dbCmd.ExprConvertToAsync<T>(sql, expression.Params, token);
    }

    public static Task<TKey> ScalarAsync<T, TKey>(this IDbCommand dbCmd, Expression<Func<T, object>> field, CancellationToken token)
    {
        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        q.Select(field);
        var sql = q.SelectInto<T>(QueryType.Scalar);
        return dbCmd.ScalarAsync<TKey>(sql, q.Params, token);
    }

    internal static Task<TKey> ScalarAsync<T, TKey>(this IDbCommand dbCmd,
        Expression<Func<T, object>> field, Expression<Func<T, bool>> predicate, CancellationToken token)
    {
        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        q.Select(field).Where(predicate);
        string sql = q.SelectInto<T>(QueryType.Scalar);
        return dbCmd.ScalarAsync<TKey>(sql, q.Params, token);
    }

    internal static Task<long> CountAsync<T>(this IDbCommand dbCmd, CancellationToken token)
    {
        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        var sql = q.ToCountStatement();
        return GetCountAsync(dbCmd, sql, q.Params, token);
    }

    internal static Task<long> CountAsync<T>(this IDbCommand dbCmd, SqlExpression<T> q, CancellationToken token)
    {
        var sql = q.ToCountStatement();
        return GetCountAsync(dbCmd, sql, q.Params, token);
    }

    internal static Task<long> CountAsync<T>(this IDbCommand dbCmd, Expression<Func<T, bool>> predicate, CancellationToken token)
    {
        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        q.Where(predicate);
        var sql = q.ToCountStatement();
        return GetCountAsync(dbCmd, sql, q.Params, token);
    }

    internal static async Task<long> GetCountAsync(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token)
    {
        var ret = await dbCmd.ColumnAsync<long>(sql, sqlParams, token).ConfigAwait();
        return ret.Sum();
    }

    internal static Task<long> RowCountAsync<T>(this IDbCommand dbCmd, SqlExpression<T> expression, CancellationToken token)
    {
        var countExpr = expression.Clone().OrderBy();
        return dbCmd.ScalarAsync<long>(dbCmd.GetDialectProvider().ToRowCountStatement(countExpr.ToSelectStatement(QueryType.Scalar)), countExpr.Params, token);
    }

    internal static Task<long> RowCountAsync(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        if (anonType != null)
            dbCmd.SetParameters(anonType.ToObjectDictionary(), excludeDefaults: false, sql:ref sql);

        return dbCmd.ScalarAsync<long>(dbCmd.GetDialectProvider().ToRowCountStatement(sql), token);
    }

    internal static Task<List<T>> LoadSelectAsync<T>(this IDbCommand dbCmd, SqlExpression<T> expression = null, IEnumerable<string> include = null, CancellationToken token = default)
    {
        return dbCmd.LoadListWithReferences<T, T>(expression, include, token);
    }

    internal static Task<List<Into>> LoadSelectAsync<Into, From>(this IDbCommand dbCmd, SqlExpression<From> expression, IEnumerable<string> include = null, CancellationToken token = default)
    {
        return dbCmd.LoadListWithReferences<Into, From>(expression, include, token);
    }

    internal static Task<List<T>> LoadSelectAsync<T>(this IDbCommand dbCmd, Expression<Func<T, bool>> predicate, IEnumerable<string> include = null, CancellationToken token = default)
    {
        var expr = dbCmd.GetDialectProvider().SqlExpression<T>().Where(predicate);
        return dbCmd.LoadListWithReferences<T, T>(expr, include, token);
    }

    internal static Task<T> ExprConvertToAsync<T>(this IDataReader dataReader, IOrmLiteDialectProvider dialectProvider, CancellationToken token)
    {
        return dialectProvider.ReaderRead(dataReader,
            () => dataReader.ConvertTo<T>(dialectProvider), token);
    }

    internal static Task<List<T>> Select<T>(this IDbCommand dbCmd, Expression<Func<T, bool>> predicate, CancellationToken token)
    {
        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        string sql = q.Where(predicate).SelectInto<T>(QueryType.Select);

        return dbCmd.ExprConvertToListAsync<T>(sql, q.Params, q.OnlyFields, token);
    }

        
    internal static async Task<DataTable> GetSchemaTableAsync(this IDbCommand dbCmd, string sql, CancellationToken token)
    {
        using var reader = await dbCmd.ExecReaderAsync(sql, token).ConfigAwait();
        return reader.GetSchemaTable();
    }

    public static Task<ColumnSchema[]> GetTableColumnsAsync(this IDbCommand dbCmd, Type table, CancellationToken token) => 
        dbCmd.GetTableColumnsAsync($"SELECT * FROM {dbCmd.GetDialectProvider().GetQuotedTableName(table.GetModelDefinition())}", token);

    public static async Task<ColumnSchema[]> GetTableColumnsAsync(this IDbCommand dbCmd, string sql, CancellationToken token) => 
        (await dbCmd.GetSchemaTableAsync(sql, token).ConfigAwait()).ToColumnSchemas(dbCmd);
}