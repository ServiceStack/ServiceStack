using System;
using System.Data;

namespace ServiceStack.OrmLite.Legacy
{
    public static class OrmLiteWriteExpressionsApiLegacy
    {
        /// <summary>
        /// Insert only fields in POCO specified by the SqlExpression lambda. E.g:
        /// <para>db.InsertOnly(new Person { FirstName = "Amy", Age = 27 }, q =&gt; q.Insert(p =&gt; new { p.FirstName, p.Age }))</para>
        /// </summary>
        [Obsolete("Use db.InsertOnly(obj, db.From<T>())")]
        public static void InsertOnly<T>(this IDbConnection dbConn, T obj, Func<SqlExpression<T>, SqlExpression<T>> onlyFields)
        {
            dbConn.Exec(dbCmd => dbCmd.InsertOnly(obj, onlyFields));
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
        [Obsolete("Use db.UpdateOnly(model, db.From<T>())")]
        public static int UpdateOnly<T>(this IDbConnection dbConn, T model, Func<SqlExpression<T>, SqlExpression<T>> onlyFields)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateOnly(model, onlyFields));
        }

        /// <summary>
        /// Flexible Update method to succinctly execute a free-text update statement using optional params. E.g:
        /// 
        ///   db.Update&lt;Person&gt;(set:"FirstName = {0}".Params("JJ"), where:"LastName = {0}".Params("Hendrix"));
        ///   UPDATE "Person" SET FirstName = 'JJ' WHERE LastName = 'Hendrix'
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static int UpdateFmt<T>(this IDbConnection dbConn, string set = null, string where = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateFmt<T>(set, where));
        }

        /// <summary>
        /// Flexible Update method to succinctly execute a free-text update statement using optional params. E.g.
        /// 
        ///   db.Update(table:"Person", set: "FirstName = {0}".Params("JJ"), where: "LastName = {0}".Params("Hendrix"));
        ///   UPDATE "Person" SET FirstName = 'JJ' WHERE LastName = 'Hendrix'
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static int UpdateFmt(this IDbConnection dbConn, string table = null, string set = null, string where = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateFmt(table, set, where));
        }

        /// <summary>
        /// Flexible Delete method to succinctly execute a delete statement using free-text where expression. E.g.
        /// 
        ///   db.Delete&lt;Person&gt;(where:"Age = {0}".Params(27));
        ///   DELETE FROM "Person" WHERE Age = 27
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static int DeleteFmt<T>(this IDbConnection dbConn, string where = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteFmt<T>(where));
        }

        /// <summary>
        /// Flexible Delete method to succinctly execute a delete statement using free-text where expression. E.g.
        /// 
        ///   db.Delete(table:"Person", where: "Age = {0}".Params(27));
        ///   DELETE FROM "Person" WHERE Age = 27
        /// </summary>
        [Obsolete(Messages.LegacyApi)]
        public static int DeleteFmt(this IDbConnection dbConn, string table = null, string where = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteFmt(table, where));
        }

        /// <summary>
        /// Delete the rows that matches the where expression, e.g:
        /// 
        ///   db.Delete&lt;Person&gt;(ev => ev.Where(p => p.Age == 27));
        ///   DELETE FROM "Person" WHERE ("Age" = 27)
        /// </summary>
        [Obsolete("Use db.Delete(db.From<T>())")]
        public static int Delete<T>(this IDbConnection dbConn, Func<SqlExpression<T>, SqlExpression<T>> where)
        {
            return dbConn.Exec(dbCmd => dbCmd.Delete(where));
        }

        /// <summary>
        /// Using an SqlExpression to only Insert the fields specified, e.g:
        /// 
        ///   db.InsertOnly(new Person { FirstName = "Amy" }, q => q.Insert(p => new { p.FirstName }));
        ///   INSERT INTO "Person" ("FirstName") VALUES ('Amy');
        /// </summary>
        [Obsolete("Use db.InsertOnly(() => new Person { ... })")]
        public static void InsertOnly<T>(this IDbConnection dbConn, T obj, SqlExpression<T> onlyFields)
        {
            dbConn.Exec(dbCmd => dbCmd.InsertOnly(obj, onlyFields));
        }
    }
}