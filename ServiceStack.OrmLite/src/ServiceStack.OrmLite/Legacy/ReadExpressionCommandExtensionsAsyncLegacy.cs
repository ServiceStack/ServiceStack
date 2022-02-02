#if ASYNC
// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite.Legacy
{
    [Obsolete(Messages.LegacyApi)]
    internal static class ReadExpressionCommandExtensionsAsyncLegacy
    {
        [Obsolete("Use db.SelectAsync(db.From<T>())")]
        internal static Task<List<T>> SelectAsync<T>(this IDbCommand dbCmd, Func<SqlExpression<T>, SqlExpression<T>> expression, CancellationToken token)
        {
            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            var sql = expression(q).SelectInto<T>(QueryType.Select);
            return dbCmd.ExprConvertToListAsync<T>(sql, q.Params, q.OnlyFields, token);
        }

        [Obsolete("Use db.SelectAsync(db.From<T>())")]
        internal static Task<List<Into>> SelectAsync<Into, From>(this IDbCommand dbCmd, Func<SqlExpression<From>, SqlExpression<From>> expression, CancellationToken token)
        {
            var q = dbCmd.GetDialectProvider().SqlExpression<From>();
            string sql = expression(q).SelectInto<Into>(QueryType.Select);
            return dbCmd.ExprConvertToListAsync<Into>(sql, q.Params, q.OnlyFields, token);
        }

        [Obsolete("Use db.SingleAsync(db.From<T>())")]
        internal static Task<T> SingleAsync<T>(this IDbCommand dbCmd, Func<SqlExpression<T>, SqlExpression<T>> expression, CancellationToken token)
        {
            var expr = dbCmd.GetDialectProvider().SqlExpression<T>();
            return dbCmd.SingleAsync(expression(expr), token);
        }

        [Obsolete("Use db.CountAsync(db.From<T>())")]
        internal static Task<long> CountAsync<T>(this IDbCommand dbCmd, Func<SqlExpression<T>, SqlExpression<T>> expression, CancellationToken token)
        {
            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            var sql = expression(q).ToCountStatement();
            return dbCmd.GetCountAsync(sql, q.Params, token);
        }

        [Obsolete("Use db.LoadSelectAsync(db.From<T>())")]
        internal static Task<List<T>> LoadSelectAsync<T>(this IDbCommand dbCmd, Func<SqlExpression<T>, SqlExpression<T>> expression, string[] include = null, CancellationToken token = default)
        {
            var expr = dbCmd.GetDialectProvider().SqlExpression<T>();
            expr = expression(expr);
            return dbCmd.LoadListWithReferences<T, T>(expr, include, token);
        }
    }
}

#endif