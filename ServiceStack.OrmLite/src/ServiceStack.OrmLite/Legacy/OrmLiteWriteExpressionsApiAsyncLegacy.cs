#if ASYNC
using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite.Legacy
{
    [Obsolete(Messages.LegacyApi)]
    public static class OrmLiteWriteExpressionsApiAsyncLegacy
    {
        /// <summary>
        /// Insert only fields in POCO specified by the SqlExpression lambda. E.g:
        /// <para>db.InsertOnly(new Person { FirstName = "Amy", Age = 27 }, q =&gt; q.Insert(p =&gt; new { p.FirstName, p.Age }))</para>
        /// </summary>
        [Obsolete("Use db.InsertOnlyAsync(obj, db.From<T>())")]
        public static Task InsertOnlyAsync<T>(this IDbConnection dbConn, T obj, Func<SqlExpression<T>, SqlExpression<T>> onlyFields, CancellationToken token = default)
        {
            return dbConn.Exec(dbCmd => dbCmd.InsertOnlyAsync(obj, onlyFields, token));
        }

        /// <summary>
        /// Using an SqlExpression to only Insert the fields specified, e.g:
        /// 
        ///   db.InsertOnly(new Person { FirstName = "Amy" }, q => q.Insert(p => new { p.FirstName }));
        ///   INSERT INTO "Person" ("FirstName") VALUES ('Amy');
        /// </summary>
        public static Task InsertOnlyAsync<T>(this IDbConnection dbConn, T obj, SqlExpression<T> onlyFields, CancellationToken token = default)
        {
            return dbConn.Exec(dbCmd => dbCmd.InsertOnlyAsync(obj, onlyFields, token));
        }

        /// <summary>
        /// Use an SqlExpression to select which fields to update and construct the where expression, E.g: 
        /// 
        ///   db.UpdateOnly(new Person { FirstName = "JJ" }, ev => ev.Update(p => p.FirstName).Where(x => x.FirstName == "Jimi"));
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("FirstName" = 'Jimi')
        /// 
        ///   What's not in the update expression doesn't get updated. No where expression updates all rows. E.g:
        /// 
        ///   db.UpdateOnly(new Person { FirstName = "JJ", LastName = "Hendo" }, ev => ev.Update(p => p.FirstName));
        ///   UPDATE "Person" SET "FirstName" = 'JJ'
        /// </summary>
        [Obsolete("Use db.UpdateOnlyAsync(model, db.From<T>())")]
        public static Task<int> UpdateOnlyAsync<T>(this IDbConnection dbConn, T model, Func<SqlExpression<T>, SqlExpression<T>> onlyFields, CancellationToken token = default)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyAsync(model, onlyFields, token));
        }

        /// <summary>
        /// Flexible Update method to succinctly execute a free-text update statement using optional params. E.g:
        /// 
        ///   db.Update&lt;Person&gt;(set:"FirstName = {0}".Params("JJ"), where:"LastName = {0}".Params("Hendrix"));
        ///   UPDATE "Person" SET FirstName = 'JJ' WHERE LastName = 'Hendrix'
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<int> UpdateFmtAsync<T>(this IDbConnection dbConn, string set = null, string where = null, CancellationToken token = default)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateFmtAsync<T>(set, where, token));
        }

        /// <summary>
        /// Flexible Update method to succinctly execute a free-text update statement using optional params. E.g.
        /// 
        ///   db.Update(table:"Person", set: "FirstName = {0}".Params("JJ"), where: "LastName = {0}".Params("Hendrix"));
        ///   UPDATE "Person" SET FirstName = 'JJ' WHERE LastName = 'Hendrix'
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<int> UpdateFmtAsync(this IDbConnection dbConn, string table = null, string set = null, string where = null, CancellationToken token = default)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateFmtAsync(table, set, where, token));
        }

        /// <summary>
        /// Flexible Delete method to succinctly execute a delete statement using free-text where expression. E.g.
        /// 
        ///   db.Delete&lt;Person&gt;(where:"Age = {0}".Params(27));
        ///   DELETE FROM "Person" WHERE Age = 27
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<int> DeleteFmtAsync<T>(this IDbConnection dbConn, string where, CancellationToken token = default)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteFmtAsync<T>(where, token));
        }
        [Obsolete(Messages.LegacyApi)]
        public static Task<int> DeleteFmtAsync<T>(this IDbConnection dbConn, string where = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteFmtAsync<T>(where, default(CancellationToken)));
        }

        /// <summary>
        /// Flexible Delete method to succinctly execute a delete statement using free-text where expression. E.g.
        /// 
        ///   db.Delete(table:"Person", where: "Age = {0}".Params(27));
        ///   DELETE FROM "Person" WHERE Age = 27
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static Task<int> DeleteFmtAsync(this IDbConnection dbConn, string table = null, string where = null, CancellationToken token = default)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteFmtAsync(table, where, token));
        }

        /// <summary>
        /// Delete the rows that matches the where expression, e.g:
        /// 
        ///   db.Delete&lt;Person&gt;(ev => ev.Where(p => p.Age == 27));
        ///   DELETE FROM "Person" WHERE ("Age" = 27)
        /// </summary>
        [Obsolete("Use db.DeleteAsync(db.From<T>())")]
        public static Task<int> DeleteAsync<T>(this IDbConnection dbConn, Func<SqlExpression<T>, SqlExpression<T>> where, CancellationToken token = default)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteAsync(where, token));
        }
    }
}

#endif