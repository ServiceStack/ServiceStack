#if ASYNC
// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Data;

namespace ServiceStack.OrmLite.Legacy
{
    public static class OrmLiteWriteApiAsyncLegacy
    {
        /// <summary>
        /// Delete rows using a SqlFormat filter. E.g:
        /// </summary>
        /// <returns>number of rows deleted</returns>
        [Obsolete(Messages.LegacyApi)]
        public static Task<int> DeleteFmtAsync<T>(this IDbConnection dbConn, CancellationToken token, string sqlFilter, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteFmtAsync<T>(token, sqlFilter, filterParams));
        }
        [Obsolete(Messages.LegacyApi)]
        public static Task<int> DeleteFmtAsync<T>(this IDbConnection dbConn, string sqlFilter, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteFmtAsync<T>(default(CancellationToken), sqlFilter, filterParams));
        }

        /// <summary>
        /// Delete rows from the runtime table type using a SqlFormat filter. E.g:
        /// </summary>
        /// <para>db.DeleteFmt(typeof(Person), "Age = {0}", 27)</para>
        /// <returns>number of rows deleted</returns>
        [Obsolete(Messages.LegacyApi)]
        public static Task<int> DeleteFmtAsync(this IDbConnection dbConn, CancellationToken token, Type tableType, string sqlFilter, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteFmtAsync(token, tableType, sqlFilter, filterParams));
        }
        [Obsolete(Messages.LegacyApi)]
        public static Task<int> DeleteFmtAsync(this IDbConnection dbConn, Type tableType, string sqlFilter, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteFmtAsync(default(CancellationToken), tableType, sqlFilter, filterParams));
        }
    }
}

#endif