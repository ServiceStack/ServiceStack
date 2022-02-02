#if ASYNC
// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite.Legacy
{
    [Obsolete(Messages.LegacyApi)]
    internal static class OrmLiteReadCommandExtensionsAsyncLegacy
    {
        internal static Task<T> SingleFmtAsync<T>(this IDbCommand dbCmd, CancellationToken token, string filter, params object[] filterParams)
        {
            return dbCmd.ConvertToAsync<T>(dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), filter, filterParams), token);
        }

        internal static Task<List<T>> SelectFmtAsync<T>(this IDbCommand dbCmd, CancellationToken token, string sqlFilter, params object[] filterParams)
        {
            return dbCmd.ConvertToListAsync<T>(
                dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sqlFilter, filterParams), token);
        }

        internal static Task<List<TModel>> SelectFmtAsync<TModel>(this IDbCommand dbCmd, CancellationToken token, Type fromTableType, string sqlFilter, params object[] filterParams)
        {
            var sql = OrmLiteReadCommandExtensionsLegacy.ToSelectFmt<TModel>(dbCmd.GetDialectProvider(), fromTableType, sqlFilter, filterParams);
            return dbCmd.ConvertToListAsync<TModel>(sql, token);
        }

        internal static Task<T> ScalarFmtAsync<T>(this IDbCommand dbCmd, CancellationToken token, string sql, params object[] sqlParams)
        {
            return dbCmd.ScalarAsync<T>(sql.SqlFmt(sqlParams), token);
        }

        internal static Task<List<T>> ColumnFmtAsync<T>(this IDbCommand dbCmd, CancellationToken token, string sql, params object[] sqlParams)
        {
            return dbCmd.ColumnAsync<T>(sql.SqlFmt(sqlParams), token);
        }

        internal static Task<HashSet<T>> ColumnDistinctFmtAsync<T>(this IDbCommand dbCmd, CancellationToken token, string sql, params object[] sqlParams)
        {
            return dbCmd.ColumnDistinctAsync<T>(sql.SqlFmt(sqlParams), token);
        }

        internal static Task<Dictionary<K, List<V>>> LookupFmtAsync<K, V>(this IDbCommand dbCmd, CancellationToken token, string sql, params object[] sqlParams)
        {
            return dbCmd.LookupAsync<K, V>(sql.SqlFmt(sqlParams), token);
        }

        internal static Task<Dictionary<K, V>> DictionaryFmtAsync<K, V>(this IDbCommand dbCmd, CancellationToken token, string sqlFormat, params object[] sqlParams)
        {
            return dbCmd.DictionaryAsync<K, V>(sqlFormat.SqlFmt(sqlParams), token);
        }

        internal static Task<bool> ExistsFmtAsync<T>(this IDbCommand dbCmd, CancellationToken token, string sqlFilter, params object[] filterParams)
        {
            var fromTableType = typeof(T);
            return dbCmd.ScalarAsync(dbCmd.GetDialectProvider().ToSelectStatement(fromTableType, sqlFilter, filterParams), token)
                .Then(x => x != null);
        }

        internal static Task<int> DeleteFmtAsync<T>(this IDbCommand dbCmd, CancellationToken token, string sqlFilter, params object[] filterParams)
        {
            return DeleteFmtAsync(dbCmd, token, typeof(T), sqlFilter, filterParams);
        }

        internal static Task<int> DeleteFmtAsync(this IDbCommand dbCmd, CancellationToken token, Type tableType, string sqlFilter, params object[] filterParams)
        {
            var dialectProvider = dbCmd.GetDialectProvider();
            return dbCmd.ExecuteSqlAsync(dialectProvider.ToDeleteStatement(tableType, sqlFilter, filterParams), token);
        }
    }
}

#endif