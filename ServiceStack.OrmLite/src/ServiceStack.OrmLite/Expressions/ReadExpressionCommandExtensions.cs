using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    internal static class ReadExpressionCommandExtensions
    {
        internal static List<T> Select<T>(this IDbCommand dbCmd, SqlExpression<T> q)
        {
            string sql = q.SelectInto<T>(QueryType.Select);
            return dbCmd.ExprConvertToList<T>(sql, q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<T> Select<T>(this IDbCommand dbCmd, Expression<Func<T, bool>> predicate)
        {
            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            string sql = q.Where(predicate).SelectInto<T>(QueryType.Select);

            return dbCmd.ExprConvertToList<T>(sql, q.Params);
        }

        internal static List<Tuple<T, T2>> SelectMulti<T, T2>(this IDbCommand dbCmd, SqlExpression<T> q)
        {
            q.SelectIfDistinct(q.CreateMultiSelect<T, T2, EOT, EOT, EOT, EOT, EOT, EOT>(dbCmd.GetDialectProvider()));
            return dbCmd.ExprConvertToList<Tuple<T, T2>>(q.ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3>> SelectMulti<T, T2, T3>(this IDbCommand dbCmd, SqlExpression<T> q)
        {
            q.SelectIfDistinct(q.CreateMultiSelect<T, T2, T3, EOT, EOT, EOT, EOT, EOT>(dbCmd.GetDialectProvider()));
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3>>(q.ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3, T4>> SelectMulti<T, T2, T3, T4>(this IDbCommand dbCmd, SqlExpression<T> q)
        {
            q.SelectIfDistinct(q.CreateMultiSelect<T, T2, T3, T4, EOT, EOT, EOT, EOT>(dbCmd.GetDialectProvider()));
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3, T4>>(q.ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3, T4, T5>> SelectMulti<T, T2, T3, T4, T5>(this IDbCommand dbCmd, SqlExpression<T> q)
        {
            q.SelectIfDistinct(q.CreateMultiSelect<T, T2, T3, T4, T5, EOT, EOT, EOT>(dbCmd.GetDialectProvider()));
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3, T4, T5>>(q.ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3, T4, T5, T6>> SelectMulti<T, T2, T3, T4, T5, T6>(this IDbCommand dbCmd, SqlExpression<T> q)
        {
            q.SelectIfDistinct(q.CreateMultiSelect<T, T2, T3, T4, T5, T6, EOT, EOT>(dbCmd.GetDialectProvider()));
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3, T4, T5, T6>>(q.ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3, T4, T5, T6, T7>> SelectMulti<T, T2, T3, T4, T5, T6, T7>(this IDbCommand dbCmd, SqlExpression<T> q)
        {
            q.SelectIfDistinct(q.CreateMultiSelect<T, T2, T3, T4, T5, T6, T7, EOT>(dbCmd.GetDialectProvider()));
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3, T4, T5, T6, T7>>(q.ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3, T4, T5, T6, T7, T8>> SelectMulti<T, T2, T3, T4, T5, T6, T7, T8>(this IDbCommand dbCmd, SqlExpression<T> q)
        {
            q.SelectIfDistinct(q.CreateMultiSelect<T, T2, T3, T4, T5, T6, T7, T8>(dbCmd.GetDialectProvider()));
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3, T4, T5, T6, T7, T8>>(q.ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static string CreateMultiSelect<T, T2, T3, T4, T5, T6, T7, T8>(this SqlExpression<T> q, IOrmLiteDialectProvider dialectProvider)
        {
            var sb = StringBuilderCache.Allocate()
                .Append($"{dialectProvider.GetQuotedTableName(typeof(T).GetModelDefinition())}.*, {Sql.EOT}");

            if (typeof(T2) != typeof(EOT))
                sb.Append($", {dialectProvider.GetQuotedTableName(typeof(T2).GetModelDefinition())}.*, {Sql.EOT}");
            if (typeof(T3) != typeof(EOT))
                sb.Append($", {dialectProvider.GetQuotedTableName(typeof(T3).GetModelDefinition())}.*, {Sql.EOT}");
            if (typeof(T4) != typeof(EOT))
                sb.Append($", {dialectProvider.GetQuotedTableName(typeof(T4).GetModelDefinition())}.*, {Sql.EOT}");
            if (typeof(T5) != typeof(EOT))
                sb.Append($", {dialectProvider.GetQuotedTableName(typeof(T5).GetModelDefinition())}.*, {Sql.EOT}");
            if (typeof(T6) != typeof(EOT))
                sb.Append($", {dialectProvider.GetQuotedTableName(typeof(T6).GetModelDefinition())}.*, {Sql.EOT}");
            if (typeof(T7) != typeof(EOT))
                sb.Append($", {dialectProvider.GetQuotedTableName(typeof(T7).GetModelDefinition())}.*, {Sql.EOT}");
            if (typeof(T8) != typeof(EOT))
                sb.Append($", {dialectProvider.GetQuotedTableName(typeof(T8).GetModelDefinition())}.*, {Sql.EOT}");

            return StringBuilderCache.ReturnAndFree(sb);
        }

        internal static string CreateMultiSelect(this ISqlExpression q, string[] tableSelects)
        {
            var sb = StringBuilderCache.Allocate();

            foreach (var tableSelect in tableSelects)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append($"{tableSelect}, {Sql.EOT}");
            }

            return StringBuilderCache.ReturnAndFree(sb);
        }

        internal static List<Tuple<T, T2>> SelectMulti<T, T2>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects)
        {
            return dbCmd.ExprConvertToList<Tuple<T, T2>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3>> SelectMulti<T, T2, T3>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects)
        {
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3, T4>> SelectMulti<T, T2, T3, T4>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects)
        {
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3, T4>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3, T4, T5>> SelectMulti<T, T2, T3, T4, T5>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects)
        {
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3, T4, T5>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3, T4, T5, T6>> SelectMulti<T, T2, T3, T4, T5, T6>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects)
        {
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3, T4, T5, T6>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3, T4, T5, T6, T7>> SelectMulti<T, T2, T3, T4, T5, T6, T7>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects)
        {
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3, T4, T5, T6, T7>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static List<Tuple<T, T2, T3, T4, T5, T6, T7, T8>> SelectMulti<T, T2, T3, T4, T5, T6, T7, T8>(this IDbCommand dbCmd, SqlExpression<T> q, string[] tableSelects)
        {
            return dbCmd.ExprConvertToList<Tuple<T, T2, T3, T4, T5, T6, T7, T8>>(q.Select(q.CreateMultiSelect(tableSelects)).ToSelectStatement(QueryType.Select), q.Params, onlyFields: q.OnlyFields);
        }

        internal static T Single<T>(this IDbCommand dbCmd, Expression<Func<T, bool>> predicate)
        {
            var q = dbCmd.GetDialectProvider().SqlExpression<T>();

            return Single(dbCmd, q.Where(predicate));
        }

        internal static T Single<T>(this IDbCommand dbCmd, SqlExpression<T> q)
        {
            string sql = q.SelectInto<T>(QueryType.Single);

            return dbCmd.ExprConvertTo<T>(sql, q.Params, onlyFields:q.OnlyFields);
        }

        public static TKey Scalar<T, TKey>(this IDbCommand dbCmd, SqlExpression<T> expression)
        {
            var sql = expression.SelectInto<T>(QueryType.Scalar);
            return dbCmd.Scalar<TKey>(sql, expression.Params);
        }

        public static TKey Scalar<T, TKey>(this IDbCommand dbCmd, Expression<Func<T, object>> field)
        {
            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            q.Select(field);
            var sql = q.SelectInto<T>(QueryType.Scalar);
            return dbCmd.Scalar<TKey>(sql, q.Params);
        }

        internal static TKey Scalar<T, TKey>(this IDbCommand dbCmd,
            Expression<Func<T, object>> field, Expression<Func<T, bool>> predicate)
        {
            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            q.Select(field).Where(predicate);
            string sql = q.SelectInto<T>(QueryType.Scalar);
            return dbCmd.Scalar<TKey>(sql, q.Params);
        }

        internal static long Count<T>(this IDbCommand dbCmd)
        {
            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            var sql = q.ToCountStatement();
            return GetCount(dbCmd, sql, q.Params);
        }

        internal static long Count<T>(this IDbCommand dbCmd, SqlExpression<T> expression)
        {
            var sql = expression.ToCountStatement();
            return GetCount(dbCmd, sql, expression.Params);
        }

        internal static long Count<T>(this IDbCommand dbCmd, Expression<Func<T, bool>> predicate)
        {
            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            q.Where(predicate);
            var sql = q.ToCountStatement();
            return GetCount(dbCmd, sql, q.Params);
        }

        internal static long GetCount(this IDbCommand dbCmd, string sql)
        {
            return dbCmd.Column<long>(sql).Sum();
        }

        internal static long GetCount(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
        {
            return dbCmd.Column<long>(sql, sqlParams).Sum();
        }

        internal static long RowCount<T>(this IDbCommand dbCmd, SqlExpression<T> expression)
        {
            //ORDER BY throws when used in sub selects in SQL Server. Removing OrderBy() clause since it doesn't impact results
            var countExpr = expression.Clone().OrderBy(); 
            return dbCmd.Scalar<long>(dbCmd.GetDialectProvider().ToRowCountStatement(countExpr.ToSelectStatement(QueryType.Scalar)), countExpr.Params);
        }

        internal static long RowCount(this IDbCommand dbCmd, string sql, object anonType)
        {
            if (anonType != null)
                dbCmd.SetParameters(anonType.ToObjectDictionary(), excludeDefaults: false, sql:ref sql);

            return dbCmd.Scalar<long>(dbCmd.GetDialectProvider().ToRowCountStatement(sql));
        }

        internal static long RowCount<T>(this IDbCommand dbCmd)
        {
            var quotedTableName = dbCmd.GetDialectProvider().GetQuotedTableName(typeof(T).GetModelDefinition());
            return dbCmd.Scalar<long>(dbCmd.GetDialectProvider().ToRowCountStatement(quotedTableName,false));
        }

        internal static long RowCount(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
        {
            return dbCmd.SetParameters(sqlParams).Scalar<long>(dbCmd.GetDialectProvider().ToRowCountStatement(sql));
        }

        internal static List<T> LoadSelect<T>(this IDbCommand dbCmd, SqlExpression<T> expression = null, IEnumerable<string> include = null)
        {
            return dbCmd.LoadListWithReferences<T, T>(expression, include);
        }

        internal static List<Into> LoadSelect<Into, From>(this IDbCommand dbCmd, SqlExpression<From> expression, IEnumerable<string> include = null)
        {
            return dbCmd.LoadListWithReferences<Into, From>(expression, include);
        }

        internal static List<T> LoadSelect<T>(this IDbCommand dbCmd, Expression<Func<T, bool>> predicate, IEnumerable<string> include = null)
        {
            var expr = dbCmd.GetDialectProvider().SqlExpression<T>().Where(predicate);
            return dbCmd.LoadListWithReferences<T, T>(expr, include);
        }

        internal static DataTable GetSchemaTable(this IDbCommand dbCmd, string sql)
        {
            using var reader = dbCmd.ExecReader(sql, CommandBehavior.KeyInfo);
            return reader.GetSchemaTable();
        }

        public static ColumnSchema[] GetTableColumns(this IDbCommand dbCmd, Type table) => 
            dbCmd.GetTableColumns($"SELECT * FROM {dbCmd.GetDialectProvider().GetQuotedTableName(table.GetModelDefinition())}");

        public static ColumnSchema[] GetTableColumns(this IDbCommand dbCmd, string sql) => 
            dbCmd.GetSchemaTable(sql).ToColumnSchemas(dbCmd);

        internal static ColumnSchema[] ToColumnSchemas(this DataTable dt, IDbCommand dbCmd)
        {
            var ret = new List<ColumnSchema>();
            foreach (DataRow row in dt.Rows)
            {
                var obj = new Dictionary<string, object>();
                foreach (DataColumn column in dt.Columns)
                {
                    obj[column.ColumnName] = row[column.Ordinal];
                }

                var to = obj.FromObjectDictionary<ColumnSchema>();
                //MySQL doesn't populate DataTypeName, so reverse populate it from Type Converter ColumnDefinition
                if (to.DataTypeName == null && to.DataType != null)
                    to.DataTypeName = dbCmd.GetDialectProvider().GetConverter(to.DataType)?.ColumnDefinition.LeftPart('(');
                if (to.DataTypeName == null)
                    continue;
                
                ret.Add(to);
            }

            return ret.ToArray();
        }
    }
}

