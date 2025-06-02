//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.OrmLite.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

public static class OrmLiteWriteCommandExtensions
{
    internal static ILog Log = LogManager.GetLogger(typeof(OrmLiteWriteCommandExtensions));

    internal static bool CreateSchema<T>(this IDbCommand dbCmd)
    {
        var schemaName = typeof(T).FirstAttribute<SchemaAttribute>()?.Name;
        if(schemaName == null) throw new InvalidOperationException($"Type {typeof(T).Name} does not have a schema attribute, just CreateSchema(string schemaName) instead"); 
        return CreateSchema(dbCmd, schemaName);
    }
        
    internal static bool CreateSchema(this IDbCommand dbCmd, string schemaName)
    {
        schemaName.ThrowIfNullOrEmpty(nameof(schemaName));
        var dialectProvider = dbCmd.GetDialectProvider();
        var schema = dialectProvider.NamingStrategy.GetSchemaName(schemaName);
        var schemaExists = dialectProvider.DoesSchemaExist(dbCmd, schema);
        if (schemaExists)
        {
            return true;
        }

        try
        {
            ExecuteSql(dbCmd, dialectProvider.ToCreateSchemaStatement(schema));
            return true;
        }
        catch (Exception ex)
        {
            if (IgnoreAlreadyExistsError(ex))
            {
                Log.DebugFormat("Ignoring existing schema '{0}': {1}", schema, ex.Message);
                return false;
            }
            throw;
        }
    }

    internal static void CreateTables(this IDbCommand dbCmd, bool overwrite, params Type[] tableTypes)
    {
        foreach (var tableType in tableTypes)
        {
            CreateTable(dbCmd, overwrite, tableType);
        }
    }

    internal static bool CreateTable<T>(this IDbCommand dbCmd, bool overwrite = false)
    {
        var tableType = typeof(T);
        return CreateTable(dbCmd, overwrite, tableType);
    }

    internal static bool CreateTable(this IDbCommand dbCmd, bool overwrite, Type modelType)
    {
        var modelDef = modelType.GetModelDefinition();

        var dialectProvider = dbCmd.GetDialectProvider();
        var tableName = dialectProvider.NamingStrategy.GetTableName(modelDef);
        var schema = dialectProvider.NamingStrategy.GetSchemaName(modelDef);
        var tableExists = dialectProvider.DoesTableExist(dbCmd, tableName, schema);
        if (overwrite && tableExists)
        {
            DropTable(dbCmd, modelDef);

            var postDropTableSql = dialectProvider.ToPostDropTableStatement(modelDef);
            if (postDropTableSql != null)
            {
                ExecuteSql(dbCmd, postDropTableSql);
            }

            tableExists = false;
        }

        try
        {
            if (!tableExists)
            {
                if (modelDef.PreCreateTableSql != null)
                {
                    ExecuteSql(dbCmd, modelDef.PreCreateTableSql);
                }

                // sequences must be created before tables
                var sequenceList = dialectProvider.SequenceList(modelType);
                if (sequenceList.Count > 0)
                {
                    foreach (var seq in sequenceList)
                    {
                        if (dialectProvider.DoesSequenceExist(dbCmd, seq) == false)
                        {
                            var seqSql = dialectProvider.ToCreateSequenceStatement(modelType, seq);
                            dbCmd.ExecuteSql(seqSql);
                        }
                    }
                }
                else
                {
                    var sequences = dialectProvider.ToCreateSequenceStatements(modelType);
                    foreach (var seq in sequences)
                    {
                        try
                        {
                            dbCmd.ExecuteSql(seq);
                        }
                        catch (Exception ex)
                        {
                            if (IgnoreAlreadyExistsGeneratorError(ex))
                            {
                                Log.DebugFormat("Ignoring existing generator '{0}': {1}", seq, ex.Message);
                                continue;
                            }

                            throw;
                        }
                    }
                }

                var createTableSql = dialectProvider.ToCreateTableStatement(modelType);
                ExecuteSql(dbCmd, createTableSql);

                var postCreateTableSql = dialectProvider.ToPostCreateTableStatement(modelDef);
                if (postCreateTableSql != null)
                {
                    ExecuteSql(dbCmd, postCreateTableSql);
                }

                if (modelDef.PostCreateTableSql != null)
                {
                    ExecuteSql(dbCmd, modelDef.PostCreateTableSql);
                }

                var sqlIndexes = dialectProvider.ToCreateIndexStatements(modelType);
                foreach (var sqlIndex in sqlIndexes)
                {
                    try
                    {
                        dbCmd.ExecuteSql(sqlIndex);
                    }
                    catch (Exception exIndex)
                    {
                        if (IgnoreAlreadyExistsError(exIndex))
                        {
                            Log.DebugFormat("Ignoring existing index '{0}': {1}", sqlIndex, exIndex.Message);
                            continue;
                        }

                        throw;
                    }
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            if (IgnoreAlreadyExistsError(ex))
            {
                Log.DebugFormat("Ignoring existing table '{0}': {1}", modelDef.ModelName, ex.Message);
                return false;
            }

            throw;
        }

        return false;
    }

    internal static void DropTable<T>(this IDbCommand dbCmd)
    {
        DropTable(dbCmd, ModelDefinition<T>.Definition);
    }

    internal static void DropTable(this IDbCommand dbCmd, Type modelType)
    {
        DropTable(dbCmd, modelType.GetModelDefinition());
    }

    internal static void DropTables(this IDbCommand dbCmd, params Type[] tableTypes)
    {
        foreach (var modelDef in tableTypes.Select(type => type.GetModelDefinition()))
        {
            DropTable(dbCmd, modelDef);
        }
    }

    private static void DropTable(IDbCommand dbCmd, ModelDefinition modelDef)
    {
        try
        {
            var dialectProvider = dbCmd.GetDialectProvider();
            var tableName = dialectProvider.NamingStrategy.GetTableName(modelDef);
            var schema = dialectProvider.NamingStrategy.GetSchemaName(modelDef);

            if (dialectProvider.DoesTableExist(dbCmd, tableName, schema))
            {
                if (modelDef.PreDropTableSql != null)
                {
                    ExecuteSql(dbCmd, modelDef.PreDropTableSql);
                }

                var dropTableFks = dialectProvider.GetDropForeignKeyConstraints(modelDef);
                if (!string.IsNullOrEmpty(dropTableFks))
                {
                    dbCmd.ExecuteSql(dropTableFks);
                }
                dbCmd.ExecuteSql($"DROP TABLE {dialectProvider.GetQuotedTableName(modelDef)}");

                if (modelDef.PostDropTableSql != null)
                {
                    ExecuteSql(dbCmd, modelDef.PostDropTableSql);
                }
            }
        }
        catch (Exception ex)
        {
            Log.DebugFormat("Could not drop table '{0}': {1}", modelDef.ModelName, ex.Message);
            throw;
        }
    }

    internal static string LastSql(this IDbCommand dbCmd)
    {
        return dbCmd.CommandText;
    }

    internal static int ExecuteSql(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams = null, Action<IDbCommand> commandFilter = null)
    {
        dbCmd.CommandText = sql;

        dbCmd.SetParameters(sqlParams);

        commandFilter?.Invoke(dbCmd);

        if (Log.IsDebugEnabled)
            Log.DebugCommand(dbCmd);

        OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

        if (OrmLiteConfig.ResultsFilter != null)
            return OrmLiteConfig.ResultsFilter.ExecuteSql(dbCmd);

        return dbCmd.ExecuteNonQuery();
    }

    internal static int ExecuteSql(this IDbCommand dbCmd, string sql, object anonType, Action<IDbCommand> commandFilter = null)
    {
        if (anonType != null)
            dbCmd.SetParameters(anonType.ToObjectDictionary(), excludeDefaults: false, sql:ref sql);

        dbCmd.CommandText = sql;

        commandFilter?.Invoke(dbCmd);

        if (Log.IsDebugEnabled)
            Log.DebugCommand(dbCmd);

        OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

        if (OrmLiteConfig.ResultsFilter != null)
            return OrmLiteConfig.ResultsFilter.ExecuteSql(dbCmd);

        return dbCmd.ExecuteNonQuery();
    }

    private static bool IgnoreAlreadyExistsError(Exception ex)
    {
        //ignore Sqlite table already exists error
        const string sqliteAlreadyExistsError = "already exists";
        const string sqlServerAlreadyExistsError = "There is already an object named";
        return ex.Message.Contains(sqliteAlreadyExistsError)
               || ex.Message.Contains(sqlServerAlreadyExistsError);
    }

    private static bool IgnoreAlreadyExistsGeneratorError(Exception ex)
    {
        const string fbError = "attempt to store duplicate value";
        const string fbAlreadyExistsError = "already exists";
        return ex.Message.Contains(fbError) || ex.Message.Contains(fbAlreadyExistsError);
    }

    public static T PopulateWithSqlReader<T>(this T objWithProperties, IOrmLiteDialectProvider dialectProvider, IDataReader reader)
    {
        var indexCache = reader.GetIndexFieldsCache(ModelDefinition<T>.Definition, dialectProvider);
        var values = new object[reader.FieldCount];
        return PopulateWithSqlReader(objWithProperties, dialectProvider, reader, indexCache, values);
    }

    public static int GetColumnIndex(this IDataReader reader, IOrmLiteDialectProvider dialectProvider, string fieldName)
    {
        try
        {
            return reader.GetOrdinal(dialectProvider.NamingStrategy.GetColumnName(fieldName));
        }
        catch (IndexOutOfRangeException /*ignoreNotFoundExInSomeProviders*/)
        {
            return NotFound;
        }
    }

    private const int NotFound = -1;

    public static T PopulateWithSqlReader<T>(this T objWithProperties,
        IOrmLiteDialectProvider dialectProvider, IDataReader reader,
        Tuple<FieldDefinition, int, IOrmLiteConverter>[] indexCache, object[] values)
    {
        dialectProvider.PopulateObjectWithSqlReader<T>(objWithProperties, reader, indexCache, values);
        return objWithProperties;
    }
        
    public static void PopulateObjectWithSqlReader<T>(this IOrmLiteDialectProvider dialectProvider, 
        object objWithProperties, IDataReader reader, 
        Tuple<FieldDefinition, int, IOrmLiteConverter>[] indexCache, object[] values)
    {
        values = PopulateValues(reader, values, dialectProvider);

        var dbNullFilter = OrmLiteConfig.OnDbNullFilter;
        FieldDefinition fieldDef = null;
        object value = null;

        foreach (var fieldCache in indexCache)
        {
            try
            {
                fieldDef = fieldCache.Item1;
                var index = fieldCache.Item2;
                var converter = fieldCache.Item3;

                if (values != null && values[index] == DBNull.Value)
                {
                    value = fieldDef.IsNullable ? null : fieldDef.FieldTypeDefaultValue;
                    var useValue = dbNullFilter?.Invoke(fieldDef);
                    if (useValue != null)
                        value = useValue;

                    fieldDef.SetValue(objWithProperties, value);
                }
                else
                {
                    value = converter.GetValue(reader, index, values);
                    if (value == null)
                    {
                        if (!fieldDef.IsNullable)
                            value = fieldDef.FieldTypeDefaultValue;
                        var useValue = dbNullFilter?.Invoke(fieldDef);
                        if (useValue != null)
                            value = useValue;
                        fieldDef.SetValue(objWithProperties, value);
                    }
                    else
                    {
                        var fieldValue = converter.FromDbValue(fieldDef.FieldType, value);
                        fieldDef.SetValue(objWithProperties, fieldValue);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not populate {0}.{1} with {2}: {3}",
                    typeof(T).Name, fieldDef?.Name, value, ex.Message);

                if (OrmLiteConfig.ThrowOnError)
                    throw;
            }
        }
            
        OrmLiteConfig.PopulatedObjectFilter?.Invoke(objWithProperties);
    }

    internal static object[] PopulateValues(this IDataReader reader, object[] values, IOrmLiteDialectProvider dialectProvider)
    {
        if (!OrmLiteConfig.DeoptimizeReader)
        {
            values ??= new object[reader.FieldCount];

            try
            {
                dialectProvider.GetValues(reader, values);
            }
            catch (Exception ex)
            {
                values = null;
                Log.Warn("Error trying to use GetValues() from DataReader. Falling back to individual field reads...", ex);
            }
        }
        else
        {
            //Calling GetValues() on System.Data.SQLite.Core ADO.NET Provider changes behavior of reader.GetGuid()
            //So allow providers to by-pass reader.GetValues() optimization.
            values = null;
        }
        return values;
    }

    internal static int Update<T>(this IDbCommand dbCmd, T obj, Action<IDbCommand> commandFilter = null)
    {
        return dbCmd.UpdateInternal<T>(obj, commandFilter);
    }

    internal static int Update<T>(this IDbCommand dbCmd, Dictionary<string,object> obj, Action<IDbCommand> commandFilter = null)
    {
        return dbCmd.UpdateInternal<T>(obj, commandFilter);
    }

    internal static int UpdateInternal<T>(this IDbCommand dbCmd, object obj, Action<IDbCommand> commandFilter = null)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, obj.ToFilterType<T>());

        var dialectProvider = dbCmd.GetDialectProvider();
        var hadRowVersion = dialectProvider.PrepareParameterizedUpdateStatement<T>(dbCmd);
        if (string.IsNullOrEmpty(dbCmd.CommandText))
            return 0;

        dialectProvider.SetParameterValues<T>(dbCmd, obj);

        return dbCmd.UpdateAndVerify<T>(commandFilter, hadRowVersion);
    }

    internal static int UpdateAndVerify<T>(this IDbCommand dbCmd, Action<IDbCommand> commandFilter, bool hadRowVersion)
    {
        commandFilter?.Invoke(dbCmd);
        var rowsUpdated = dbCmd.ExecNonQuery();

        if (hadRowVersion && rowsUpdated == 0)
            throw new OptimisticConcurrencyException();

        return rowsUpdated;
    }

    internal static int Update<T>(this IDbCommand dbCmd, T[] objs, Action<IDbCommand> commandFilter = null)
    {
        return dbCmd.UpdateAll(objs: objs, commandFilter: commandFilter);
    }

    internal static int UpdateAll<T>(this IDbCommand dbCmd, IEnumerable<T> objs, Action<IDbCommand> commandFilter = null)
    {
        OrmLiteUtils.AssertNotAnonType<T>();

        IDbTransaction dbTrans = null;

        int count = 0;
        try
        {
            dbCmd.Transaction ??= dbTrans = dbCmd.Connection.BeginTransaction();

            var dialectProvider = dbCmd.GetDialectProvider();

            var hadRowVersion = dialectProvider.PrepareParameterizedUpdateStatement<T>(dbCmd);
            if (string.IsNullOrEmpty(dbCmd.CommandText))
                return 0;

            foreach (var obj in objs)
            {
                OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, obj);

                dialectProvider.SetParameterValues<T>(dbCmd, obj);

                commandFilter?.Invoke(dbCmd); //filters can augment SQL & only should be invoked once
                commandFilter = null;
                    
                var rowsUpdated = dbCmd.ExecNonQuery();
                if (hadRowVersion && rowsUpdated == 0) 
                    throw new OptimisticConcurrencyException();

                count += rowsUpdated;                
            }

            dbTrans?.Commit();
        }
        finally
        {
            dbTrans?.Dispose();
        }

        return count;
    }

    private static int AssertRowsUpdated(IDbCommand dbCmd, bool hadRowVersion)
    {
        var rowsUpdated = dbCmd.ExecNonQuery();
        if (hadRowVersion && rowsUpdated == 0)
            throw new OptimisticConcurrencyException();

        return rowsUpdated;
    }

    internal static int Delete<T>(this IDbCommand dbCmd, T anonType, Action<IDbCommand> commandFilter = null)
    {
        return dbCmd.Delete<T>((object)anonType, commandFilter);
    }

    internal static int Delete<T>(this IDbCommand dbCmd, object anonType, Action<IDbCommand> commandFilter = null)
    {
        var dialectProvider = dbCmd.GetDialectProvider();

        var hadRowVersion = dialectProvider.PrepareParameterizedDeleteStatement<T>(
            dbCmd, anonType.AllFieldsMap<T>());

        dialectProvider.SetParameterValues<T>(dbCmd, anonType);

        commandFilter?.Invoke(dbCmd);

        return AssertRowsUpdated(dbCmd, hadRowVersion);
    }

    internal static int DeleteNonDefaults<T>(this IDbCommand dbCmd, T filter)
    {
        var dialectProvider = dbCmd.GetDialectProvider();
        var hadRowVersion = dialectProvider.PrepareParameterizedDeleteStatement<T>(
            dbCmd, filter.AllFieldsMap<T>().NonDefaultsOnly());

        dialectProvider.SetParameterValues<T>(dbCmd, filter);

        return AssertRowsUpdated(dbCmd, hadRowVersion);
    }

    internal static int Delete<T>(this IDbCommand dbCmd, T[] objs)
    {
        if (objs.Length == 0)
            return 0;

        return DeleteAll(dbCmd, objs);
    }

    internal static int DeleteNonDefaults<T>(this IDbCommand dbCmd, T[] filters)
    {
        if (filters.Length == 0) return 0;

        return DeleteAll(dbCmd, filters, o => o.AllFieldsMap<T>().NonDefaultsOnly());
    }

    private static int DeleteAll<T>(IDbCommand dbCmd, IEnumerable<T> objs, 
        Func<object,Dictionary<string,object>> fieldValuesFn, Action<IDbCommand> commandFilter = null)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        IDbTransaction dbTrans = null;

        int count = 0;
        try
        {
            dbCmd.Transaction ??= dbTrans = dbCmd.Connection.BeginTransaction();

            var dialectProvider = dbCmd.GetDialectProvider();

            foreach (var obj in objs)
            {
                var fieldValues = fieldValuesFn != null
                    ? fieldValuesFn(obj)
                    : obj.AllFieldsMap<T>();

                dialectProvider.PrepareParameterizedDeleteStatement<T>(dbCmd, fieldValues);

                dialectProvider.SetParameterValues<T>(dbCmd, obj);

                commandFilter?.Invoke(dbCmd); //filters can augment SQL & only should be invoked once
                commandFilter = null;

                count += dbCmd.ExecNonQuery();
            }

            dbTrans?.Commit();
        }
        finally
        {
            dbTrans?.Dispose();
        }

        return count;
    }

    internal static int DeleteById<T>(this IDbCommand dbCmd, object id, Action<IDbCommand> commandFilter = null)
    {
        var sql = DeleteByIdSql<T>(dbCmd, id);

        return dbCmd.ExecuteSql(sql, commandFilter: commandFilter);
    }

    internal static string DeleteByIdSql<T>(this IDbCommand dbCmd, object id)
    {
        var modelDef = ModelDefinition<T>.Definition;
        var dialectProvider = dbCmd.GetDialectProvider();
        var idParamString = dialectProvider.GetParam();

        var sql = $"DELETE FROM {dialectProvider.GetQuotedTableName(modelDef)} " +
                  $"WHERE {dialectProvider.GetQuotedColumnName(modelDef.PrimaryKey.FieldName)} = {idParamString}";

        var idParam = dbCmd.CreateParameter();
        idParam.ParameterName = idParamString;
        idParam.Value = id;
        dbCmd.Parameters.Add(idParam);
        return sql;
    }

    internal static void DeleteById<T>(this IDbCommand dbCmd, object id, ulong rowVersion, Action<IDbCommand> commandFilter = null)
    {
        var sql = DeleteByIdSql<T>(dbCmd, id, rowVersion);

        var rowsAffected = dbCmd.ExecuteSql(sql, commandFilter: commandFilter);
        if (rowsAffected == 0)
            throw new OptimisticConcurrencyException("The row was modified or deleted since the last read");
    }

    internal static string DeleteByIdSql<T>(this IDbCommand dbCmd, object id, ulong rowVersion)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        var modelDef = ModelDefinition<T>.Definition;
        var dialectProvider = dbCmd.GetDialectProvider();

        dbCmd.Parameters.Clear();

        var idParam = dbCmd.CreateParameter();
        idParam.ParameterName = dialectProvider.GetParam();
        idParam.Value = id;
        dbCmd.Parameters.Add(idParam);

        var rowVersionField = modelDef.RowVersion;
        if (rowVersionField == null)
            throw new InvalidOperationException(
                "Cannot use DeleteById with rowVersion for model type without a row version column");

        var rowVersionParam = dbCmd.CreateParameter();
        rowVersionParam.ParameterName = dialectProvider.GetParam("rowVersion");
        var converter = dialectProvider.GetConverterBestMatch(typeof(RowVersionConverter));
        converter.InitDbParam(rowVersionParam, typeof(ulong));

        rowVersionParam.Value = converter.ToDbValue(typeof(ulong), rowVersion);
        dbCmd.Parameters.Add(rowVersionParam);

        var sql = $"DELETE FROM {dialectProvider.GetQuotedTableName(modelDef)} " +
                  $"WHERE {dialectProvider.GetQuotedColumnName(modelDef.PrimaryKey.FieldName)} = {idParam.ParameterName} " +
                  $"AND {dialectProvider.GetRowVersionColumn(rowVersionField)} = {rowVersionParam.ParameterName}";

        return sql;
    }

    internal static int DeleteByIds<T>(this IDbCommand dbCmd, IEnumerable idValues)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        var sqlIn = dbCmd.SetIdsInSqlParams(idValues);
        if (string.IsNullOrEmpty(sqlIn))
            return 0;

        var sql = GetDeleteByIdsSql<T>(sqlIn, dbCmd.GetDialectProvider());

        return dbCmd.ExecuteSql(sql);
    }

    internal static string GetDeleteByIdsSql<T>(string sqlIn, IOrmLiteDialectProvider dialectProvider)
    {
        var modelDef = ModelDefinition<T>.Definition;

        var sql = $"DELETE FROM {dialectProvider.GetQuotedTableName(modelDef)} " +
                  $"WHERE {dialectProvider.GetQuotedColumnName(modelDef.PrimaryKey.FieldName)} IN ({sqlIn})";
        return sql;
    }

    internal static int DeleteAll<T>(this IDbCommand dbCmd)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        return DeleteAll(dbCmd, typeof(T));
    }

    internal static int DeleteAll<T>(this IDbCommand dbCmd, IEnumerable<T> rows)
    {
        var ids = rows.Map(x => x.GetId());
        return dbCmd.DeleteByIds<T>(ids);
    }

    internal static int DeleteAll(this IDbCommand dbCmd, Type tableType)
    {
        return dbCmd.ExecuteSql(dbCmd.GetDialectProvider().ToDeleteStatement(tableType, null));
    }

    internal static int Delete<T>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);
        return dbCmd.ExecuteSql(dbCmd.GetDialectProvider().ToDeleteStatement(typeof(T), sql));
    }

    internal static int Delete(this IDbCommand dbCmd, Type tableType, string sql, object anonType = null)
    {
        if (anonType != null) dbCmd.SetParameters(tableType, anonType, excludeDefaults: false, sql: ref sql);
        return dbCmd.ExecuteSql(dbCmd.GetDialectProvider().ToDeleteStatement(tableType, sql));
    }
        
    internal static long Insert<T>(this IDbCommand dbCmd, T obj, Action<IDbCommand> commandFilter, bool selectIdentity = false, bool enableIdentityInsert=false)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj);

        var dialectProvider = dbCmd.GetDialectProvider();
        var pkField = ModelDefinition<T>.Definition.FieldDefinitions.FirstOrDefault(f => f.IsPrimaryKey);
        if (!enableIdentityInsert || pkField is not { AutoIncrement: true })
        {
            dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd,
                insertFields: dialectProvider.GetNonDefaultValueInsertFields<T>(obj));

            return InsertInternal<T>(dialectProvider, dbCmd, obj, commandFilter, selectIdentity);
        }

        try
        {
            dialectProvider.EnableIdentityInsert<T>(dbCmd);
            dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd,
                insertFields: dialectProvider.GetNonDefaultValueInsertFields<T>(obj),
                shouldInclude: f => f == pkField);
            InsertInternal<T>(dialectProvider, dbCmd, obj, commandFilter, selectIdentity);
            if (selectIdentity)
            {
                var id = pkField.GetValue(obj);
                return Convert.ToInt64(id);
            }
            return default;
        }
        finally
        {
            dialectProvider.DisableIdentityInsert<T>(dbCmd);
        }
    }
        
    internal static long Insert<T>(this IDbCommand dbCmd, Dictionary<string,object> obj, 
        Action<IDbCommand> commandFilter, bool selectIdentity = false)
    {
        OrmLiteUtils.AssertNotAnonType<T>();
            
        OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj.ToFilterType<T>());

        var dialectProvider = dbCmd.GetDialectProvider();
        var modelDef = ModelDefinition<T>.Definition;
        var pkField = modelDef.PrimaryKey;
        object id = null;
        var enableIdentityInsert = pkField?.AutoIncrement == true && obj.TryGetValue(pkField.Name, out id);

        try
        {
            if (enableIdentityInsert)
                dialectProvider.EnableIdentityInsert<T>(dbCmd);
                
            dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd, 
                insertFields: dialectProvider.GetNonDefaultValueInsertFields<T>(obj),
                shouldInclude: f => obj.ContainsKey(f.Name));

            var ret = InsertInternal<T>(dialectProvider, dbCmd, obj, commandFilter, selectIdentity);
            if (enableIdentityInsert)
                ret = Convert.ToInt64(id);

            if (modelDef.HasAnyReferences(obj.Keys))
            {
                if (pkField != null && !obj.ContainsKey(pkField.Name))
                    obj[pkField.Name] = ret;

                var instance = obj.FromObjectDictionary<T>();
                dbCmd.SaveAllReferences(instance);
            }
            return ret;
        }
        finally
        {
            if (enableIdentityInsert)
                dialectProvider.DisableIdentityInsert<T>(dbCmd);
        }
    }
        
    internal static void RemovePrimaryKeyWithDefaultValue<T>(this Dictionary<string, object> obj)
    {
        var pkField = typeof(T).GetModelDefinition().PrimaryKey;
        if (pkField != null && (pkField.AutoIncrement || pkField.AutoId))
        {
            // Don't insert Primary Key with Default Value
            if (obj.TryGetValue(pkField.Name, out var idValue) &&
                idValue != null && !idValue.Equals(pkField.DefaultValue))
            {
                obj.Remove(pkField.Name);
            }
        }
    }

    private static long InsertInternal<T>(IOrmLiteDialectProvider dialectProvider, IDbCommand dbCmd, object obj, Action<IDbCommand> commandFilter, bool selectIdentity)
    {
        OrmLiteUtils.AssertNotAnonType<T>();

        dialectProvider.SetParameterValues<T>(dbCmd, obj);

        commandFilter?.Invoke(dbCmd); //dbCmd.OnConflictInsert() needs to be applied before last insert id

        if (dialectProvider.HasInsertReturnValues(ModelDefinition<T>.Definition))
        {
            using var reader = dbCmd.ExecReader(dbCmd.CommandText);
            return reader.PopulateReturnValues<T>(dialectProvider, obj);
        }

        if (selectIdentity)
        {
            dbCmd.CommandText += dialectProvider.GetLastInsertIdSqlSuffix<T>();

            return dbCmd.ExecLongScalar();
        }

        return dbCmd.ExecNonQuery();
    }

    internal static long PopulateReturnValues<T>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider, object obj)
    {
        if (reader.Read())
        {
            var modelDef = ModelDefinition<T>.Definition;
            var values = new object[reader.FieldCount];
            var indexCache = reader.GetIndexFieldsCache(modelDef, dialectProvider);
            dialectProvider.PopulateObjectWithSqlReader<T>(obj, reader, indexCache, values);
            if ((modelDef.PrimaryKey != null) && (modelDef.PrimaryKey.AutoIncrement || modelDef.PrimaryKey.ReturnOnInsert))
            {
                var id = modelDef.GetPrimaryKey(obj);
                if (modelDef.PrimaryKey.AutoId)
                    return 1;
                return Convert.ToInt64(id);
            }
        }

        return 0;
    }

    internal static void Insert<T>(this IDbCommand dbCmd, Action<IDbCommand> commandFilter, params T[] objs)
    {
        dbCmd.InsertAll(objs: objs, commandFilter: commandFilter);
    }

    internal static long InsertIntoSelect<T>(this IDbCommand dbCmd, ISqlExpression query, Action<IDbCommand> commandFilter) => 
        dbCmd.InsertIntoSelectInternal<T>(query, commandFilter).ExecNonQuery();
        
    internal static IDbCommand InsertIntoSelectInternal<T>(this IDbCommand dbCmd, ISqlExpression query, Action<IDbCommand> commandFilter)
    {
        var dialectProvider = dbCmd.GetDialectProvider();

        var sql = query.ToSelectStatement(QueryType.Select);
        var selectFields = query.GetUntypedSqlExpression()
            .SelectExpression
            .Substring("SELECT ".Length)
            .ParseCommands();

        var fieldsOrAliases = selectFields
            .Map(x => x.Original.ToString().AliasOrColumn());

        dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd, insertFields: fieldsOrAliases);

        dbCmd.SetParameters(query.Params);

        dbCmd.CommandText = dbCmd.CommandText.LeftPart(")") + ")\n" + sql;

        commandFilter?.Invoke(dbCmd); //dbCmd.OnConflictInsert() needs to be applied before last insert id
        return dbCmd;
    }

    internal static void InsertAll<T>(this IDbCommand dbCmd, IEnumerable<T> objs, Action<IDbCommand> commandFilter)
    {
        IDbTransaction dbTrans = null;

        try
        {
            dbCmd.Transaction ??= dbTrans = dbCmd.Connection.BeginTransaction();

            var dialectProvider = dbCmd.GetDialectProvider();

            dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd);

            foreach (var obj in objs)
            {
                OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj);
                dialectProvider.SetParameterValues<T>(dbCmd, obj);

                commandFilter?.Invoke(dbCmd); //filters can augment SQL & only should be invoked once
                commandFilter = null;

                try
                {
                    dbCmd.ExecNonQuery();
                }
                catch (Exception ex)
                {
                    Log.Error($"SQL ERROR: {dbCmd.GetLastSqlAndParams()}", ex);
                    throw;
                }
            }

            dbTrans?.Commit();
        }
        finally
        {
            dbTrans?.Dispose();
        }
    }

    internal static void InsertUsingDefaults<T>(this IDbCommand dbCmd, params T[] objs)
    {
        IDbTransaction dbTrans = null;

        try
        {
            dbCmd.Transaction ??= dbTrans = dbCmd.Connection.BeginTransaction();

            var dialectProvider = dbCmd.GetDialectProvider();

            var modelDef = typeof(T).GetModelDefinition();
            var fieldsWithoutDefaults = modelDef.FieldDefinitionsArray
                .Where(x => x.DefaultValue == null)
                .Select(x => x.Name)
                .ToSet(); 

            dialectProvider.PrepareParameterizedInsertStatement<T>(dbCmd,
                insertFields: fieldsWithoutDefaults);

            foreach (var obj in objs)
            {
                OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj);
                dialectProvider.SetParameterValues<T>(dbCmd, obj);

                try
                {
                    dbCmd.ExecNonQuery();
                }
                catch (Exception ex)
                {
                    Log.Error($"SQL ERROR: {dbCmd.GetLastSqlAndParams()}", ex);
                    throw;
                }
            }

            dbTrans?.Commit();
        }
        finally
        {
            dbTrans?.Dispose();
        }
    }

    internal static int Save<T>(this IDbCommand dbCmd, params T[] objs)
    {
        return SaveAll(dbCmd, objs);
    }

    internal static bool Save<T>(this IDbCommand dbCmd, T obj)
    {
        var modelDef = typeof(T).GetModelDefinition();
        var id = modelDef.GetPrimaryKey(obj);
        var existingRow = id != null && !id.Equals(id.GetType().GetDefaultValue()) 
            ? dbCmd.SingleById<T>(id) 
            : default;

        if (Equals(existingRow, default(T)))
        {
            if (modelDef.HasAutoIncrementId)
            {
                var dialectProvider = dbCmd.GetDialectProvider();
                var newId = dbCmd.Insert(obj, commandFilter:null, selectIdentity: true);
                var safeId = dialectProvider.FromDbValue(newId, modelDef.PrimaryKey.FieldType);
                modelDef.PrimaryKey.SetValue(obj, safeId);
                id = newId;
            }
            else
            {
                dbCmd.Insert(obj, commandFilter:null);
            }

            modelDef.RowVersion?.SetValue(obj, dbCmd.GetRowVersion(modelDef, id));

            return true;
        }

        var rowsUpdated = dbCmd.Update(obj);
        if (rowsUpdated == 0 && Env.StrictMode)
            throw new OptimisticConcurrencyException("No rows were inserted or updated");

        modelDef.RowVersion?.SetValue(obj, dbCmd.GetRowVersion(modelDef, id));

        return false;
    }

    internal static int SaveAll<T>(this IDbCommand dbCmd, IEnumerable<T> objs)
    {
        var saveRows = objs.ToList();

        var firstRow = saveRows.FirstOrDefault();
        if (Equals(firstRow, default(T))) return 0;

        var modelDef = typeof(T).GetModelDefinition();

        var firstRowId = modelDef.GetPrimaryKey(firstRow);
        var defaultIdValue = firstRowId?.GetType().GetDefaultValue();

        var idMap = defaultIdValue != null
            ? saveRows.Where(x => !defaultIdValue.Equals(modelDef.GetPrimaryKey(x))).ToSafeDictionary(x => modelDef.GetPrimaryKey(x))
            : saveRows.Where(x => modelDef.GetPrimaryKey(x) != null).ToSafeDictionary(x => modelDef.GetPrimaryKey(x));

        var existingRowsMap = dbCmd.SelectByIds<T>(idMap.Keys).ToDictionary(x => modelDef.GetPrimaryKey(x));

        var rowsAdded = 0;

        IDbTransaction dbTrans = null;

        dbCmd.Transaction ??= dbTrans = dbCmd.Connection!.BeginTransaction();

        try
        {
            var dialect = dbCmd.Dialect();
            foreach (var row in saveRows)
            {
                var id = modelDef.GetPrimaryKey(row);
                if (id != defaultIdValue && existingRowsMap.ContainsKey(id))
                {
                    dbCmd.Update(row);
                }
                else
                {
                    if (modelDef.HasAutoIncrementId)
                    {
                        var newId = dbCmd.Insert(row, commandFilter: null, selectIdentity: true);
                        var safeId = dialect.FromDbValue(newId, modelDef.PrimaryKey.FieldType);
                        modelDef.PrimaryKey.SetValue(row, safeId);
                        id = newId;
                    }
                    else
                    {
                        dbCmd.Insert(row, commandFilter: null);
                    }

                    rowsAdded++;
                }

                modelDef.RowVersion?.SetValue(row, dbCmd.GetRowVersion(modelDef, id));
            }

            dbTrans?.Commit();
        }
        finally
        {
            dbTrans?.Dispose();
            if (dbCmd.Transaction == dbTrans)
                dbCmd.Transaction = null;
        }

        return rowsAdded;
    }

    internal static void SaveAllReferences<T>(this IDbCommand dbCmd, T instance) => 
        SaveAllReferences(dbCmd, ModelDefinition<T>.Definition, instance);

    internal static void SaveAllReferences(IDbCommand dbCmd, ModelDefinition modelDef, object instance)
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
                    dbCmd.CreateTypedApi(refType).SaveAll(results);
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
                    if (refField != null && refSelf == null)
                        refField.SetValue(result, pkValue);

                    dbCmd.CreateTypedApi(refType).Save(result);

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
            dbCmd.CreateTypedApi(instance.GetType()).Update(instance);
        }
    }

    internal static void SaveReferences<T, TRef>(this IDbCommand dbCmd, T instance, params TRef[] refs)
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

        dbCmd.SaveAll(refs);

        foreach (var oRef in refs)
        {
            //Save Self Table.RefTableId PK
            if (refSelf != null)
            {
                var refPkValue = refModelDef.PrimaryKey.GetValue(oRef);
                refSelf.SetValue(instance, refPkValue);
                dbCmd.Update(instance);
            }
        }
    }

    // Procedures
    internal static void ExecuteProcedure<T>(this IDbCommand dbCmd, T obj)
    {
        var dialectProvider = dbCmd.GetDialectProvider();
        dialectProvider.PrepareStoredProcedureStatement(dbCmd, obj);
        dbCmd.ExecNonQuery();
    }

    internal static object GetRowVersion(this IDbCommand dbCmd, ModelDefinition modelDef, object id)
    {
        var sql = RowVersionSql(dbCmd, modelDef, id);
        var to = dbCmd.GetDialectProvider().FromDbRowVersion(modelDef.RowVersion.FieldType, dbCmd.Scalar<object>(sql));

        if (to is ulong u && modelDef.RowVersion.ColumnType == typeof(byte[]))
            return BitConverter.GetBytes(u);

        return to ?? modelDef.RowVersion.ColumnType.GetDefaultValue();
    }

    internal static string RowVersionSql(this IDbCommand dbCmd, ModelDefinition modelDef, object id)
    {
        var dialectProvider = dbCmd.GetDialectProvider();
        var idParamString = dialectProvider.GetParam();

        var sql = $"SELECT {dialectProvider.GetRowVersionSelectColumn(modelDef.RowVersion)} " +
                  $"FROM {dialectProvider.GetQuotedTableName(modelDef)} " +
                  $"WHERE {dialectProvider.GetQuotedColumnName(modelDef.PrimaryKey.FieldName)} = {idParamString}";

        dbCmd.Parameters.Clear();
        var idParam = dbCmd.CreateParameter();
        idParam.Direction = ParameterDirection.Input;
        idParam.ParameterName = idParamString;

        dialectProvider.SetParamValue(idParam, id, modelDef.PrimaryKey.ColumnType, modelDef.PrimaryKey);

        dbCmd.Parameters.Add(idParam);
        return sql;
    }
}