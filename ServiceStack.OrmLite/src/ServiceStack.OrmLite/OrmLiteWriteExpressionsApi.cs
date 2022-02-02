using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite
{
    public static class OrmLiteWriteExpressionsApi
    {
        /// <summary>
        /// Use an SqlExpression to select which fields to update and construct the where expression, E.g: 
        /// 
        ///   var q = db.From&gt;Person&lt;());
        ///   db.UpdateOnlyFields(new Person { FirstName = "JJ" }, q.Update(p => p.FirstName).Where(x => x.FirstName == "Jimi"));
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("FirstName" = 'Jimi')
        /// 
        ///   What's not in the update expression doesn't get updated. No where expression updates all rows. E.g:
        /// 
        ///   db.UpdateOnlyFields(new Person { FirstName = "JJ", LastName = "Hendo" }, ev.Update(p => p.FirstName));
        ///   UPDATE "Person" SET "FirstName" = 'JJ'
        /// </summary>
        public static int UpdateOnlyFields<T>(this IDbConnection dbConn, 
            T model, SqlExpression<T> onlyFields, 
            Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyFields(model, onlyFields, commandFilter));
        }

        /// <summary>
        /// Update record, updating only fields specified in updateOnly that matches the where condition (if any), E.g:
        /// 
        ///   db.UpdateOnly(new Person { FirstName = "JJ" }, new[]{ "FirstName" }, p => p.LastName == "Hendrix");
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
        /// </summary>
        public static int UpdateOnlyFields<T>(this IDbConnection dbConn, 
            T obj,
            string[] onlyFields,
            Expression<Func<T, bool>> where = null,
            Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyFields(obj, onlyFields, where, commandFilter));
        }

        /// <summary>
        /// Update record, updating only fields specified in updateOnly that matches the where condition (if any), E.g:
        /// 
        ///   db.UpdateOnly(new Person { FirstName = "JJ" }, p => p.FirstName, p => p.LastName == "Hendrix");
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
        ///
        ///   db.UpdateOnly(new Person { FirstName = "JJ" }, p => p.FirstName);
        ///   UPDATE "Person" SET "FirstName" = 'JJ'
        ///
        ///   db.UpdateOnly(new Person { FirstName = "JJ", Age = 27 }, p => new { p.FirstName, p.Age );
        ///   UPDATE "Person" SET "FirstName" = 'JJ', "Age" = 27
        /// </summary>
        public static int UpdateOnlyFields<T>(this IDbConnection dbConn, 
            T obj,
            Expression<Func<T, object>> onlyFields = null,
            Expression<Func<T, bool>> where = null,
            Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyFields(obj, onlyFields, where, commandFilter));
        }

        /// <summary>
        /// Update only fields in the specified expression that matches the where condition (if any), E.g:
        /// 
        ///   db.UpdateOnly(() => new Person { FirstName = "JJ" }, where: p => p.LastName == "Hendrix");
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
        ///
        ///   db.UpdateOnly(() => new Person { FirstName = "JJ" });
        ///   UPDATE "Person" SET "FirstName" = 'JJ'
        /// </summary>
        public static int UpdateOnly<T>(this IDbConnection dbConn, 
            Expression<Func<T>> updateFields,
            Expression<Func<T, bool>> where = null,
            Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateOnly(updateFields, dbCmd.GetDialectProvider().SqlExpression<T>().Where(where), commandFilter));
        }

        /// <summary>
        /// Update only fields in the specified expression that matches the where condition (if any), E.g:
        /// 
        ///   db.UpdateOnly(() => new Person { FirstName = "JJ" }, db.From&gt;Person&lt;().Where(p => p.LastName == "Hendrix"));
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
        /// </summary>
        public static int UpdateOnly<T>(this IDbConnection dbConn,
            Expression<Func<T>> updateFields,
            SqlExpression<T> q,
            Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateOnly(updateFields, q, commandFilter));
        }

        /// <summary>
        /// Update only fields in the specified expression that matches the where condition (if any), E.g:
        ///
        ///   var q = db.From&gt;Person&lt;().Where(p => p.LastName == "Hendrix");
        ///   db.UpdateOnly(() => new Person { FirstName = "JJ" }, q.WhereExpression, q.Params);
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
        /// </summary>
        public static int UpdateOnly<T>(this IDbConnection dbConn,
            Expression<Func<T>> updateFields,
            string whereExpression,
            IEnumerable<IDbDataParameter> sqlParams,
            Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateOnly(updateFields, whereExpression, sqlParams, commandFilter));
        }

        /// <summary>
        /// Updates all values from Object Dictionary matching the where condition. E.g
        /// 
        ///   db.UpdateOnly&lt;Person&gt;(new Dictionary&lt;string,object&lt; { {"FirstName", "JJ"} }, where:p => p.FirstName == "Jimi");
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("FirstName" = 'Jimi')
        /// </summary>
        public static int UpdateOnly<T>(this IDbConnection dbConn, Dictionary<string, object> updateFields, Expression<Func<T, bool>> obj)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateOnly(updateFields, obj));
        }

        /// <summary>
        /// Updates all values from Object Dictionary, Requires Id which is used as a Primary Key Filter. E.g
        /// 
        ///   db.UpdateOnly&lt;Person&gt;(new Dictionary&lt;string,object&lt; { {"Id", 1}, {"FirstName", "JJ"} });
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("Id" = 1)
        /// </summary>
        public static int UpdateOnly<T>(this IDbConnection dbConn, Dictionary<string, object> updateFields, Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateOnly<T>(updateFields, commandFilter));
        }

        /// <summary>
        /// Updates all values from Object Dictionary matching the where condition. E.g
        /// 
        ///   db.UpdateOnly&lt;Person&gt;(new Dictionary&lt;string,object&lt; { {"FirstName", "JJ"} }, "FirstName == {0}", new[] { "Jimi" });
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("FirstName" = 'Jimi')
        /// </summary>
        public static int UpdateOnly<T>(this IDbConnection dbConn, Dictionary<string, object> updateFields, string whereExpression, object[] whereParams, Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateOnly<T>(updateFields, whereExpression, whereParams, commandFilter));
        }

        /// <summary>
        /// Update record, updating only fields specified in updateOnly that matches the where condition (if any), E.g:
        /// Numeric fields generates an increment sql which is useful to increment counters, etc...
        /// avoiding concurrency conflicts
        /// 
        ///   db.UpdateAdd(() => new Person { Age = 5 }, where: p => p.LastName == "Hendrix");
        ///   UPDATE "Person" SET "Age" = "Age" + 5 WHERE ("LastName" = 'Hendrix')
        ///
        ///   db.UpdateAdd(() => new Person { Age = 5 });
        ///   UPDATE "Person" SET "Age" = "Age" + 5
        /// </summary>
        public static int UpdateAdd<T>(this IDbConnection dbConn,
            Expression<Func<T>> updateFields,
            Expression<Func<T, bool>> where = null,
            Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateAdd(updateFields, dbCmd.GetDialectProvider().SqlExpression<T>().Where(where), commandFilter));
        }

        /// <summary>
        /// Update record, updating only fields specified in updateOnly that matches the where condition (if any), E.g:
        /// Numeric fields generates an increment sql which is useful to increment counters, etc...
        /// avoiding concurrency conflicts
        /// 
        ///   db.UpdateAdd(() => new Person { Age = 5 }, db.From&lt;Person&gt;().Where(p => p.LastName == "Hendrix"));
        ///   UPDATE "Person" SET "Age" = "Age" + 5 WHERE ("LastName" = 'Hendrix')
        /// </summary>
        public static int UpdateAdd<T>(this IDbConnection dbConn,
            Expression<Func<T>> updateFields,
            SqlExpression<T> q,
            Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateAdd(updateFields, q, commandFilter));
        }

        /// <summary>
        /// Updates all non-default values set on item matching the where condition (if any). E.g
        /// 
        ///   db.UpdateNonDefaults(new Person { FirstName = "JJ" }, p => p.FirstName == "Jimi");
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("FirstName" = 'Jimi')
        /// </summary>
        public static int UpdateNonDefaults<T>(this IDbConnection dbConn, T item, Expression<Func<T, bool>> obj)
        {
            return dbConn.Exec(dbCmd => dbCmd.UpdateNonDefaults(item, obj));
        }

        /// <summary>
        /// Updates all values set on item matching the where condition (if any). E.g
        /// 
        ///   db.Update(new Person { Id = 1, FirstName = "JJ" }, p => p.LastName == "Hendrix");
        ///   UPDATE "Person" SET "Id" = 1,"FirstName" = 'JJ',"LastName" = NULL,"Age" = 0 WHERE ("LastName" = 'Hendrix')
        /// </summary>
        public static int Update<T>(this IDbConnection dbConn, T item, Expression<Func<T, bool>> where, Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.Update(item, where, commandFilter));
        }

        /// <summary>
        /// Updates all matching fields populated on anonymousType that matches where condition (if any). E.g:
        /// 
        ///   db.Update&lt;Person&gt;(new { FirstName = "JJ" }, p => p.LastName == "Hendrix");
        ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
        /// </summary>
        public static int Update<T>(this IDbConnection dbConn, object updateOnly, Expression<Func<T, bool>> where, Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.Update<T>(updateOnly, where, commandFilter));
        }

        /// <summary>
        /// Using an SqlExpression to only Insert the fields specified, e.g:
        /// 
        ///   db.InsertOnly(new Person { FirstName = "Amy" }, p => p.FirstName));
        ///   INSERT INTO "Person" ("FirstName") VALUES ('Amy');
        /// 
        ///   db.InsertOnly(new Person { Id =1 , FirstName="Amy" }, p => new { p.Id, p.FirstName }));
        ///   INSERT INTO "Person" ("Id", "FirstName") VALUES (1, 'Amy');
        /// </summary>
        public static long InsertOnly<T>(this IDbConnection dbConn, T obj, Expression<Func<T, object>> onlyFields, bool selectIdentity = false)
        {
            return dbConn.Exec(dbCmd => dbCmd.InsertOnly(obj, onlyFields.GetFieldNames(), selectIdentity));
        }

        /// <summary>
        /// Using an SqlExpression to only Insert the fields specified, e.g:
        /// 
        ///   db.InsertOnly(new Person { FirstName = "Amy" }, new[]{ "FirstName" }));
        ///   INSERT INTO "Person" ("FirstName") VALUES ('Amy');
        /// </summary>
        public static long InsertOnly<T>(this IDbConnection dbConn, T obj, string[] onlyFields, bool selectIdentity = false)
        {
            return dbConn.Exec(dbCmd => dbCmd.InsertOnly(obj, onlyFields, selectIdentity));
        }

        /// <summary>
        /// Using an SqlExpression to only Insert the fields specified, e.g:
        /// 
        ///   db.InsertOnly(() => new Person { FirstName = "Amy" }));
        ///   INSERT INTO "Person" ("FirstName") VALUES (@FirstName);
        /// </summary>
        public static long InsertOnly<T>(this IDbConnection dbConn, Expression<Func<T>> insertFields, bool selectIdentity = false)
        {
            return dbConn.Exec(dbCmd => dbCmd.InsertOnly(insertFields, selectIdentity));
        }

        /// <summary>
        /// Delete the rows that matches the where expression, e.g:
        /// 
        ///   db.Delete&lt;Person&gt;(p => p.Age == 27);
        ///   DELETE FROM "Person" WHERE ("Age" = 27)
        /// </summary>
        public static int Delete<T>(this IDbConnection dbConn, Expression<Func<T, bool>> where, Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.Delete(where, commandFilter));
        }

        /// <summary>
        /// Delete the rows that matches the where expression, e.g:
        /// 
        ///   var q = db.From&lt;Person&gt;());
        ///   db.Delete&lt;Person&gt;(q.Where(p => p.Age == 27));
        ///   DELETE FROM "Person" WHERE ("Age" = 27)
        /// </summary>
        public static int Delete<T>(this IDbConnection dbConn, SqlExpression<T> where, Action<IDbCommand> commandFilter = null)
        {
            return dbConn.Exec(dbCmd => dbCmd.Delete(where, commandFilter));
        }

        /// <summary>
        /// Delete the rows that matches the where filter, e.g:
        /// 
        ///   db.DeleteWhere&lt;Person&gt;("Age = {0}", new object[] { 27 });
        ///   DELETE FROM "Person" WHERE ("Age" = 27)
        /// </summary>
        public static int DeleteWhere<T>(this IDbConnection dbConn, string whereFilter, object[] whereParams)
        {
            return dbConn.Exec(dbCmd => dbCmd.DeleteWhere<T>(whereFilter, whereParams));
        }
    }
}