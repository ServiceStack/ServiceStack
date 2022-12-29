#if ASYNC
// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite.Legacy
{
    public static class OrmLiteReadApiAsyncLegacy
    {
        /// <summary>
        /// Returns results from using an SqlFormat query. E.g:
        /// <para>db.SelectFmt&lt;Person&gt;("Age &gt; {0}", 40)</para>
        /// <para>db.SelectFmt&lt;Person&gt;("SELECT * FROM Person WHERE Age &gt; {0}", 40)</para>
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<List<T>> SelectFmtAsync<T>(this IDbConnection dbConn, CancellationToken token, string sqlFormat, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.SelectFmtAsync<T>(token, sqlFormat, filterParams));
        }
        [Obsolete(Messages.LegacyApi)]
        public static Task<List<T>> SelectFmtAsync<T>(this IDbConnection dbConn, string sqlFormat, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.SelectFmtAsync<T>(default(CancellationToken), sqlFormat, filterParams));
        }

        /// <summary>
        /// Returns a partial subset of results from the specified tableType using a SqlFormat query. E.g:
        /// <para>db.SelectFmt&lt;EntityWithId&gt;(typeof(Person), "Age &gt; {0}", 40)</para>
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<List<TModel>> SelectFmtAsync<TModel>(this IDbConnection dbConn, CancellationToken token, Type fromTableType, string sqlFormat, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.SelectFmtAsync<TModel>(token, fromTableType, sqlFormat, filterParams));
        }
        [Obsolete(Messages.LegacyApi)]
        public static Task<List<TModel>> SelectFmtAsync<TModel>(this IDbConnection dbConn, Type fromTableType, string sqlFormat, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.SelectFmtAsync<TModel>(default(CancellationToken), fromTableType, sqlFormat, filterParams));
        }

        /// <summary>
        /// Returns a single scalar value using an SqlFormat query. E.g:
        /// <para>db.ScalarFmt&lt;int&gt;("SELECT COUNT(*) FROM Person WHERE Age &gt; {0}", 40)</para>
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<T> ScalarFmtAsync<T>(this IDbConnection dbConn, CancellationToken token, string sqlFormat, params object[] sqlParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.ScalarFmtAsync<T>(token, sqlFormat, sqlParams));
        }
        [Obsolete(Messages.LegacyApi)]
        public static Task<T> ScalarFmtAsync<T>(this IDbConnection dbConn, string sqlFormat, params object[] sqlParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.ScalarFmtAsync<T>(default(CancellationToken), sqlFormat, sqlParams));
        }

        /// <summary>
        /// Returns the first column in a List using a SqlFormat query. E.g:
        /// <para>db.ColumnFmt&lt;string&gt;("SELECT LastName FROM Person WHERE Age = {0}", 27)</para>
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<List<T>> ColumnFmtAsync<T>(this IDbConnection dbConn, CancellationToken token, string sqlFormat, params object[] sqlParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.ColumnFmtAsync<T>(token, sqlFormat, sqlParams));
        }
        [Obsolete(Messages.LegacyApi)]
        public static Task<List<T>> ColumnFmtAsync<T>(this IDbConnection dbConn, string sqlFormat, params object[] sqlParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.ColumnFmtAsync<T>(default(CancellationToken), sqlFormat, sqlParams));
        }

        /// <summary>
        /// Returns the distinct first column values in a HashSet using an SqlFormat query. E.g:
        /// <para>db.ColumnDistinctFmt&lt;int&gt;("SELECT Age FROM Person WHERE Age &lt; {0}", 50)</para>
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<HashSet<T>> ColumnDistinctFmtAsync<T>(this IDbConnection dbConn, CancellationToken token, string sqlFormat, params object[] sqlParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.ColumnDistinctFmtAsync<T>(token, sqlFormat, sqlParams));
        }
        [Obsolete(Messages.LegacyApi)]
        public static Task<HashSet<T>> ColumnDistinctFmtAsync<T>(this IDbConnection dbConn, string sqlFormat, params object[] sqlParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.ColumnDistinctFmtAsync<T>(default(CancellationToken), sqlFormat, sqlParams));
        }

        /// <summary>
        /// Returns an Dictionary&lt;K, List&lt;V&gt;&gt; grouping made from the first two columns using an SqlFormat query. E.g:
        /// <para>db.LookupFmt&lt;int, string&gt;("SELECT Age, LastName FROM Person WHERE Age &lt; {0}", 50)</para>
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<Dictionary<K, List<V>>> LookupFmtAsync<K, V>(this IDbConnection dbConn, CancellationToken token, string sqlFormat, params object[] sqlParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.LookupFmtAsync<K, V>(token, sqlFormat, sqlParams));
        }
        [Obsolete(Messages.LegacyApi)]
        public static Task<Dictionary<K, List<V>>> LookupFmtAsync<K, V>(this IDbConnection dbConn, string sqlFormat, params object[] sqlParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.LookupFmtAsync<K, V>(default(CancellationToken), sqlFormat, sqlParams));
        }

        /// <summary>
        /// Returns a Dictionary from the first 2 columns: Column 1 (Keys), Column 2 (Values) using an SqlFormat query. E.g:
        /// <para>db.DictionaryFmt&lt;int, string&gt;("SELECT Id, LastName FROM Person WHERE Age &lt; {0}", 50)</para>
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<Dictionary<K, V>> DictionaryFmtAsync<K, V>(this IDbConnection dbConn, CancellationToken token, string sqlFormat, params object[] sqlParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.DictionaryFmtAsync<K, V>(token, sqlFormat, sqlParams));
        }
        [Obsolete(Messages.LegacyApi)]
        public static Task<Dictionary<K, V>> DictionaryFmtAsync<K, V>(this IDbConnection dbConn, string sqlFormat, params object[] sqlParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.DictionaryFmtAsync<K, V>(default(CancellationToken), sqlFormat, sqlParams));
        }

        /// <summary>
        /// Returns true if the Query returns any records, using an SqlFormat query. E.g:
        /// <para>db.ExistsFmt&lt;Person&gt;("Age = {0}", 42)</para>
        /// <para>db.ExistsFmt&lt;Person&gt;("SELECT * FROM Person WHERE Age = {0}", 50)</para>
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<bool> ExistsFmtAsync<T>(this IDbConnection dbConn, CancellationToken token, string sqlFormat, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.ExistsFmtAsync<T>(token, sqlFormat, filterParams));
        }
        [Obsolete(Messages.LegacyApi)]
        public static Task<bool> ExistsFmtAsync<T>(this IDbConnection dbConn, string sqlFormat, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.ExistsFmtAsync<T>(default(CancellationToken), sqlFormat, filterParams));
        }

        /// <summary>
        /// Returns true if the Query returns any records that match the SqlExpression lambda, E.g:
        /// <para>db.Exists&lt;Person&gt;(q =&gt; q.Where(x =&gt; x.Age &lt; 50))</para>
        /// </summary>
        [Obsolete("Use db.ExistsAsync(db.From<T>())")]
        public static Task<bool> ExistsAsync<T>(this IDbConnection dbConn, Func<SqlExpression<T>, SqlExpression<T>> expression, CancellationToken token = default)
        {
            return dbConn.Exec(dbCmd =>
            {
                var q = dbCmd.GetDialectProvider().SqlExpression<T>();
                var sql = expression(q).Limit(1);
                return dbCmd.SingleAsync<T>(sql, token).Then(x => x != null);
            });
        }

        /// <summary>
        /// Returns results from a Stored Procedure using an SqlFormat query. E.g:
        /// <para></para>
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<List<TOutputModel>> SqlProcedureFmtAsync<TOutputModel>(this IDbConnection dbConn, CancellationToken token,
            object anonType,
            string sqlFilter,
            params object[] filterParams)
            where TOutputModel : new()
        {
            return dbConn.Exec(dbCmd => dbCmd.SqlProcedureFmtAsync<TOutputModel>(token,
                anonType, sqlFilter, filterParams));
        }
    }
}

#endif