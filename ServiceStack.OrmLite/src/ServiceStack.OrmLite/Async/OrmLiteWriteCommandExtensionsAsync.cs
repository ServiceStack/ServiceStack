// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

internal static class OrmLiteWriteCommandExtensionsAsync
{
    internal static ILog Log => OrmLiteLog.Log;

    internal static Task<int> ExecuteSqlAsync(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token) =>
        dbCmd.SetParameters(sqlParams).ExecuteSqlAsync(sql, (Action<IDbCommand>) null, token);
        
    internal static Task<int> ExecuteSqlAsync(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams, 
        Action<IDbCommand> commandFilter, CancellationToken token)
    {
        return dbCmd.SetParameters(sqlParams).ExecuteSqlAsync(sql, commandFilter, token);
    }

    internal static Task<int> ExecuteSqlAsync(this IDbCommand dbCmd, string sql, CancellationToken token) =>
        dbCmd.ExecuteSqlAsync(sql,(Action<IDbCommand>)null, token);

    internal static Task<int> ExecuteSqlAsync(this IDbCommand dbCmd, string sql, 
        Action<IDbCommand> commandFilter, CancellationToken token)
    {
        dbCmd.CommandText = sql;

        commandFilter?.Invoke(dbCmd);

        if (Log.IsDebugEnabled)
            Log.DebugCommand(dbCmd);

        OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

        if (OrmLiteConfig.ResultsFilter != null)
            return OrmLiteConfig.ResultsFilter.ExecuteSql(dbCmd).InTask();

        return dbCmd.WithLog(dbCmd.ExecNonQueryWithRetryAsync(token));
    }

    internal static Task<int> ExecuteSqlAsync(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token) =>
        dbCmd.ExecuteSqlAsync(sql, anonType, null, token);

    internal static Task<int> ExecuteSqlAsync(this IDbCommand dbCmd, string sql, object anonType, 
        Action<IDbCommand> commandFilter, CancellationToken token)
    {
        if (anonType != null)
            dbCmd.SetParameters(anonType.ToObjectDictionary(), excludeDefaults: false, sql:ref sql);

        dbCmd.CommandText = sql;

        commandFilter?.Invoke(dbCmd);

        if (Log.IsDebugEnabled)
            Log.DebugCommand(dbCmd);

        OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

        if (OrmLiteConfig.ResultsFilter != null)
            return OrmLiteConfig.ResultsFilter.ExecuteSql(dbCmd).InTask();

        return dbCmd.WithLog(dbCmd.ExecNonQueryWithRetryAsync(token));
    }

    internal static Task<int> UpdateAsync<T>(this IDbCommand dbCmd, T obj, CancellationToken token, Action<IDbCommand> commandFilter = null)
    {
        return dbCmd.UpdateInternalAsync<T>(obj, token, commandFilter);
    }

    internal static Task<int> UpdateAsync<T>(this IDbCommand dbCmd, Dictionary<string,object> obj, CancellationToken token, Action<IDbCommand> commandFilter = null)
    {
        return dbCmd.UpdateInternalAsync<T>(obj, token, commandFilter);
    }

    internal static async Task<int> UpdateInternalAsync<T>(this IDbCommand dbCmd, object obj, CancellationToken token, Action<IDbCommand> commandFilter=null)
    {
        if (!dbCmd.PrepareUpdate<T>(obj, out var hadRowVersion))
            return 0;

        return await dbCmd.UpdateAndVerifyAsync<T>(commandFilter, hadRowVersion, token).ConfigAwait();
    }

    internal static async Task<int> UpdateAndVerifyAsync<T>(this IDbCommand dbCmd, Action<IDbCommand> commandFilter, bool hadRowVersion, CancellationToken token)
    {
        commandFilter?.Invoke(dbCmd);
        var rowsUpdated = await dbCmd.ExecNonQueryAsync(token).ConfigAwait();

        if (hadRowVersion && rowsUpdated == 0)
            throw new OptimisticConcurrencyException(OrmLiteWriteCommandExtensions.RowModifiedMessage);

        return rowsUpdated;
    }

    internal static Task<int> UpdateAsync<T>(this IDbCommand dbCmd, Action<IDbCommand> commandFilter, CancellationToken token, T[] objs)
    {
        return dbCmd.UpdateAllAsync(objs, commandFilter, token);
    }

    internal static async Task<int> UpdateAllAsync<T>(this IDbCommand dbCmd, IEnumerable<T> objs, Action<IDbCommand> commandFilter, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        IDbTransaction dbTrans = null;

        int count = 0;
        if (dbCmd.Transaction == null)
            dbCmd.Transaction = dbTrans = dbCmd.Connection.BeginTransaction();

        var dialectProvider = dbCmd.GetDialectProvider();

        var hadRowVersion = dialectProvider.PrepareParameterizedUpdateStatement<T>(dbCmd);
        if (string.IsNullOrEmpty(dbCmd.CommandText))
            return 0;

        dbCmd.AddFilterToWhere(typeof(T));

        using (dbTrans)
        {
            using var batch = OrmLiteBatch.TryCreate(dbCmd, needsRowsAffected: hadRowVersion);
            foreach (var obj in objs)
            {
                OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, obj);
    
                dbCmd.SetUpdateParameterValues<T>(obj);

                commandFilter?.Invoke(dbCmd); //filters can augment SQL & only should be invoked once
                commandFilter = null;

                if (batch != null)
                {
                    await batch.AddAsync(dbCmd, token).ConfigAwait();
                    continue;
                }
    
                var rowsUpdated = await dbCmd.ExecNonQueryAsync(token).ConfigAwait();
                        
                if (hadRowVersion && rowsUpdated == 0)
                    throw new OptimisticConcurrencyException(OrmLiteWriteCommandExtensions.RowModifiedMessage);
    
                count += rowsUpdated;
            }

            if (batch != null)
            {
                await batch.FlushAsync(token).ConfigAwait();
                count = batch.RowsAffected;
            }

            dbTrans?.Commit();
        }
        return count;
    }

    private static async Task<int> AssertRowsUpdatedAsync(IDbCommand dbCmd, bool hadRowVersion, CancellationToken token)
    {
        var rowsUpdated = await dbCmd.ExecNonQueryAsync(token).ConfigAwait();
        if (hadRowVersion && rowsUpdated == 0)
            throw new OptimisticConcurrencyException(OrmLiteWriteCommandExtensions.RowModifiedMessage);

        return rowsUpdated;
    }

    internal static Task<int> DeleteAsync<T>(this IDbCommand dbCmd, T filter, CancellationToken token)
    {
        return dbCmd.DeleteAsync<T>((object)filter, token);
    }

    internal static Task<int> DeleteAsync<T>(this IDbCommand dbCmd, object anonType, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        var dialectProvider = dbCmd.GetDialectProvider();

        var hadRowVersion = dialectProvider.PrepareParameterizedDeleteStatement<T>(
            dbCmd, anonType.AllFieldsMap<T>());

        dbCmd.AddFilterToWhere(typeof(T));
        dialectProvider.SetParameterValues<T>(dbCmd, anonType);

        return AssertRowsUpdatedAsync(dbCmd, hadRowVersion, token);
    }

    internal static Task<int> DeleteNonDefaultsAsync<T>(this IDbCommand dbCmd, T filter, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        var dialectProvider = dbCmd.GetDialectProvider();
        var hadRowVersion = dialectProvider.PrepareParameterizedDeleteStatement<T>(
            dbCmd, filter.AllFieldsMap<T>().NonDefaultsOnly());

        dbCmd.AddFilterToWhere(typeof(T));
        dialectProvider.SetParameterValues<T>(dbCmd, filter);

        return AssertRowsUpdatedAsync(dbCmd, hadRowVersion, token);
    }

    internal static Task<int> DeleteAsync<T>(this IDbCommand dbCmd, CancellationToken token, params T[] objs)
    {
        if (objs.Length == 0) 
            return TaskResult.Zero;

        return DeleteAllAsync(dbCmd, objs, fieldValuesFn:null, token: token);
    }

    internal static Task<int> DeleteNonDefaultsAsync<T>(this IDbCommand dbCmd, CancellationToken token, params T[] filters)
    {
        if (filters.Length == 0)
            return TaskResult.Zero;

        return DeleteAllAsync(dbCmd, filters, o => o.AllFieldsMap<T>().NonDefaultsOnly(), token:token);
    }

    private static async Task<int> DeleteAllAsync<T>(IDbCommand dbCmd, IEnumerable<T> objs, Func<object, Dictionary<string, object>> fieldValuesFn = null, 
        Action<IDbCommand> commandFilter=null, CancellationToken token=default)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        IDbTransaction dbTrans = null;

        int count = 0;
        if (dbCmd.Transaction == null)
            dbCmd.Transaction = dbTrans = dbCmd.Connection.BeginTransaction();

        var dialectProvider = dbCmd.GetDialectProvider();

        using (dbTrans)
        {
            foreach (var obj in objs)
            {
                var fieldValues = fieldValuesFn != null
                    ? fieldValuesFn(obj)
                    : obj.AllFieldsMap<T>();

                dialectProvider.PrepareParameterizedDeleteStatement<T>(dbCmd, fieldValues);

                dbCmd.AddFilterToWhere(typeof(T));
                dialectProvider.SetParameterValues<T>(dbCmd, obj);
                    
                commandFilter?.Invoke(dbCmd); //filters can augment SQL & only should be invoked once
                commandFilter = null;

                var rowsAffected = await dbCmd.ExecNonQueryAsync(token).ConfigAwait();
                count += rowsAffected;
            }
            dbTrans?.Commit();
        }

        return count;
    }

    internal static Task<int> DeleteByIdAsync<T>(this IDbCommand dbCmd, object id, 
        Action<IDbCommand> commandFilter, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        var sql = dbCmd.DeleteByIdSql<T>(id);
        return dbCmd.ExecuteSqlAsync(sql, commandFilter, token);
    }

    internal static async Task DeleteByIdAsync<T>(this IDbCommand dbCmd, object id, ulong rowVersion, 
        Action<IDbCommand> commandFilter, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        var sql = dbCmd.DeleteByIdSql<T>(id, rowVersion);

        var rowsAffected = await dbCmd.ExecuteSqlAsync(sql, commandFilter, token).ConfigAwait();
        if (rowsAffected == 0)
            throw new OptimisticConcurrencyException(OrmLiteWriteCommandExtensions.RowModifiedMessage);
    }

    internal static async Task<int> DeleteByIdsAsync<T>(this IDbCommand dbCmd, IEnumerable idValues, 
        Action<IDbCommand> commandFilter, CancellationToken token)
    {
        var dialect = dbCmd.GetDialectProvider();
        var batches = OrmLiteUtils.GetIdBatches(idValues, dialect);
        if (batches.Count == 1)
        {
            var sqlIn = dbCmd.SetIdsInSqlParams(batches[0]);
            if (string.IsNullOrEmpty(sqlIn))
                return 0;

            var sql = dbCmd.AddFilterToWhere(typeof(T), OrmLiteWriteCommandExtensions.GetDeleteByIdsSql<T>(sqlIn, dialect));
            return await dbCmd.ExecuteSqlAsync(sql, commandFilter, token).ConfigAwait();
        }

        // Delete all batches atomically
        IDbTransaction dbTrans = null;
        try
        {
            dbCmd.Transaction ??= dbTrans = dbCmd.Connection.BeginTransaction();

            var count = 0;
            foreach (var batch in batches)
            {
                dbCmd.Parameters.Clear();
                var sqlIn = dbCmd.SetIdsInSqlParams(batch);
                var sql = dbCmd.AddFilterToWhere(typeof(T), OrmLiteWriteCommandExtensions.GetDeleteByIdsSql<T>(sqlIn, dialect));
                count += await dbCmd.ExecuteSqlAsync(sql, commandFilter, token).ConfigAwait();
            }

            dbTrans?.Commit();
            return count;
        }
        finally
        {
            dbTrans?.Dispose();
            if (dbTrans != null && dbCmd.Transaction == dbTrans)
                dbCmd.Transaction = null;
        }
    }

    internal static Task<int> DeleteAllAsync<T>(this IDbCommand dbCmd, CancellationToken token)
    {
        return DeleteAllAsync(dbCmd, typeof(T), token);
    }

    internal static Task<int> DeleteAllAsync<T>(this IDbCommand dbCmd, IEnumerable<T> rows, CancellationToken token)
    {
        var ids = rows.Map(x => x.GetId());
        return dbCmd.DeleteByIdsAsync<T>(ids, null, token:token);
    }

    internal static Task<int> DeleteAllAsync(this IDbCommand dbCmd, Type tableType, CancellationToken token)
    {
        return dbCmd.ExecuteSqlAsync(dbCmd.ToFilteredDeleteStatement(tableType, null), token);
    }

    internal static Task<int> DeleteAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);
        return dbCmd.ExecuteSqlAsync(dbCmd.ToFilteredDeleteStatement(typeof(T), sql), token);
    }

    internal static Task<int> DeleteAsync(this IDbCommand dbCmd, Type tableType, string sql, object anonType, CancellationToken token)
    {
        if (anonType != null) dbCmd.SetParameters(tableType, anonType, excludeDefaults: false, sql: ref sql);
        return dbCmd.ExecuteSqlAsync(dbCmd.ToFilteredDeleteStatement(tableType, sql), token);
    }

    internal static async Task<long> InsertAsync<T>(this IDbCommand dbCmd, T obj, Action<IDbCommand> commandFilter, bool selectIdentity, bool enableIdentityInsert,CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
        OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj);

        dbCmd.SetInsertRuleValues<T>(obj);
        return await dbCmd.InsertObjectAsync(obj, commandFilter, selectIdentity, enableIdentityInsert, token).ConfigAwait();
    }

    private static async Task<long> InsertObjectAsync<T>(this IDbCommand dbCmd, T obj, Action<IDbCommand> commandFilter, bool selectIdentity, bool enableIdentityInsert,CancellationToken token)
    {
        var dialectProvider = dbCmd.GetDialectProvider();
        var pkField = ModelDefinition<T>.Definition.FieldDefinitions.FirstOrDefault(f => f.IsPrimaryKey);
        if (!enableIdentityInsert || pkField == null || !pkField.AutoIncrement)
        {
            dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd,
                insertFields: dialectProvider.GetNonDefaultValueInsertFields<T>(obj));
            return await InsertInternalAsync<T>(dialectProvider, dbCmd, obj, commandFilter, selectIdentity, token).ConfigAwait();
        }
        else
        {
            try
            {
                await dialectProvider.EnableIdentityInsertAsync<T>(dbCmd, token);
                dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd,
                    insertFields: dialectProvider.GetNonDefaultValueInsertFields<T>(obj),
                    shouldInclude: f => f == pkField);
                await InsertInternalAsync<T>(dialectProvider, dbCmd, obj, commandFilter, selectIdentity, token).ConfigAwait();
                if (selectIdentity)
                {
                    var id = pkField.GetValue(obj);
                    return Convert.ToInt64(id);
                }

                return default;
            }
            finally
            {
                await dialectProvider.DisableIdentityInsertAsync<T>(dbCmd, token);
            }
        }
    }

    internal static async Task<long> InsertAsync<T>(this IDbCommand dbCmd, Dictionary<string,object> obj, 
        Action<IDbCommand> commandFilter, bool selectIdentity, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
        OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj.ToFilterType<T>());
        obj = dbCmd.WithInsertRuleValues<T>(obj);

        var dialectProvider = dbCmd.GetDialectProvider();
        var modelDef = ModelDefinition<T>.Definition;
        var pkField = modelDef.PrimaryKey;
        object id = null;
        var enableIdentityInsert = pkField?.AutoIncrement == true && obj.TryGetValue(pkField.Name, out id);

        try
        {
            if (enableIdentityInsert)
                await dialectProvider.EnableIdentityInsertAsync<T>(dbCmd, token).ConfigAwait();
                
            dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd,
                insertFields: dialectProvider.GetNonDefaultValueInsertFields<T>(obj),
                shouldInclude: f => obj.ContainsKey(f.Name));

            var ret = await InsertInternalAsync<T>(dialectProvider, dbCmd, obj, commandFilter, selectIdentity, token).ConfigAwait();
            if (enableIdentityInsert)
                ret = Convert.ToInt64(id);

            if (modelDef.HasAnyReferences(obj.Keys))
            {
                if (pkField != null && !obj.ContainsKey(pkField.Name))
                    obj[pkField.Name] = ret;

                var instance = obj.FromObjectDictionary<T>();
                await dbCmd.SaveAllReferencesAsync(instance, token).ConfigAwait();
            }
            return ret;
        }
        finally
        {
            if (enableIdentityInsert)
                await dialectProvider.DisableIdentityInsertAsync<T>(dbCmd, token).ConfigAwait();
        }
    }

    private static async Task<long> InsertInternalAsync<T>(IOrmLiteDialectProvider dialectProvider,
        IDbCommand dbCmd, object obj, Action<IDbCommand> commandFilter, bool selectIdentity, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();

        dialectProvider.SetParameterValues<T>(dbCmd, obj);

        commandFilter?.Invoke(dbCmd);

        if (dialectProvider.HasInsertReturnValues(ModelDefinition<T>.Definition))
        {
            using var reader = await dbCmd.ExecReaderAsync(dbCmd.CommandText, token).ConfigAwait();
            return reader.PopulateReturnValues<T>(dialectProvider, obj);
        }

        if (selectIdentity)
        {
            dbCmd.CommandText += dialectProvider.GetLastInsertIdSqlSuffix<T>();

            return await dbCmd.ExecLongScalarAsync().ConfigAwait();
        }

        return await dbCmd.ExecNonQueryAsync(token).ConfigAwait();
    }

    internal static Task InsertAsync<T>(this IDbCommand dbCmd, Action<IDbCommand> commandFilter, CancellationToken token, T[] objs)
    {
        return InsertAllAsync(dbCmd, objs, commandFilter, token:token);
    }
        
    internal static async Task InsertUsingDefaultsAsync<T>(this IDbCommand dbCmd, T[] objs, CancellationToken token)
    {
        IDbTransaction dbTrans = null;

        if (dbCmd.Transaction == null)
            dbCmd.Transaction = dbTrans = dbCmd.Connection.BeginTransaction();

        var dialectProvider = dbCmd.GetDialectProvider();

        var modelDef = typeof(T).GetModelDefinition();
        var fieldsWithoutDefaults = modelDef.FieldDefinitionsArray
            .Where(x => x.DefaultValue == null)
            .Select(x => x.Name)
            .ToSet(); 

        dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd,
            insertFields: dbCmd.WithRuleFields<T>(fieldsWithoutDefaults, forInsert: true));

        using (dbTrans)
        {
            foreach (var obj in objs)
            {
                OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj);

                dbCmd.SetInsertParameterValues<T>(obj);

                await dbCmd.ExecNonQueryAsync(token).ConfigAwait();
            }
            dbTrans?.Commit();
        }
    }

    internal static async Task<long> InsertIntoSelectAsync<T>(this IDbCommand dbCmd, ISqlExpression query, Action<IDbCommand> commandFilter, CancellationToken token) => 
        OrmLiteReadCommandExtensions.ToLong(await dbCmd.InsertIntoSelectInternal<T>(query, commandFilter).ExecNonQueryAsync(token: token).ConfigAwait());

    internal static async Task InsertAllAsync<T>(this IDbCommand dbCmd, IEnumerable<T> objs, Action<IDbCommand> commandFilter, bool enableIdentityInsert=false, CancellationToken token = default)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        IDbTransaction dbTrans = null;

        try
        {
            if (dbCmd.Transaction == null)
                dbCmd.Transaction = dbTrans = dbCmd.Connection.BeginTransaction();

            var dialectProvider = dbCmd.GetDialectProvider();
            if (enableIdentityInsert)
            {
                await dialectProvider.EnableIdentityInsertAsync<T>(dbCmd, token);
            }
            try
            {
                // Inserts that return values are read one at a time
                using var batch = dialectProvider.HasInsertReturnValues(ModelDefinition<T>.Definition)
                    ? null
                    : OrmLiteBatch.TryCreate(dbCmd);
                foreach (var obj in objs)
                {
                    OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj);

                    dbCmd.SetInsertRuleValues<T>(obj);
                    var pkField = ModelDefinition<T>.Definition.FieldDefinitions.FirstOrDefault(f => f.IsPrimaryKey);
                    if (!enableIdentityInsert || pkField is not { AutoIncrement: true })
                    {
                        dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd,
                            insertFields: dialectProvider.GetNonDefaultValueInsertFields<T>(obj));
                    }
                    else
                    {
                        dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd,
                            insertFields: dialectProvider.GetNonDefaultValueInsertFields<T>(obj),
                            shouldInclude: f => f == pkField);
                    }

                    if (batch != null)
                    {
                        dialectProvider.SetParameterValues<T>(dbCmd, obj);
                        commandFilter?.Invoke(dbCmd);
                        await batch.AddAsync(dbCmd, token).ConfigAwait();
                        continue;
                    }

                    await InsertInternalAsync<T>(dialectProvider, dbCmd, obj, commandFilter, selectIdentity:false, token);
                }
                if (batch != null)
                    await batch.FlushAsync(token).ConfigAwait();
            }
            finally
            {
                if (enableIdentityInsert)
                {
                    await dialectProvider.DisableIdentityInsertAsync<T>(dbCmd, token);
                }
            }
            // Only rows that were all inserted are committed
            dbTrans?.Commit();
        }
        finally
        {
            dbTrans?.Dispose();
        }
    }

    internal static Task<int> SaveAsync<T>(this IDbCommand dbCmd, CancellationToken token, params T[] objs)
    {
        return SaveAllAsync(dbCmd, objs, token);
    }

    internal static Task UpsertAsync<T>(this IDbCommand dbCmd, T obj,
        ICollection<string> updateOnly, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
        OrmLiteConfig.UpsertFilter?.Invoke(dbCmd, obj);
        return dbCmd.UpsertRowAsync(obj, updateOnly, token);
    }

    private static async Task UpsertRowAsync<T>(this IDbCommand dbCmd, T obj,
        ICollection<string> updateOnly, CancellationToken token)
    {
        var modelDef = typeof(T).GetModelDefinition();
        var primaryKey = modelDef.FieldDefinitions.FirstOrDefault(x => x.IsPrimaryKey)
            ?? throw new NotSupportedException($"'{typeof(T).Name}' does not have a primary key");
        var updateFieldDefs = OrmLiteWriteCommandExtensions.GetUpsertUpdateFieldDefinitions(modelDef, updateOnly);
        var canonicalUpdateOnly = updateOnly == null
            ? null
            : updateFieldDefs.Map(x => x.Name);

        var id = primaryKey.GetValue(obj);
        var defaultId = primaryKey.FieldType.GetDefaultValue();
        if (primaryKey.AutoIncrement && (id == null || Equals(id, defaultId)))
        {
            var dialect = dbCmd.GetDialectProvider();
            var newId = await dbCmd.InsertAsync(obj, commandFilter: null, selectIdentity: true,
                enableIdentityInsert: false, token: token).ConfigAwait();
            primaryKey.SetValue(obj, dialect.FromDbValue(newId, primaryKey.FieldType));
            await dbCmd.ReadBackUpsertFieldsAsync(obj, modelDef,
                OrmLiteWriteCommandExtensions.GetUpsertFieldsAfterInsert(dialect, modelDef), primaryKey.GetValue(obj), token).ConfigAwait();
            return;
        }

        // A single upsert statement can't filter the row it updates in every RDBMS, or use different values for
        // its insert and update, so a filtered existence check is followed by an insert or a filtered update
        var dialectProvider = dbCmd.GetDialectProvider();
        if (dialectProvider is not IOrmLiteUpsertDialectProvider { SupportsUpsert: true } upsertProvider
            || dbCmd.HasFilters<T>() || dbCmd.HasWriteRules<T>())
        {
            await dbCmd.UpsertUsingSaveAsync(obj, modelDef, primaryKey, updateFieldDefs, token).ConfigAwait();
            return;
        }

        var readBackFields = OrmLiteWriteCommandExtensions.GetUpsertReadBackFields(modelDef);
        var didReadBack = false;
        var enableIdentityInsert = primaryKey.AutoIncrement;
        try
        {
            if (enableIdentityInsert)
                await dialectProvider.EnableIdentityInsertAsync<T>(dbCmd, token).ConfigAwait();

            upsertProvider.PrepareParameterizedUpsertStatement<T>(dbCmd,
                dialectProvider.GetNonDefaultValueInsertFields<T>(obj), canonicalUpdateOnly);
            dialectProvider.SetParameterValues<T>(dbCmd, obj);

            // Return the upserted row in the same statement when supported, e.g. RETURNING or OUTPUT
            var returningSql = readBackFields.Count > 0
                ? upsertProvider.ToUpsertReturningStatement(dbCmd.CommandText, modelDef, readBackFields)
                : null;
            if (returningSql != null)
            {
                var row = await dbCmd.ConvertToAsync<T>(returningSql, token).ConfigAwait();
                if (row != null) // no row is returned when an existing row isn't updated, e.g. DO NOTHING
                {
                    OrmLiteWriteCommandExtensions.CopyFields(row, obj, readBackFields);
                    didReadBack = true;
                }
            }
            else
            {
                await dbCmd.ExecNonQueryAsync(token).ConfigAwait();
            }
        }
        finally
        {
            if (enableIdentityInsert)
                await dialectProvider.DisableIdentityInsertAsync<T>(dbCmd, token).ConfigAwait();
        }

        if (!didReadBack)
            await dbCmd.ReadBackUpsertFieldsAsync(obj, modelDef, readBackFields, primaryKey.GetValue(obj), token).ConfigAwait();
    }

    /// <summary>
    /// Reads back fields with a separate query, only the row version if it's the only field
    /// </summary>
    private static async Task ReadBackUpsertFieldsAsync<T>(this IDbCommand dbCmd, T obj, ModelDefinition modelDef,
        List<FieldDefinition> fields, object id, CancellationToken token)
    {
        if (fields.Count == 0)
            return;

        if (fields.Count == 1 && fields[0].IsRowVersion)
        {
            fields[0].SetValue(obj, await dbCmd.GetRowVersionAsync(modelDef, id, token).ConfigAwait());
            return;
        }

        var row = await dbCmd.SingleByIdAsync<T>(id, token).ConfigAwait();
        if (row != null)
            OrmLiteWriteCommandExtensions.CopyFields(row, obj, fields);
    }

    internal static async Task UpsertAllAsync<T>(this IDbCommand dbCmd, IEnumerable<T> objs,
        ICollection<string> updateOnly, CancellationToken token)
    {
        var rows = objs.ToList();
        if (rows.Count == 0)
            return;

        IDbTransaction dbTrans = null;
        try
        {
            dbCmd.Transaction ??= dbTrans = dbCmd.Connection.BeginTransaction();

            var modelDef = typeof(T).GetModelDefinition();
            using var batch = dbCmd.CreateUpsertBatch<T>(modelDef);
            if (batch == null)
            {
                foreach (var row in rows)
                    await dbCmd.UpsertAsync(row, updateOnly, token).ConfigAwait();
            }
            else
            {
                var dialectProvider = dbCmd.GetDialectProvider();
                var primaryKey = modelDef.FieldDefinitions.First(x => x.IsPrimaryKey);
                var canonicalUpdateOnly = updateOnly == null
                    ? null
                    : OrmLiteWriteCommandExtensions.GetUpsertUpdateFieldDefinitions(modelDef, updateOnly).Map(x => x.Name);
                var identityInsert = false;
                try
                {
                    foreach (var row in rows)
                    {
                        OrmLiteUtils.AssertNotAnonType<T>();
                        OrmLiteConfig.UpsertFilter?.Invoke(dbCmd, row);

                        if (OrmLiteWriteCommandExtensions.IsUpsertInsert(primaryKey, row))
                        {
                            // Run after the rows before it, without the identity inserts of upserted rows
                            await batch.FlushAsync(token).ConfigAwait();
                            if (identityInsert)
                            {
                                await dialectProvider.DisableIdentityInsertAsync<T>(dbCmd, token).ConfigAwait();
                                identityInsert = false;
                            }
                            await dbCmd.UpsertRowAsync(row, updateOnly, token).ConfigAwait();
                            continue;
                        }

                        if (primaryKey.AutoIncrement && !identityInsert)
                        {
                            await dialectProvider.EnableIdentityInsertAsync<T>(dbCmd, token).ConfigAwait();
                            identityInsert = true;
                        }
                        dbCmd.PrepareUpsert(row, canonicalUpdateOnly);
                        await batch.AddAsync(dbCmd, token).ConfigAwait();
                    }
                    await batch.FlushAsync(token).ConfigAwait();
                }
                finally
                {
                    if (identityInsert)
                        await dialectProvider.DisableIdentityInsertAsync<T>(dbCmd, token).ConfigAwait();
                }
            }
            dbTrans?.Commit();
        }
        finally
        {
            dbTrans?.Dispose();
            if (dbCmd.Transaction == dbTrans)
                dbCmd.Transaction = null;
        }
    }

    private static async Task UpsertUsingSaveAsync<T>(this IDbCommand dbCmd, T obj,
        ModelDefinition modelDef, FieldDefinition primaryKey, List<FieldDefinition> updateFieldDefs,
        CancellationToken token)
    {
        var id = primaryKey.GetValue(obj);
        if (await dbCmd.ExistsByIdAsync<T>(id, token).ConfigAwait())
        {
            if (updateFieldDefs.Count > 0)
            {
                await dbCmd.UpdateOnlyAsync<T>(dbCmd.GetUpsertUpdateFields(obj, primaryKey, updateFieldDefs),
                    commandFilter: null, token: token, applyRules: false).ConfigAwait();
            }
        }
        else
        {
            await dbCmd.InsertAsync(obj, commandFilter: null, selectIdentity: false,
                enableIdentityInsert: primaryKey.AutoIncrement, token: token).ConfigAwait();
            await dbCmd.ReadBackUpsertFieldsAsync(obj, modelDef,
                OrmLiteWriteCommandExtensions.GetUpsertFieldsAfterInsert(dbCmd.GetDialectProvider(), modelDef), id, token).ConfigAwait();
            return;
        }

        await dbCmd.ReadBackUpsertFieldsAsync(obj, modelDef, OrmLiteWriteCommandExtensions.GetUpsertReadBackFields(modelDef), id, token).ConfigAwait();
    }

    internal static async Task<bool> SaveAsync<T>(this IDbCommand dbCmd, T obj, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        var modelDef = typeof(T).GetModelDefinition();
        var id = modelDef.GetPrimaryKey(obj);
        var existingRow = id != null ? await dbCmd.SingleByIdAsync<T>(id, token).ConfigAwait() : default(T);

        if (Equals(existingRow, default(T)))
        {
            if (modelDef.HasAutoIncrementId)
            {

                var newId = await dbCmd.InsertAsync(obj, commandFilter: null, selectIdentity: true, enableIdentityInsert:false, token:token).ConfigAwait();
                var safeId = dbCmd.GetDialectProvider().FromDbValue(newId, modelDef.PrimaryKey.FieldType);
                modelDef.PrimaryKey.SetValue(obj, safeId);
                id = newId;
            }
            else
            {
                await dbCmd.InsertAsync(obj, commandFilter:null, selectIdentity:false, enableIdentityInsert: false, token: token).ConfigAwait();
            }

            modelDef.RowVersion?.SetValue(obj, await dbCmd.GetRowVersionAsync(modelDef, id, token).ConfigAwait());

            return true;
        }

        await dbCmd.UpdateInternalAsync<T>(obj, token).ConfigAwait();

        modelDef.RowVersion?.SetValue(obj, await dbCmd.GetRowVersionAsync(modelDef, id, token).ConfigAwait());

        return false;
    }

    internal static async Task<int> SaveAllAsync<T>(this IDbCommand dbCmd, IEnumerable<T> objs, CancellationToken token)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        var saveRows = objs.ToList();

        var firstRow = saveRows.FirstOrDefault();
        if (Equals(firstRow, default(T))) return 0;

        var modelDef = typeof(T).GetModelDefinition();

        var firstRowId = modelDef.GetPrimaryKey(firstRow);
        var defaultIdValue = firstRowId?.GetType().GetDefaultValue();

        var idMap = defaultIdValue != null
            ? saveRows.Where(x => !defaultIdValue.Equals(modelDef.GetPrimaryKey(x))).ToSafeDictionary(x => modelDef.GetPrimaryKey(x))
            : saveRows.Where(x => modelDef.GetPrimaryKey(x) != null).ToSafeDictionary(x => modelDef.GetPrimaryKey(x));

        var existingRowsMap = (await dbCmd.SelectByIdsAsync<T>(idMap.Keys, token).ConfigAwait()).ToDictionary(x => modelDef.GetPrimaryKey(x));

        var rowsAdded = 0;

        IDbTransaction dbTrans = null;

        if (dbCmd.Transaction == null)
            dbCmd.Transaction = dbTrans = dbCmd.Connection.BeginTransaction();

        var dialectProvider = dbCmd.GetDialectProvider();

        using (dbTrans)
        {
            // Row versions are read back after each row is saved
            using var batch = modelDef.RowVersion == null ? OrmLiteBatch.TryCreate(dbCmd) : null;
            var batchInserts = batch != null && !modelDef.HasAutoIncrementId && !dialectProvider.HasInsertReturnValues(modelDef);
            foreach (var row in saveRows)
            {
                var id = modelDef.GetPrimaryKey(row);
                if (id != defaultIdValue && existingRowsMap.ContainsKey(id))
                {
                    if (batch != null)
                    {
                        if (dbCmd.PrepareUpdate<T>(row, out _))
                            await batch.AddAsync(dbCmd, token).ConfigAwait();
                    }
                    else
                    {
                        await dbCmd.UpdateInternalAsync<T>(row, token).ConfigAwait();
                    }
                }
                else
                {
                    if (batchInserts)
                    {
                        dbCmd.PrepareInsert(row);
                        await batch.AddAsync(dbCmd, token).ConfigAwait();
                    }
                    else if (modelDef.HasAutoIncrementId)
                    {
                        if (batch != null)
                            await batch.FlushAsync(token).ConfigAwait(); // rows are saved in order, the new id is read back
                        var newId = await dbCmd.InsertAsync(row, commandFilter:null, selectIdentity:true, enableIdentityInsert: false, token: token).ConfigAwait();
                        var safeId = dialectProvider.FromDbValue(newId, modelDef.PrimaryKey.FieldType);
                        modelDef.PrimaryKey.SetValue(row, safeId);
                        id = newId;
                    }
                    else
                    {
                        if (batch != null)
                            await batch.FlushAsync(token).ConfigAwait();
                        await dbCmd.InsertAsync(row, commandFilter:null, selectIdentity:false, enableIdentityInsert: false, token:token).ConfigAwait();
                    }

                    rowsAdded++;
                }

                modelDef.RowVersion?.SetValue(row, await dbCmd.GetRowVersionAsync(modelDef, id, token).ConfigAwait());
            }
            if (batch != null)
                await batch.FlushAsync(token).ConfigAwait();

            dbTrans?.Commit();
        }

        return rowsAdded;
    }

    internal static async Task SaveAllReferencesAsync<T>(this IDbCommand dbCmd, T instance, CancellationToken token) => 
        await SaveAllReferences(dbCmd, ModelDefinition<T>.Definition, instance, token).ConfigAwait();

    internal static async Task SaveAllReferences(IDbCommand dbCmd, ModelDefinition modelDef, object instance, CancellationToken token)
    {
        var pkValue = modelDef.PrimaryKey.GetValue(instance);
        var fieldDefs = modelDef.ReferenceFieldDefinitionsArray;

        bool updateInstance = false;
        foreach (var fieldDef in fieldDefs)
        {
            var listInterface = fieldDef.FieldType.GetTypeWithGenericInterfaceOf(typeof(IList<>));
            if (listInterface != null)
            {
                var refType = listInterface.GetGenericArguments()[0];
                var refModelDef = refType.GetModelDefinition();

                var refField = modelDef.GetRefFieldDef(refModelDef, refType);

                var results = (IEnumerable)fieldDef.GetValue(instance);
                if (results != null)
                {
                    foreach (var oRef in results)
                    {
                        refField.SetValue(oRef, pkValue);
                    }
                    await dbCmd.CreateTypedApi(refType).SaveAllAsync(results, token).ConfigAwait();
                }
            }
            else
            {
                var refType = fieldDef.FieldType;
                var refModelDef = refType.GetModelDefinition();

                var refSelf = modelDef.GetSelfRefFieldDefIfExists(refModelDef, fieldDef);

                var result = fieldDef.GetValue(instance);
                var refField = refSelf == null
                    ? modelDef.GetRefFieldDef(refModelDef, refType)
                    : modelDef.GetRefFieldDefIfExists(refModelDef);

                if (result != null)
                {
                    refField?.SetValue(result, pkValue);

                    await dbCmd.CreateTypedApi(refType).SaveAsync(result, token).ConfigAwait();

                    //Save Self Table.RefTableId PK
                    if (refSelf != null)
                    {
                        var refPkValue = refModelDef.PrimaryKey.GetValue(result);
                        refSelf.SetValue(instance, refPkValue);
                        updateInstance = true;
                    }
                }
            }
        }
        if (updateInstance)
        {
            await dbCmd.CreateTypedApi(instance.GetType()).UpdateAsync(instance, token).ConfigAwait();
        }
    }

    public static async Task SaveReferencesAsync<T, TRef>(this IDbCommand dbCmd, CancellationToken token, T instance, params TRef[] refs)
    {
        var modelDef = ModelDefinition<T>.Definition;
        var pkValue = modelDef.PrimaryKey.GetValue(instance);

        var refType = typeof(TRef);
        var refModelDef = ModelDefinition<TRef>.Definition;

        var refSelf = modelDef.GetSelfRefFieldDefIfExists(refModelDef, null);

        foreach (var oRef in refs)
        {
            var refField = refSelf == null
                ? modelDef.GetRefFieldDef(refModelDef, refType)
                : modelDef.GetRefFieldDefIfExists(refModelDef);

            refField?.SetValue(oRef, pkValue);
        }

        await dbCmd.SaveAllAsync(refs, token).ConfigAwait();

        foreach (var oRef in refs)
        {
            //Save Self Table.RefTableId PK
            if (refSelf != null)
            {
                var refPkValue = refModelDef.PrimaryKey.GetValue(oRef);
                refSelf.SetValue(instance, refPkValue);
                await dbCmd.UpdateAsync(instance, token, null).ConfigAwait();
            }
        }
    }

    // Procedures
    internal static Task ExecuteProcedureAsync<T>(this IDbCommand dbCommand, T obj, CancellationToken token)
    {
        var dialectProvider = dbCommand.GetDialectProvider();
        string sql = dialectProvider.ToExecuteProcedureStatement(obj);
        dbCommand.CommandType = CommandType.StoredProcedure;
        return dbCommand.ExecuteSqlAsync(sql, token);
    }

    internal static async Task<object> GetRowVersionAsync(this IDbCommand dbCmd, ModelDefinition modelDef, object id, CancellationToken token)
    {
        var sql = dbCmd.RowVersionSql(modelDef, id);
        var rowVersion = await dbCmd.ScalarAsync<object>(sql, token).ConfigAwait();
        var to = dbCmd.GetDialectProvider().FromDbRowVersion(modelDef.RowVersion.FieldType, rowVersion);

        if (to is ulong u && modelDef.RowVersion.ColumnType == typeof(byte[]))
            return BitConverter.GetBytes(u);
            
        return to ?? modelDef.RowVersion.ColumnType.GetDefaultValue();
    }
}
