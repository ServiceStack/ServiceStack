using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite;

public static class OrmLiteWriteExpressionsApiAsync
{
    /// <summary>
    /// Use an SqlExpression to select which fields to update and construct the where expression, E.g: 
    /// 
    ///   var q = db.From&gt;Person&lt;());
    ///   db.UpdateOnlyFieldsAsync(new Person { FirstName = "JJ" }, q.Update(p => p.FirstName).Where(x => x.FirstName == "Jimi"));
    ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("FirstName" = 'Jimi')
    /// 
    ///   What's not in the update expression doesn't get updated. No where expression updates all rows. E.g:
    /// 
    ///   db.UpdateOnlyFieldsAsync(new Person { FirstName = "JJ", LastName = "Hendo" }, ev.Update(p => p.FirstName));
    ///   UPDATE "Person" SET "FirstName" = 'JJ'
    /// </summary>
    public static Task<int> UpdateOnlyFieldsAsync<T>(this IDbConnection dbConn,
        T model,
        SqlExpression<T> onlyFields,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyFieldsAsync(model, onlyFields, commandFilter, token));
    }

    /// <summary>
    /// Update record, updating only fields specified in updateOnly that matches the where condition (if any), E.g:
    /// 
    ///   db.UpdateOnlyAsync(new Person { FirstName = "JJ" }, new[]{ "FirstName" }, p => p.LastName == "Hendrix");
    ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
    /// </summary>
    public static Task<int> UpdateOnlyFieldsAsync<T>(this IDbConnection dbConn, 
        T obj,
        string[] onlyFields,
        Expression<Func<T, bool>> where = null,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyFieldsAsync(obj, onlyFields, where, commandFilter, token));
    }

    /// <summary>
    /// Update record, updating only fields specified in updateOnly that matches the where condition (if any), E.g:
    /// 
    ///   db.UpdateOnlyAsync(new Person { FirstName = "JJ" }, p => p.FirstName, p => p.LastName == "Hendrix");
    ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
    ///
    ///   db.UpdateOnlyAsync(new Person { FirstName = "JJ" }, p => p.FirstName);
    ///   UPDATE "Person" SET "FirstName" = 'JJ'
    /// </summary>
    public static Task<int> UpdateOnlyFieldsAsync<T>(this IDbConnection dbConn, 
        T obj,
        Expression<Func<T, object>> onlyFields = null,
        Expression<Func<T, bool>> where = null,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyFieldsAsync(obj, onlyFields, where, commandFilter, token));
    }

    /// <summary>
    /// Update record, updating only fields specified in updateOnly that matches the where condition (if any), E.g:
    /// 
    ///   db.UpdateOnlyAsync(() => new Person { FirstName = "JJ" }, where: p => p.LastName == "Hendrix");
    ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
    ///
    ///   db.UpdateOnlyAsync(() => new Person { FirstName = "JJ" });
    ///   UPDATE "Person" SET "FirstName" = 'JJ'
    /// </summary>
    public static Task<int> UpdateOnlyAsync<T>(this IDbConnection dbConn,
        Expression<Func<T>> updateFields,
        Expression<Func<T, bool>> where = null,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyAsync(updateFields, dbCmd.GetDialectProvider().SqlExpression<T>().Where(where), commandFilter, token));
    }

    /// <summary>
    /// Update record, updating only fields specified in updateOnly that matches the where condition (if any), E.g:
    /// 
    ///   db.UpdateOnlyAsync(() => new Person { FirstName = "JJ" }, db.From&lt;Person&gt;().Where(p => p.LastName == "Hendrix"));
    ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
    /// </summary>
    public static Task<int> UpdateOnlyAsync<T>(this IDbConnection dbConn,
        Expression<Func<T>> updateFields,
        SqlExpression<T> q,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyAsync(updateFields, q, commandFilter, token));
    }

    /// <summary>
    /// Update record, updating only fields specified in updateOnly that matches the where condition (if any), E.g:
    /// 
    ///   var q = db.From&gt;Person&lt;().Where(p => p.LastName == "Hendrix");
    ///   db.UpdateOnlyAsync(() => new Person { FirstName = "JJ" }, q.WhereExpression, q.Params);
    ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
    /// </summary>
    public static Task<int> UpdateOnlyAsync<T>(this IDbConnection dbConn,
        Expression<Func<T>> updateFields,
        string whereExpression,
        IEnumerable<IDbDataParameter> sqlParams,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyAsync(updateFields, whereExpression, sqlParams, commandFilter, token));
    }

    /// <summary>
    /// Updates all values from Object Dictionary matching the where condition. E.g
    /// 
    ///   db.UpdateOnlyAsync&lt;Person&gt;(new Dictionary&lt;string,object&lt; { {"FirstName", "JJ"} }, where:p => p.FirstName == "Jimi");
    ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("FirstName" = 'Jimi')
    /// </summary>
    public static Task<int> UpdateOnlyAsync<T>(this IDbConnection dbConn, 
        Dictionary<string, object> updateFields, 
        Expression<Func<T, bool>> where, 
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyAsync(updateFields, where, commandFilter, token));
    }

    /// <summary>
    /// Updates all values from Object Dictionary, Requires Id which is used as a Primary Key Filter. E.g
    /// 
    ///   db.UpdateOnlyAsync&lt;Person&gt;(new Dictionary&lt;string,object&lt; { {"Id", 1}, {"FirstName", "JJ"} });
    ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("Id" = 1)
    /// </summary>
    public static Task<int> UpdateOnlyAsync<T>(this IDbConnection dbConn, 
        Dictionary<string, object> updateFields, 
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyAsync<T>(updateFields, commandFilter, token));
    }

    /// <summary>
    /// Updates all values from Object Dictionary matching the where condition. E.g
    /// 
    ///   db.UpdateOnlyAsync&lt;Person&gt;(new Dictionary&lt;string,object&lt; { {"FirstName", "JJ"} }, "FirstName == {0}", new[]{ "Jimi" });
    ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("FirstName" = 'Jimi')
    /// </summary>
    public static Task<int> UpdateOnlyAsync<T>(this IDbConnection dbConn, 
        Dictionary<string, object> updateFields, 
        string whereExpression, 
        object[] whereParams, 
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateOnlyAsync<T>(updateFields, whereExpression, whereParams, commandFilter, token));
    }

    /// <summary>
    /// Update record, updating only fields specified in updateOnly that matches the where condition (if any), E.g:
    /// Numeric fields generates an increment sql which is useful to increment counters, etc...
    /// avoiding concurrency conflicts
    /// 
    ///   db.UpdateAddAsync(() => new Person { Age = 5 }, where: p => p.LastName == "Hendrix");
    ///   UPDATE "Person" SET "Age" = "Age" + 5 WHERE ("LastName" = 'Hendrix')
    ///
    ///   db.UpdateAddAsync(() => new Person { Age = 5 });
    ///   UPDATE "Person" SET "Age" = "Age" + 5
    /// </summary>
    public static Task<int> UpdateAddAsync<T>(this IDbConnection dbConn,
        Expression<Func<T>> updateFields,
        Expression<Func<T, bool>> where = null,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateAddAsync(updateFields, dbCmd.GetDialectProvider().SqlExpression<T>().Where(where), commandFilter, token));
    }

    /// <summary>
    /// Update record, updating only fields specified in updateOnly that matches the where condition (if any), E.g:
    /// Numeric fields generates an increment sql which is useful to increment counters, etc...
    /// avoiding concurrency conflicts
    /// 
    ///   db.UpdateAddAsync(() => new Person { Age = 5 }, db.From&lt;Person&gt;().Where(p => p.LastName == "Hendrix"));
    ///   UPDATE "Person" SET "Age" = "Age" + 5 WHERE ("LastName" = 'Hendrix')
    /// </summary>
    public static Task<int> UpdateAddAsync<T>(this IDbConnection dbConn,
        Expression<Func<T>> updateFields,
        SqlExpression<T> q,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateAddAsync(updateFields, q, commandFilter, token));
    }

    /// <summary>
    /// Updates all non-default values set on item matching the where condition (if any). E.g
    /// 
    ///   db.UpdateNonDefaultsAsync(new Person { FirstName = "JJ" }, p => p.FirstName == "Jimi");
    ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("FirstName" = 'Jimi')
    /// </summary>
    public static Task<int> UpdateNonDefaultsAsync<T>(this IDbConnection dbConn, T item, Expression<Func<T, bool>> obj, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateNonDefaultsAsync(item, obj, token));
    }

    /// <summary>
    /// Updates all values set on item matching the where condition (if any). E.g
    /// 
    ///   db.UpdateAsync(new Person { Id = 1, FirstName = "JJ" }, p => p.LastName == "Hendrix");
    ///   UPDATE "Person" SET "Id" = 1,"FirstName" = 'JJ',"LastName" = NULL,"Age" = 0 WHERE ("LastName" = 'Hendrix')
    /// </summary>
    public static Task<int> UpdateAsync<T>(this IDbConnection dbConn,
        T item,
        Expression<Func<T, bool>> where,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateAsync(item, where, commandFilter, token));
    }

    /// <summary>
    /// Updates all matching fields populated on anonymousType that matches where condition (if any). E.g:
    /// 
    ///   db.UpdateAsync&lt;Person&gt;(new { FirstName = "JJ" }, p => p.LastName == "Hendrix");
    ///   UPDATE "Person" SET "FirstName" = 'JJ' WHERE ("LastName" = 'Hendrix')
    /// </summary>
    public static Task<int> UpdateAsync<T>(this IDbConnection dbConn,
        object updateOnly,
        Expression<Func<T, bool>> where = null,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.UpdateAsync(updateOnly, where, commandFilter, token));
    }

    /// <summary>
    /// Using an SqlExpression to only Insert the fields specified, e.g:
    /// 
    ///   db.InsertOnlyAsync(new Person { FirstName = "Amy" }, p => p.FirstName));
    ///   INSERT INTO "Person" ("FirstName") VALUES ('Amy');
    /// 
    ///   db.InsertOnlyAsync(new Person { Id =1 , FirstName="Amy" }, p => new { p.Id, p.FirstName }));
    ///   INSERT INTO "Person" ("Id", "FirstName") VALUES (1, 'Amy');
    /// </summary>
    public static Task InsertOnlyAsync<T>(this IDbConnection dbConn, T obj, Expression<Func<T, object>> onlyFields, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.InsertOnlyAsync(obj, onlyFields.GetFieldNames(), token));
    }

    /// <summary>
    /// Using an SqlExpression to only Insert the fields specified, e.g:
    /// 
    ///   db.InsertOnlyAsync(new Person { FirstName = "Amy" }, new[]{ "FirstName" }));
    ///   INSERT INTO "Person" ("FirstName") VALUES ('Amy');
    /// </summary>
    public static Task InsertOnlyAsync<T>(this IDbConnection dbConn, T obj, string[] onlyFields, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.InsertOnlyAsync(obj, onlyFields, token));
    }

    /// <summary>
    /// Using an SqlExpression to only Insert the fields specified, e.g:
    /// 
    ///   db.InsertOnlyAsync(() => new Person { FirstName = "Amy" }));
    ///   INSERT INTO "Person" ("FirstName") VALUES (@FirstName);
    /// </summary>
    public static Task<int> InsertOnlyAsync<T>(this IDbConnection dbConn, Expression<Func<T>> insertFields, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.InsertOnlyAsync(insertFields, token));
    }

    /// <summary>
    /// Delete the rows that matches the where expression, e.g:
    /// 
    ///   db.DeleteAsync&lt;Person&gt;(p => p.Age == 27);
    ///   DELETE FROM "Person" WHERE ("Age" = 27)
    /// </summary>
    public static Task<int> DeleteAsync<T>(this IDbConnection dbConn, Expression<Func<T, bool>> where, 
        Action<IDbCommand> commandFilter = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.DeleteAsync(where, commandFilter, token));
    }

    /// <summary>
    /// Delete the rows that matches the where expression, e.g:
    /// 
    ///   var q = db.From&gt;Person&lt;());
    ///   db.DeleteAsync&lt;Person&gt;(q.Where(p => p.Age == 27));
    ///   DELETE FROM "Person" WHERE ("Age" = 27)
    /// </summary>
    public static Task<int> DeleteAsync<T>(this IDbConnection dbConn, SqlExpression<T> where
        , Action<IDbCommand> commandFilter = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.DeleteAsync(where, commandFilter, token));
    }
    
    /// <summary>
    /// Delete the rows that matches the where filter, e.g:
    /// 
    ///   db.DeleteWhereAsync&lt;Person&gt;("Age = {0}", new object[] { 27 });
    ///   DELETE FROM "Person" WHERE ("Age" = 27)
    /// </summary>
    public static Task<int> DeleteWhereAsync<T>(this IDbConnection dbConn, string whereFilter, object[] whereParams
        , Action<IDbCommand> commandFilter = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.DeleteWhereAsync<T>(whereFilter, whereParams, commandFilter, token));
    }
}
