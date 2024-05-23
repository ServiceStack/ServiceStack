#if ASYNC
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

internal static class WriteExpressionCommandExtensionsAsync
{
    internal static Task<int> UpdateOnlyFieldsAsync<T>(this IDbCommand dbCmd, T model, SqlExpression<T> onlyFields, Action<IDbCommand> commandFilter, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        dbCmd.UpdateOnlySql(model, onlyFields);
        commandFilter?.Invoke(dbCmd);
        return dbCmd.ExecNonQueryAsync(token);
    }

    internal static Task<int> UpdateOnlyFieldsAsync<T>(this IDbCommand dbCmd, T obj,
        Expression<Func<T, object>> onlyFields,
        Expression<Func<T, bool>> where,
        Action<IDbCommand> commandFilter,
        CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        if (onlyFields == null)
            throw new ArgumentNullException(nameof(onlyFields));

        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        q.Update(onlyFields);
        q.Where(where);
        return dbCmd.UpdateOnlyFieldsAsync(obj, q, commandFilter, token);
    }

    internal static Task<int> UpdateOnlyFieldsAsync<T>(this IDbCommand dbCmd, T obj,
        string[] onlyFields,
        Expression<Func<T, bool>> where,
        Action<IDbCommand> commandFilter,
        CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        if (onlyFields == null)
            throw new ArgumentNullException(nameof(onlyFields));

        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        q.Update(onlyFields);
        q.Where(where);
        return dbCmd.UpdateOnlyFieldsAsync(obj, q, commandFilter, token);
    }

    internal static Task<int> UpdateOnlyAsync<T>(this IDbCommand dbCmd,
        Expression<Func<T>> updateFields,
        SqlExpression<T> q,
        Action<IDbCommand> commandFilter,
        CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        var cmd = dbCmd.InitUpdateOnly(updateFields, q);
        commandFilter?.Invoke(cmd);
        return cmd.ExecNonQueryAsync(token);
    }

    internal static Task<int> UpdateOnlyAsync<T>(this IDbCommand dbCmd,
        Expression<Func<T>> updateFields,
        string whereExpression,
        IEnumerable<IDbDataParameter> sqlParams,
        Action<IDbCommand> commandFilter,
        CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        var cmd = dbCmd.InitUpdateOnly(updateFields, whereExpression, sqlParams);
        commandFilter?.Invoke(cmd);
        return cmd.ExecNonQueryAsync(token);
    }

    public static Task<int> UpdateAddAsync<T>(this IDbCommand dbCmd,
        Expression<Func<T>> updateFields,
        SqlExpression<T> q,
        Action<IDbCommand> commandFilter,
        CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        var cmd = dbCmd.InitUpdateAdd(updateFields, q);
        commandFilter?.Invoke(cmd);
        return cmd.ExecNonQueryAsync(token);
    }

    public static Task<int> UpdateOnlyAsync<T>(this IDbCommand dbCmd,
        Dictionary<string, object> updateFields,
        Expression<Func<T, bool>> where,
        Action<IDbCommand> commandFilter = null, 
        CancellationToken token = default)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        if (updateFields == null)
            throw new ArgumentNullException(nameof(updateFields));

        OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, updateFields.ToFilterType<T>());

        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        q.Where(where);
        q.PrepareUpdateStatement(dbCmd, updateFields);
        return dbCmd.UpdateAndVerifyAsync<T>(commandFilter, updateFields.ContainsKey(ModelDefinition.RowVersionName), token);
    }

    public static Task<int> UpdateOnlyAsync<T>(this IDbCommand dbCmd,
        Dictionary<string, object> updateFields,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbCmd.UpdateOnlyReferencesAsync<T>(updateFields, dbFields =>
        {
            var whereExpr = dbCmd.GetDialectProvider().GetUpdateOnlyWhereExpression<T>(dbFields, out var exprArgs);
            dbCmd.PrepareUpdateOnly<T>(dbFields, whereExpr, exprArgs);
            return dbCmd.UpdateAndVerifyAsync<T>(commandFilter, dbFields.ContainsKey(ModelDefinition.RowVersionName), token);
        }, token);
    }

    public static Task<int> UpdateOnlyAsync<T>(this IDbCommand dbCmd,
        Dictionary<string, object> updateFields,
        string whereExpression,
        object[] whereParams,
        Action<IDbCommand> commandFilter = null,
        CancellationToken token = default)
    {
        return dbCmd.UpdateOnlyReferencesAsync<T>(updateFields, dbFields =>
        {
            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            q.Where(whereExpression, whereParams);
            q.PrepareUpdateStatement(dbCmd, dbFields);
            return dbCmd.UpdateAndVerifyAsync<T>(commandFilter, dbFields.ContainsKey(ModelDefinition.RowVersionName), token);
        }, token);
    }

    public static async Task<int> UpdateOnlyReferencesAsync<T>(this IDbCommand dbCmd,
        Dictionary<string, object> updateFields, Func<Dictionary<string, object>, Task<int>> fn, CancellationToken token = default)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        if (updateFields == null)
            throw new ArgumentNullException(nameof(updateFields));

        OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, updateFields.ToFilterType<T>());

        var dbFields = updateFields;
        var modelDef = ModelDefinition<T>.Definition;
        var hasReferences = modelDef.HasAnyReferences(updateFields.Keys); 
        if (hasReferences)
        {
            dbFields = new Dictionary<string, object>();
            foreach (var entry in updateFields)
            {
                if (!modelDef.IsReference(entry.Key)) 
                    dbFields[entry.Key] = entry.Value;
            }
        }

        var ret = await fn(dbFields).ConfigAwait();

        if (hasReferences)
        {
            var instance = updateFields.FromObjectDictionary<T>();
            await dbCmd.SaveAllReferencesAsync(instance, token).ConfigAwait();
        }
        return ret;
    }

    internal static Task<int> UpdateNonDefaultsAsync<T>(this IDbCommand dbCmd, T item, Expression<Func<T, bool>> obj, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();

        OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, item);

        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        q.Where(obj);
        q.PrepareUpdateStatement(dbCmd, item, excludeDefaults: true);
        return dbCmd.ExecNonQueryAsync(token);
    }

    internal static Task<int> UpdateAsync<T>(this IDbCommand dbCmd, T item, Expression<Func<T, bool>> expression, Action<IDbCommand> commandFilter, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();

        OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, item);

        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        q.Where(expression);
        q.PrepareUpdateStatement(dbCmd, item);
        commandFilter?.Invoke(dbCmd);
        return dbCmd.ExecNonQueryAsync(token);
    }

    internal static Task<int> UpdateAsync<T>(this IDbCommand dbCmd, object updateOnly, Expression<Func<T, bool>> where, Action<IDbCommand> commandFilter, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();

        OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, updateOnly.ToFilterType<T>());

        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        var whereSql = q.Where(where).WhereExpression;
        q.CopyParamsTo(dbCmd);
        dbCmd.PrepareUpdateAnonSql<T>(dbCmd.GetDialectProvider(), updateOnly, whereSql);
        commandFilter?.Invoke(dbCmd);

        return dbCmd.ExecNonQueryAsync(token);
    }

    internal static Task InsertOnlyAsync<T>(this IDbCommand dbCmd, T obj, string[] onlyFields, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();

        OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj);

        var dialectProvider = dbCmd.GetDialectProvider();
        var sql = dialectProvider.ToInsertRowStatement(dbCmd, obj, onlyFields);

        dialectProvider.SetParameterValues<T>(dbCmd, obj);

        return dbCmd.ExecuteSqlAsync(sql, token);
    }

    public static Task<int> InsertOnlyAsync<T>(this IDbCommand dbCmd, Expression<Func<T>> insertFields, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();

        return dbCmd.InitInsertOnly(insertFields).ExecNonQueryAsync(token);
    }

    internal static Task<int> DeleteAsync<T>(this IDbCommand dbCmd, Expression<Func<T, bool>> where, 
        Action<IDbCommand> commandFilter, CancellationToken token)
    {
        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        q.Where(where);
        return dbCmd.DeleteAsync(q, commandFilter, token);
    }

    internal static Task<int> DeleteAsync<T>(this IDbCommand dbCmd, SqlExpression<T> q, 
        Action<IDbCommand> commandFilter, CancellationToken token)
    {
        var sql = q.ToDeleteRowStatement();
        return dbCmd.ExecuteSqlAsync(sql, q.Params, commandFilter, token);
    }

    internal static Task<int> DeleteWhereAsync<T>(this IDbCommand dbCmd, string whereFilter, object[] whereParams, 
        Action<IDbCommand> commandFilter, CancellationToken token)
    {
        var q = dbCmd.GetDialectProvider().SqlExpression<T>();
        q.Where(whereFilter, whereParams);
        var sql = q.ToDeleteRowStatement();
        return dbCmd.ExecuteSqlAsync(sql, q.Params, commandFilter, token);
    }
}

#endif