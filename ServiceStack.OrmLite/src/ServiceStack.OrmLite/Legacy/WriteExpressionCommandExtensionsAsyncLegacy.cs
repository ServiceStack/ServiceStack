#if ASYNC
using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite.Legacy
{
    [Obsolete(Messages.LegacyApi)]
    internal static class WriteExpressionCommandExtensionsAsyncLegacy
    {
        [Obsolete("Use db.InsertOnlyAsync(obj, db.From<T>())")]
        internal static Task InsertOnlyAsync<T>(this IDbCommand dbCmd, T obj, Func<SqlExpression<T>, SqlExpression<T>> onlyFields, CancellationToken token)
        {
            return dbCmd.InsertOnlyAsync(obj, onlyFields(dbCmd.GetDialectProvider().SqlExpression<T>()), token);
        }

        [Obsolete("Use db.UpdateOnlyAsync(model, db.From<T>())")]
        internal static Task<int> UpdateOnlyAsync<T>(this IDbCommand dbCmd, T model, Func<SqlExpression<T>, SqlExpression<T>> onlyFields, CancellationToken token)
        {
            return dbCmd.UpdateOnlyFieldsAsync(model, onlyFields(dbCmd.GetDialectProvider().SqlExpression<T>()), null, token);
        }

        internal static Task<int> UpdateFmtAsync<T>(this IDbCommand dbCmd, string set, string where, CancellationToken token)
        {
            return dbCmd.UpdateFmtAsync(typeof(T).GetModelDefinition().ModelName, set, where, token);
        }

        internal static Task<int> UpdateFmtAsync(this IDbCommand dbCmd, string table, string set, string where, CancellationToken token)
        {
            var sql = WriteExpressionCommandExtensionsLegacy.UpdateFmtSql(dbCmd.GetDialectProvider(), table, set, where);
            return dbCmd.ExecuteSqlAsync(sql, token);
        }

        [Obsolete("Use db.DeleteAsync(db.From<T>())")]
        internal static Task<int> DeleteAsync<T>(this IDbCommand dbCmd, Func<SqlExpression<T>, SqlExpression<T>> where, CancellationToken token)
        {
            return dbCmd.DeleteAsync(where(dbCmd.GetDialectProvider().SqlExpression<T>()), null, token);
        }

        internal static Task<int> DeleteFmtAsync<T>(this IDbCommand dbCmd, string where, CancellationToken token)
        {
            return dbCmd.DeleteFmtAsync(typeof(T).GetModelDefinition().ModelName, where, token);
        }

        internal static Task<int> DeleteFmtAsync(this IDbCommand dbCmd, string table, string where, CancellationToken token)
        {
            var sql = WriteExpressionCommandExtensionsLegacy.DeleteFmtSql(dbCmd.GetDialectProvider(), table, where);
            return dbCmd.ExecuteSqlAsync(sql, token);
        }

        internal static Task InsertOnlyAsync<T>(this IDbCommand dbCmd, T obj, SqlExpression<T> onlyFields, CancellationToken token)
        {
            if (OrmLiteConfig.InsertFilter != null)
                OrmLiteConfig.InsertFilter(dbCmd, obj);

            var dialectProvider = dbCmd.GetDialectProvider();
            var sql = dialectProvider.ToInsertRowStatement(dbCmd, obj, onlyFields.InsertFields);

            dialectProvider.SetParameterValues<T>(dbCmd, obj);
            
            return dbCmd.ExecuteSqlAsync(sql, token);
        }
    }
}

#endif