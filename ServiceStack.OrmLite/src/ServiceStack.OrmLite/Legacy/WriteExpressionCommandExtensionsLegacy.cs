using System;
using System.Data;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Legacy
{
    [Obsolete(Messages.LegacyApi)]
    internal static class WriteExpressionCommandExtensionsLegacy
    {
        [Obsolete("Use db.InsertOnly(obj, db.From<T>())")]
        public static void InsertOnly<T>(this IDbCommand dbCmd, T obj, Func<SqlExpression<T>, SqlExpression<T>> onlyFields)
        {
            dbCmd.InsertOnly(obj, onlyFields(dbCmd.GetDialectProvider().SqlExpression<T>()));
        }

        [Obsolete("Use db.UpdateOnly(model, db.From<T>())")]
        public static int UpdateOnly<T>(this IDbCommand dbCmd, T model, Func<SqlExpression<T>, SqlExpression<T>> onlyFields)
        {
            return dbCmd.UpdateOnlyFields(model, onlyFields(dbCmd.GetDialectProvider().SqlExpression<T>()));
        }

        public static int UpdateFmt<T>(this IDbCommand dbCmd, string set = null, string where = null)
        {
            return dbCmd.UpdateFmt(typeof(T).GetModelDefinition().ModelName, set, where);
        }

        public static int UpdateFmt(this IDbCommand dbCmd, string table = null, string set = null, string where = null)
        {
            var sql = UpdateFmtSql(dbCmd.GetDialectProvider(), table, set, @where);
            return dbCmd.ExecuteSql(sql);
        }

        internal static string UpdateFmtSql(IOrmLiteDialectProvider dialectProvider, string table, string set, string @where)
        {
            if (table == null)
                throw new ArgumentNullException("table");
            if (set == null)
                throw new ArgumentNullException("set");

            var sql = StringBuilderCache.Allocate();
            sql.Append("UPDATE ");
            sql.Append(dialectProvider.GetQuotedTableName(table));
            sql.Append(" SET ");
            sql.Append(set.SqlVerifyFragment());
            if (!string.IsNullOrEmpty(@where))
            {
                sql.Append(" WHERE ");
                sql.Append(@where.SqlVerifyFragment());
            }
            return StringBuilderCache.ReturnAndFree(sql);
        }

        internal static int DeleteFmt<T>(this IDbCommand dbCmd, string sqlFilter, params object[] filterParams)
        {
            return DeleteFmt(dbCmd, typeof(T), sqlFilter, filterParams);
        }

        internal static int DeleteFmt(this IDbCommand dbCmd, Type tableType, string sqlFilter, params object[] filterParams)
        {
            return dbCmd.ExecuteSql(dbCmd.GetDialectProvider().ToDeleteStatement(tableType, sqlFilter, filterParams));
        }

        public static int DeleteFmt<T>(this IDbCommand dbCmd, string where = null)
        {
            return dbCmd.DeleteFmt(typeof(T).GetModelDefinition().ModelName, where);
        }

        public static int DeleteFmt(this IDbCommand dbCmd, string table = null, string where = null)
        {
            var sql = DeleteFmtSql(dbCmd.GetDialectProvider(), table, @where);
            return dbCmd.ExecuteSql(sql);
        }

        internal static string DeleteFmtSql(IOrmLiteDialectProvider dialectProvider, string table, string @where)
        {
            if (table == null)
                throw new ArgumentNullException("table");
            if (@where == null)
                throw new ArgumentNullException("where");

            var sql = StringBuilderCache.Allocate();
            sql.AppendFormat("DELETE FROM {0} WHERE {1}",
                             dialectProvider.GetQuotedTableName(table),
                             @where.SqlVerifyFragment());
            return StringBuilderCache.ReturnAndFree(sql);
        }

        [Obsolete("Use db.Delete(db.From<T>())")]
        internal static int Delete<T>(this IDbCommand dbCmd, Func<SqlExpression<T>, SqlExpression<T>> where)
        {
            return dbCmd.Delete(where(dbCmd.GetDialectProvider().SqlExpression<T>()));
        }

        [Obsolete(Messages.LegacyApi)]
        public static void InsertOnly<T>(this IDbCommand dbCmd, T obj, SqlExpression<T> onlyFields)
        {
            if (OrmLiteConfig.InsertFilter != null)
                OrmLiteConfig.InsertFilter(dbCmd, obj);

            var dialectProvider = dbCmd.GetDialectProvider();
            var sql = dialectProvider.ToInsertRowStatement(dbCmd, obj, onlyFields.InsertFields);
            
            dialectProvider.SetParameterValues<T>(dbCmd, obj);

            dbCmd.ExecuteSql(sql);
        }
    }
}