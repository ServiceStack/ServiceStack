#if ASYNC
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

namespace ServiceStack.OrmLite
{
    internal static class OrmLiteWriteCommandExtensionsAsync
    {
        internal static ILog Log = LogManager.GetLogger(typeof(OrmLiteWriteCommandExtensionsAsync));

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

            return dbCmd.GetDialectProvider().ExecuteNonQueryAsync(dbCmd, token);
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

            return dbCmd.GetDialectProvider().ExecuteNonQueryAsync(dbCmd, token);
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
            OrmLiteUtils.AssertNotAnonType<T>();
            
            OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, obj.ToFilterType<T>());

            var dialectProvider = dbCmd.GetDialectProvider();
            var hadRowVersion = dialectProvider.PrepareParameterizedUpdateStatement<T>(dbCmd);
            if (string.IsNullOrEmpty(dbCmd.CommandText))
                return 0;

            dialectProvider.SetParameterValues<T>(dbCmd, obj);

            return await dbCmd.UpdateAndVerifyAsync<T>(commandFilter, hadRowVersion, token).ConfigAwait();
        }

        internal static async Task<int> UpdateAndVerifyAsync<T>(this IDbCommand dbCmd, Action<IDbCommand> commandFilter, bool hadRowVersion, CancellationToken token)
        {
            commandFilter?.Invoke(dbCmd);
            var rowsUpdated = await dbCmd.ExecNonQueryAsync(token).ConfigAwait();

            if (hadRowVersion && rowsUpdated == 0)
                throw new OptimisticConcurrencyException();

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

            using (dbTrans)
            {
                foreach (var obj in objs)
                {
                    OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, obj);
    
                    dialectProvider.SetParameterValues<T>(dbCmd, obj);

                    commandFilter?.Invoke(dbCmd); //filters can augment SQL & only should be invoked once
                    commandFilter = null;
    
                    var rowsUpdated = await dbCmd.ExecNonQueryAsync(token).ConfigAwait();
                        
                    if (hadRowVersion && rowsUpdated == 0)
                        throw new OptimisticConcurrencyException();
    
                    count += rowsUpdated;
                }

                dbTrans?.Commit();
            }
            return count;
        }

        private static async Task<int> AssertRowsUpdatedAsync(IDbCommand dbCmd, bool hadRowVersion, CancellationToken token)
        {
            var rowsUpdated = await dbCmd.ExecNonQueryAsync(token).ConfigAwait();
            if (hadRowVersion && rowsUpdated == 0)
                throw new OptimisticConcurrencyException();

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

            dialectProvider.SetParameterValues<T>(dbCmd, anonType);

            return AssertRowsUpdatedAsync(dbCmd, hadRowVersion, token);
        }

        internal static Task<int> DeleteNonDefaultsAsync<T>(this IDbCommand dbCmd, T filter, CancellationToken token)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            
            var dialectProvider = dbCmd.GetDialectProvider();
            var hadRowVersion = dialectProvider.PrepareParameterizedDeleteStatement<T>(
                dbCmd, filter.AllFieldsMap<T>().NonDefaultsOnly());

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
                throw new OptimisticConcurrencyException("The row was modified or deleted since the last read");
        }

        internal static Task<int> DeleteByIdsAsync<T>(this IDbCommand dbCmd, IEnumerable idValues, 
            Action<IDbCommand> commandFilter, CancellationToken token)
        {
            var sqlIn = dbCmd.SetIdsInSqlParams(idValues);
            if (string.IsNullOrEmpty(sqlIn))
                return TaskResult.Zero;

            var sql = OrmLiteWriteCommandExtensions.GetDeleteByIdsSql<T>(sqlIn, dbCmd.GetDialectProvider());

            return dbCmd.ExecuteSqlAsync(sql, commandFilter, token);
        }

        internal static Task<int> DeleteAllAsync<T>(this IDbCommand dbCmd, CancellationToken token)
        {
            return DeleteAllAsync(dbCmd, typeof(T), token);
        }

        internal static Task<int> DeleteAllAsync(this IDbCommand dbCmd, Type tableType, CancellationToken token)
        {
            var dialectProvider = dbCmd.GetDialectProvider();
            return dbCmd.ExecuteSqlAsync(dialectProvider.ToDeleteStatement(tableType, null), token);
        }

        internal static Task<int> DeleteAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            
            if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);
            return dbCmd.ExecuteSqlAsync(dbCmd.GetDialectProvider().ToDeleteStatement(typeof(T), sql), token);
        }

        internal static Task<int> DeleteAsync(this IDbCommand dbCmd, Type tableType, string sql, object anonType, CancellationToken token)
        {
            if (anonType != null) dbCmd.SetParameters(tableType, anonType, excludeDefaults: false, sql: ref sql);
            return dbCmd.ExecuteSqlAsync(dbCmd.GetDialectProvider().ToDeleteStatement(tableType, sql), token);
        }

        internal static async Task<long> InsertAsync<T>(this IDbCommand dbCmd, T obj, Action<IDbCommand> commandFilter, bool selectIdentity, bool enableIdentityInsert,CancellationToken token)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj);

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

        internal static async Task<long> InsertAsync<T>(this IDbCommand dbCmd, Dictionary<string,object> obj, Action<IDbCommand> commandFilter, bool selectIdentity, CancellationToken token)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj.ToFilterType<T>());

            var dialectProvider = dbCmd.GetDialectProvider();
            var pkField = ModelDefinition<T>.Definition.PrimaryKey;
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
                    return Convert.ToInt64(id);
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
            return InsertAllAsync(dbCmd, objs, commandFilter, token);
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

            dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd, insertFields: fieldsWithoutDefaults);

            using (dbTrans)
            {
                foreach (var obj in objs)
                {
                    OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj);

                    dialectProvider.SetParameterValues<T>(dbCmd, obj);

                    await dbCmd.ExecNonQueryAsync(token).ConfigAwait();
                }
                dbTrans?.Commit();
            }
        }

        internal static async Task<long> InsertIntoSelectAsync<T>(this IDbCommand dbCmd, ISqlExpression query, Action<IDbCommand> commandFilter, CancellationToken token) => 
            OrmLiteReadCommandExtensions.ToLong(await dbCmd.InsertIntoSelectInternal<T>(query, commandFilter).ExecNonQueryAsync(token: token).ConfigAwait());

        internal static async Task InsertAllAsync<T>(this IDbCommand dbCmd, IEnumerable<T> objs, Action<IDbCommand> commandFilter, CancellationToken token)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            
            IDbTransaction dbTrans = null;

            if (dbCmd.Transaction == null)
                dbCmd.Transaction = dbTrans = dbCmd.Connection.BeginTransaction();

            var dialectProvider = dbCmd.GetDialectProvider();

            dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd);

            using (dbTrans)
            {
                foreach (var obj in objs)
                {
                    OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj);

                    dialectProvider.SetParameterValues<T>(dbCmd, obj);

                    commandFilter?.Invoke(dbCmd); //filters can augment SQL & only should be invoked once
                    commandFilter = null;

                    await dbCmd.ExecNonQueryAsync(token).ConfigAwait();
                }
                dbTrans?.Commit();
            }
        }

        internal static Task<int> SaveAsync<T>(this IDbCommand dbCmd, CancellationToken token, params T[] objs)
        {
            return SaveAllAsync(dbCmd, objs, token);
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

            await dbCmd.UpdateAsync(obj, token, null);

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
                foreach (var row in saveRows)
                {
                    var id = modelDef.GetPrimaryKey(row);
                    if (id != defaultIdValue && existingRowsMap.ContainsKey(id))
                    {
                        await dbCmd.UpdateAsync(row, token, null).ConfigAwait();
                    }
                    else
                    {
                        if (modelDef.HasAutoIncrementId)
                        {
                            var newId = await dbCmd.InsertAsync(row, commandFilter:null, selectIdentity:true, enableIdentityInsert: false, token: token).ConfigAwait();
                            var safeId = dialectProvider.FromDbValue(newId, modelDef.PrimaryKey.FieldType);
                            modelDef.PrimaryKey.SetValue(row, safeId);
                            id = newId;
                        }
                        else
                        {
                            await dbCmd.InsertAsync(row, commandFilter:null, selectIdentity:false, enableIdentityInsert: false, token:token).ConfigAwait();
                        }

                        rowsAdded++;
                    }

                    modelDef.RowVersion?.SetValue(row, await dbCmd.GetRowVersionAsync(modelDef, id, token).ConfigAwait());
                }

                dbTrans?.Commit();
            }

            return rowsAdded;
        }

        public static async Task SaveAllReferencesAsync<T>(this IDbCommand dbCmd, T instance, CancellationToken token)
        {
            var modelDef = ModelDefinition<T>.Definition;
            var pkValue = modelDef.PrimaryKey.GetValue(instance);

            var fieldDefs = modelDef.AllFieldDefinitionsArray.Where(x => x.IsReference);
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
                            await dbCmd.UpdateAsync(instance, token, null).ConfigAwait();
                        }
                    }
                }
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
}
#endif