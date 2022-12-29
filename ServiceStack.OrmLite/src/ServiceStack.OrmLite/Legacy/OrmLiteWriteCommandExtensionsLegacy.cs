using System;
using System.Data;

namespace ServiceStack.OrmLite.Legacy
{
    [Obsolete(Messages.LegacyApi)]
    public static class OrmLiteWriteCommandExtensionsLegacy
    {
        /// <summary>
        /// Delete rows using a SqlFormat filter. E.g:
        /// <para>db.Delete&lt;Person&gt;("Age > {0}", 42)</para>
        /// </summary>
        /// <returns>number of rows deleted</returns>
        [Obsolete(Messages.LegacyApi)]
        public static int DeleteFmt<T>(this IDbConnection dbConn, string sqlFilter, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteFmt<T>(sqlFilter, filterParams));
        }

        /// <summary>
        /// Delete rows from the runtime table type using a SqlFormat filter. E.g:
        /// </summary>
        /// <para>db.DeleteFmt(typeof(Person), "Age = {0}", 27)</para>
        /// <returns>number of rows deleted</returns>
        [Obsolete(Messages.LegacyApi)]
        public static int DeleteFmt(this IDbConnection dbConn, Type tableType, string sqlFilter, params object[] filterParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteFmt(tableType, sqlFilter, filterParams));
        }
    }
}