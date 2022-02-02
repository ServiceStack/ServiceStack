// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite
{
    public static class OrmLiteResultsFilterExtensions
    {
        internal static ILog Log = LogManager.GetLogger(typeof(OrmLiteResultsFilterExtensions));

        public static int ExecNonQuery(this IDbCommand dbCmd, string sql, object anonType = null)
        {
            if (anonType != null)
                dbCmd.SetParameters(anonType.ToObjectDictionary(), (bool)false, sql:ref sql);

            dbCmd.CommandText = sql;

            if (Log.IsDebugEnabled)
                Log.DebugCommand(dbCmd);

            OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.ExecuteSql(dbCmd);

            return dbCmd.ExecuteNonQuery();
        }

        public static int ExecNonQuery(this IDbCommand dbCmd, string sql, Dictionary<string, object> dict)
        {

            if (dict != null)
                dbCmd.SetParameters(dict, (bool)false, sql:ref sql);

            dbCmd.CommandText = sql;

            if (Log.IsDebugEnabled)
                Log.DebugCommand(dbCmd);

            OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.ExecuteSql(dbCmd);

            return dbCmd.ExecuteNonQuery();
        }

        public static int ExecNonQuery(this IDbCommand dbCmd)
        {
            OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.ExecuteSql(dbCmd);

            if (Log.IsDebugEnabled)
                Log.DebugCommand(dbCmd);

            return dbCmd.ExecuteNonQuery();
        }

        public static int ExecNonQuery(this IDbCommand dbCmd, string sql, Action<IDbCommand> dbCmdFilter)
        {
            dbCmdFilter?.Invoke(dbCmd);

            dbCmd.CommandText = sql;

            OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.ExecuteSql(dbCmd);

            if (Log.IsDebugEnabled)
                Log.DebugCommand(dbCmd);

            return dbCmd.ExecuteNonQuery();
        }

        public static List<T> ConvertToList<T>(this IDbCommand dbCmd, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            var isScalar = OrmLiteUtils.IsScalar<T>();

            if (OrmLiteConfig.ResultsFilter != null)
            {
                return isScalar
                    ? OrmLiteConfig.ResultsFilter.GetColumn<T>(dbCmd)
                    : OrmLiteConfig.ResultsFilter.GetList<T>(dbCmd);
            }

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return isScalar
                ? reader.Column<T>(dbCmd.GetDialectProvider())
                : reader.ConvertToList<T>(dbCmd.GetDialectProvider());
        }

        public static IList ConvertToList(this IDbCommand dbCmd, Type refType, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetRefList(dbCmd, refType);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.ConvertToList(dbCmd.GetDialectProvider(), refType);
        }

        public static IDbDataParameter PopulateWith(this IDbDataParameter to, IDbDataParameter from)
        {
            to.ParameterName = from.ParameterName;
            to.DbType = from.DbType;
            to.Value = from.Value;

            if (from.Precision != default(byte))
                to.Precision = from.Precision;
            if (from.Scale != default(byte))
                to.Scale = from.Scale;
            if (from.Size != default(int))
                to.Size = from.Size;

            return to;
        }

        internal static List<T> ExprConvertToList<T>(this IDbCommand dbCmd, string sql = null, IEnumerable<IDbDataParameter> sqlParams = null, HashSet<string> onlyFields=null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            dbCmd.SetParameters(sqlParams);

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetList<T>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.ConvertToList<T>(dbCmd.GetDialectProvider(), onlyFields:onlyFields);
        }

        public static T ConvertTo<T>(this IDbCommand dbCmd, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetSingle<T>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.ConvertTo<T>(dbCmd.GetDialectProvider());
        }

        internal static object ConvertTo(this IDbCommand dbCmd, Type refType, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetRefSingle(dbCmd, refType);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.ConvertTo(dbCmd.GetDialectProvider(), refType);
        }

        internal static T Scalar<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
        {
            return dbCmd.SetParameters(sqlParams).Scalar<T>(sql);
        }

        internal static T Scalar<T>(this IDbCommand dbCmd, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetScalar<T>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.Scalar<T>(dbCmd.GetDialectProvider());
        }

        public static object Scalar(this IDbCommand dbCmd, ISqlExpression sqlExpression)
        {
            dbCmd.PopulateWith(sqlExpression, QueryType.Scalar);

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetScalar(dbCmd);

            return dbCmd.ExecuteScalar();
        }

        public static object Scalar(this IDbCommand dbCmd, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetScalar(dbCmd);

            return dbCmd.ExecuteScalar();
        }

        public static long ExecLongScalar(this IDbCommand dbCmd, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            if (Log.IsDebugEnabled)
                Log.DebugCommand(dbCmd);

            OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetLongScalar(dbCmd);

            return dbCmd.LongScalar();
        }

        internal static T ExprConvertTo<T>(this IDbCommand dbCmd, string sql = null, IEnumerable<IDbDataParameter> sqlParams = null, HashSet<string> onlyFields = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            dbCmd.SetParameters(sqlParams);

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetSingle<T>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.ConvertTo<T>(dbCmd.GetDialectProvider(), onlyFields: onlyFields);
        }

        internal static List<T> Column<T>(this IDbCommand dbCmd, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetColumn<T>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.Column<T>(dbCmd.GetDialectProvider());
        }

        internal static List<T> Column<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
        {
            return dbCmd.SetParameters(sqlParams).Column<T>(sql);
        }

        internal static HashSet<T> ColumnDistinct<T>(this IDbCommand dbCmd, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetColumnDistinct<T>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.ColumnDistinct<T>(dbCmd.GetDialectProvider());
        }

        internal static HashSet<T> ColumnDistinct<T>(this IDbCommand dbCmd, ISqlExpression expression)
        {
            dbCmd.PopulateWith(expression, QueryType.Select);

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetColumnDistinct<T>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.ColumnDistinct<T>(dbCmd.GetDialectProvider());
        }

        internal static Dictionary<K, V> Dictionary<K, V>(this IDbCommand dbCmd, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetDictionary<K, V>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.Dictionary<K, V>(dbCmd.GetDialectProvider());
        }

        internal static Dictionary<K, V> Dictionary<K, V>(this IDbCommand dbCmd, ISqlExpression expression)
        {
            dbCmd.PopulateWith(expression, QueryType.Select);

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetDictionary<K, V>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.Dictionary<K, V>(dbCmd.GetDialectProvider());
        }

        internal static List<KeyValuePair<K, V>> KeyValuePairs<K, V>(this IDbCommand dbCmd, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetKeyValuePairs<K, V>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.KeyValuePairs<K, V>(dbCmd.GetDialectProvider());
        }

        internal static List<KeyValuePair<K, V>> KeyValuePairs<K, V>(this IDbCommand dbCmd, ISqlExpression expression)
        {
            dbCmd.PopulateWith(expression, QueryType.Select);

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetKeyValuePairs<K,V>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.KeyValuePairs<K, V>(dbCmd.GetDialectProvider());
        }

        internal static Dictionary<K, List<V>> Lookup<K, V>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
        {
            return dbCmd.SetParameters(sqlParams).Lookup<K, V>(sql);
        }

        internal static Dictionary<K, List<V>> Lookup<K, V>(this IDbCommand dbCmd, string sql = null)
        {
            if (sql != null)
                dbCmd.CommandText = sql;

            if (OrmLiteConfig.ResultsFilter != null)
                return OrmLiteConfig.ResultsFilter.GetLookup<K, V>(dbCmd);

            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.Lookup<K, V>(dbCmd.GetDialectProvider());
        }
    }
}
