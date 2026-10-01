using System;
using System.Collections.Generic;
using System.Data;

namespace ServiceStack.OrmLite.Legacy
{
    [Obsolete(Messages.LegacyApi)]
    internal static class ReadExpressionCommandExtensionsLegacy
    {
        [Obsolete("Use db.Select(db.From<T>())")]
        internal static List<T> Select<T>(this IDbCommand dbCmd, Func<SqlExpression<T>, SqlExpression<T>> expression)
        {
            var q = dbCmd.CreateQuery<T>();
            string sql = expression(q).SelectInto<T>(QueryType.Select);

            return dbCmd.ExprConvertToList<T>(sql, q.Params, onlyFields: q.OnlyFields);
        }

        [Obsolete("Use db.Select<Into,From>(db.From<From>())")]
        internal static List<Into> Select<Into, From>(this IDbCommand dbCmd, Func<SqlExpression<From>, SqlExpression<From>> expression)
        {
            var q = dbCmd.CreateQuery<From>();
            string sql = expression(q).SelectInto<Into>(QueryType.Select);

            return dbCmd.ExprConvertToList<Into>(sql, q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Into> Select<Into, From>(this IDbCommand dbCmd, SqlExpression<From> q)
        {
            string sql = q.SelectInto<Into>(QueryType.Select);
            return dbCmd.ExprConvertToList<Into>(sql, q.Params, onlyFields: q.OnlyFields);
        }

        [Obsolete("Use db.Single(db.From<T>())")]
        internal static T Single<T>(this IDbCommand dbCmd, Func<SqlExpression<T>, SqlExpression<T>> expression)
        {
            var q = dbCmd.CreateQuery<T>();
            return dbCmd.Single(expression(q));
        }

        [Obsolete("Use db.Count(db.From<T>())")]
        internal static long Count<T>(this IDbCommand dbCmd, Func<SqlExpression<T>, SqlExpression<T>> expression)
        {
            var q = dbCmd.CreateQuery<T>();
            var sql = expression(q).ToCountStatement();
            return dbCmd.GetCount(sql, q.Params);
        }

        [Obsolete("Use db.LoadSelect(db.From<T>())")]
        internal static List<T> LoadSelect<T>(this IDbCommand dbCmd, Func<SqlExpression<T>, SqlExpression<T>> expression, IEnumerable<string> include = null)
        {
            var expr = dbCmd.CreateQuery<T>();
            expr = expression(expr);
            return dbCmd.LoadListWithReferences<T, T>(expr, include);
        }
    }
}