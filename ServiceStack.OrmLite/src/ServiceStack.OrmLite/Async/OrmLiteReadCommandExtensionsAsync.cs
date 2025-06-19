// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.OrmLite.Support;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

internal static class OrmLiteReadCommandExtensionsAsync
{
    internal static ILog Log = LogManager.GetLogger(typeof(OrmLiteReadCommandExtensionsAsync));

    internal static Task<IDataReader> ExecReaderAsync(this IDbCommand dbCmd, string sql, CancellationToken token)
    {
        dbCmd.CommandText = sql;

        if (Log.IsDebugEnabled)
            Log.DebugCommand(dbCmd);

        OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

        return dbCmd.GetDialectProvider().ExecuteReaderAsync(dbCmd, token);
    }

    internal static Task<IDataReader> ExecReaderAsync(this IDbCommand dbCmd, string sql, IEnumerable<IDataParameter> parameters, CancellationToken token)
    {
        dbCmd.CommandText = sql;
        dbCmd.Parameters.Clear();

        foreach (var param in parameters)
        {
            dbCmd.Parameters.Add(param);
        }

        if (Log.IsDebugEnabled)
            Log.DebugCommand(dbCmd);

        OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

        return dbCmd.GetDialectProvider().ExecuteReaderAsync(dbCmd, token);
    }

    internal static Task<List<T>> SelectAsync<T>(this IDbCommand dbCmd, CancellationToken token)
    {
        return SelectAsync<T>(dbCmd, (string)null, (object)null, token);
    }

    internal static Task<List<TModel>> SelectAsync<TModel>(this IDbCommand dbCmd, Type fromTableType, CancellationToken token)
    {
        return SelectAsync<TModel>(dbCmd, fromTableType, null, null, token);
    }

    internal static Task<List<TModel>> SelectAsync<TModel>(this IDbCommand dbCmd, Type fromTableType, string sqlFilter, object anonType, CancellationToken token)
    {
        if (anonType != null) dbCmd.SetParameters(fromTableType, anonType, excludeDefaults: false, sql: ref sqlFilter);
        var sql = OrmLiteReadCommandExtensions.ToSelect<TModel>(dbCmd.GetDialectProvider(), fromTableType, sqlFilter);
        return dbCmd.ConvertToListAsync<TModel>(sql, token);
    }

    internal static Task<List<T>> SelectByIdsAsync<T>(this IDbCommand dbCmd, IEnumerable idValues, CancellationToken token)
    {
        var sqlIn = dbCmd.SetIdsInSqlParams(idValues);
        return string.IsNullOrEmpty(sqlIn)
            ? new List<T>().InTask()
            : SelectAsync<T>(dbCmd, dbCmd.GetDialectProvider().GetQuotedColumnName(ModelDefinition<T>.Definition.PrimaryKey) + " IN (" + sqlIn + ")", (object)null, token);
    }

    internal static Task<T> SingleByIdAsync<T>(this IDbCommand dbCmd, object value, CancellationToken token)
    {
        dbCmd.SetFilter<T>(ModelDefinition<T>.PrimaryKeyName, value);
        return dbCmd.ConvertToAsync<T>(null, token);
    }

    internal static Task<T> SingleWhereAsync<T>(this IDbCommand dbCmd, string name, object value, CancellationToken token)
    {
        dbCmd.SetFilter<T>(name, value);
        return dbCmd.ConvertToAsync<T>(null, token);
    }

    internal static Task<T> SingleAsync<T>(this IDbCommand dbCmd, object anonType, CancellationToken token)
    {
        return dbCmd.SetFilters<T>(anonType, excludeDefaults: false).ConvertToAsync<T>(null, token);
    }

    internal static Task<T> SingleAsync<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token)
    {
        return OrmLiteUtils.IsScalar<T>()
            ? dbCmd.ScalarAsync<T>(sql, sqlParams, token)
            : dbCmd.SetParameters(sqlParams).ConvertToAsync<T>(dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql), token);
    }

    internal static Task<T> SingleAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        return OrmLiteUtils.IsScalar<T>()
            ? dbCmd.ScalarAsync<T>(sql, anonType, token)
            : dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql).ConvertToAsync<T>(dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql), token);
    }

    internal static Task<List<T>> WhereAsync<T>(this IDbCommand dbCmd, string name, object value, CancellationToken token)
    {
        dbCmd.SetFilter<T>(name, value);
        return dbCmd.ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> WhereAsync<T>(this IDbCommand dbCmd, object anonType, CancellationToken token)
    {
        return dbCmd.SetFilters<T>(anonType).ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> SelectAsync<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token)
    {
        dbCmd.SetParameters(sqlParams).CommandText = dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql);
        return dbCmd.ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> SelectAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql).CommandText = dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql);
        return dbCmd.ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> SelectAsync<T>(this IDbCommand dbCmd, string sql, Dictionary<string, object> dict, CancellationToken token)
    {
        dbCmd.SetParameters(dict, excludeDefaults: false, sql:ref sql).CommandText = dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql);
        return dbCmd.ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> SqlListAsync<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token)
    {
        dbCmd.SetParameters(sqlParams).CommandText = sql;
        return dbCmd.ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> SqlListAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql).CommandText = sql;
        return dbCmd.ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> SqlListAsync<T>(this IDbCommand dbCmd, string sql, Dictionary<string, object> dict, CancellationToken token)
    {
        dbCmd.SetParameters(dict, excludeDefaults: false, sql:ref sql).CommandText = sql;
        return dbCmd.ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> SqlListAsync<T>(this IDbCommand dbCmd, string sql, Action<IDbCommand> dbCmdFilter, CancellationToken token)
    {
        dbCmdFilter?.Invoke(dbCmd);
        dbCmd.CommandText = sql;

        return dbCmd.ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> SqlColumnAsync<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token)
    {
        dbCmd.SetParameters(sqlParams).CommandText = sql;
        return dbCmd.ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> SqlColumnAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        dbCmd.SetParameters(anonType.ToObjectDictionary(), excludeDefaults: false, sql:ref sql).CommandText = sql;
        return dbCmd.ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> SqlColumnAsync<T>(this IDbCommand dbCmd, string sql, Dictionary<string, object> dict, CancellationToken token)
    {
        dbCmd.SetParameters(dict, excludeDefaults: false, sql:ref sql).CommandText = sql;
        return dbCmd.ConvertToListAsync<T>(null, token);
    }

    internal static Task<T> SqlScalarAsync<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token)
    {
        return dbCmd.SetParameters(sqlParams).ScalarAsync<T>(sql, token);
    }

    internal static Task<T> SqlScalarAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        return dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql).ScalarAsync<T>(sql, token);
    }

    internal static Task<T> SqlScalarAsync<T>(this IDbCommand dbCmd, string sql, Dictionary<string, object> dict, CancellationToken token)
    {
        return dbCmd.SetParameters(dict, excludeDefaults: false, sql:ref sql).ScalarAsync<T>(sql, token);
    }

    internal static Task<List<T>> SelectNonDefaultsAsync<T>(this IDbCommand dbCmd, object filter, CancellationToken token)
    {
        return dbCmd.SetFilters<T>(filter, excludeDefaults: true).ConvertToListAsync<T>(null, token);
    }

    internal static Task<List<T>> SelectNonDefaultsAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        return dbCmd.SetParameters<T>(anonType, excludeDefaults: true, sql: ref sql).ConvertToListAsync<T>(dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql), token);
    }

    internal static Task<T> ScalarAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        return dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql).ScalarAsync<T>(sql, token);
    }

    internal static Task<T> ScalarAsync<T>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider, CancellationToken token)
    {
        return dialectProvider.ReaderRead(reader, () => 
            OrmLiteReadCommandExtensions.ToScalar<T>(dialectProvider, reader), token);
    }

    public static async Task<long> LongScalarAsync(this IDbCommand dbCmd, CancellationToken token)
    {
        var ret = await dbCmd.GetDialectProvider().ExecuteScalarAsync(dbCmd, token).ConfigAwait();
        return OrmLiteReadCommandExtensions.ToLong(ret);
    }

    internal static Task<List<T>> ColumnAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);

        return dbCmd.ColumnAsync<T>(dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql), token);
    }

    internal static async Task<List<T>> ColumnAsync<T>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider, CancellationToken token)
    {
        var ret = await dialectProvider.ReaderEach(reader, () =>
        {
            var value = dialectProvider.FromDbValue(reader, 0, typeof(T));
            return value == DBNull.Value ? default(T) : value;
        }, token).ConfigAwait();
                
        var columnValues = new List<T>();
        ret.Each(o => columnValues.Add((T)o));
        return columnValues;
    }

    internal static Task<HashSet<T>> ColumnDistinctAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);

        return dbCmd.ColumnDistinctAsync<T>(sql, token);
    }

    internal static async Task<HashSet<T>> ColumnDistinctAsync<T>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider, CancellationToken token)
    {
        var ret = await dialectProvider.ReaderEach(reader, () =>
        {
            var value = dialectProvider.FromDbValue(reader, 0, typeof(T));
            return value == DBNull.Value ? default(T) : value;
        }, token).ConfigAwait();
                
        var columnValues = new HashSet<T>();
        ret.Each(o => columnValues.Add((T)o));
        return columnValues;
    }

    internal static Task<Dictionary<K, List<V>>> LookupAsync<K, V>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        if (anonType != null) 
            dbCmd.SetParameters(anonType.ToObjectDictionary(), (bool)false, sql:ref sql);

        return dbCmd.LookupAsync<K, V>(sql, token);
    }

    internal static Task<Dictionary<K, List<V>>> LookupAsync<K, V>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider, CancellationToken token)
    {
        var lookup = new Dictionary<K, List<V>>();

        return dialectProvider.ReaderEach(reader, () =>
        {
            var key = (K)dialectProvider.FromDbValue(reader, 0, typeof(K));
            var value = (V)dialectProvider.FromDbValue(reader, 1, typeof(V));

            if (!lookup.TryGetValue(key, out var values))
            {
                values = new List<V>();
                lookup[key] = values;
            }
            values.Add(value);
        }, lookup, token);
    }

    internal static Task<Dictionary<K, V>> DictionaryAsync<K, V>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        if (anonType != null) 
            dbCmd.SetParameters(anonType.ToObjectDictionary(), excludeDefaults: false, sql:ref sql);

        return dbCmd.DictionaryAsync<K, V>(sql, token);
    }

    internal static Task<Dictionary<K, V>> DictionaryAsync<K, V>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider, CancellationToken token)
    {
        var map = new Dictionary<K, V>();

        return dialectProvider.ReaderEach(reader, () =>
        {
            var key = (K)dialectProvider.FromDbValue(reader, 0, typeof(K));
            var value = (V)dialectProvider.FromDbValue(reader, 1, typeof(V));
            map.Add(key, value);
        }, map, token);
    }

    internal static Task<List<KeyValuePair<K, V>>> KeyValuePairsAsync<K, V>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        if (anonType != null) 
            dbCmd.SetParameters(anonType.ToObjectDictionary(), excludeDefaults: false, sql:ref sql);

        return dbCmd.KeyValuePairsAsync<K, V>(sql, token);
    }

    internal static Task<List<KeyValuePair<K, V>>> KeyValuePairsAsync<K, V>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider, CancellationToken token)
    {
        var to = new List<KeyValuePair<K, V>>();

        return dialectProvider.ReaderEach(reader, () =>
        {
            var key = (K)dialectProvider.FromDbValue(reader, 0, typeof(K));
            var value = (V)dialectProvider.FromDbValue(reader, 1, typeof(V));
            to.Add(new KeyValuePair<K, V>(key, value));
        }, to, token);
    }

    internal static async Task<bool> ExistsAsync<T>(this IDbCommand dbCmd, object anonType, CancellationToken token)
    {
        string sql = null;
        if (anonType != null) dbCmd.SetParameters(anonType.ToObjectDictionary(), excludeDefaults: true, sql:ref sql);

        sql = dbCmd.GetFilterSql<T>();

        var ret = await dbCmd.ScalarAsync(sql, token).ConfigAwait();
        return ret != null;
    }

    internal static async Task<bool> ExistsAsync<T>(this IDbCommand dbCmd, string sql, object anonType, CancellationToken token)
    {
        if (anonType != null) dbCmd.SetParameters(anonType.ToObjectDictionary(), (bool)false, sql:ref sql);

        var ret = await dbCmd.ScalarAsync(dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql), token).ConfigAwait();
        return ret != null;
    }

    // procedures ...		
    internal static Task<List<TOutputModel>> SqlProcedureAsync<TOutputModel>(this IDbCommand dbCommand, object fromObjWithProperties, CancellationToken token)
    {
        return SqlProcedureFmtAsync<TOutputModel>(dbCommand, token, fromObjWithProperties, String.Empty);
    }

    internal static Task<List<TOutputModel>> SqlProcedureFmtAsync<TOutputModel>(this IDbCommand dbCmd, CancellationToken token,
        object fromObjWithProperties,
        string sqlFilter,
        params object[] filterParams)
    {
        var modelType = typeof(TOutputModel);

        string sql = dbCmd.GetDialectProvider().ToSelectFromProcedureStatement(
            fromObjWithProperties, modelType, sqlFilter, filterParams);

        return dbCmd.ConvertToListAsync<TOutputModel>(sql, token);
    }

    internal static async Task<T> LoadSingleByIdAsync<T>(this IDbCommand dbCmd, object value, string[] include = null, CancellationToken token = default)
    {
        var row = await dbCmd.SingleByIdAsync<T>(value, token).ConfigAwait();
        if (row == null)
            return default(T);

        await dbCmd.LoadReferencesAsync(row, include, token).ConfigAwait();

        return row;
    }

    public static async Task LoadReferencesAsync<T>(this IDbCommand dbCmd, T instance, string[] include = null, CancellationToken token = default)
    {
        var loadRef = new LoadReferencesAsync<T>(dbCmd, instance);
        var fieldDefs = loadRef.FieldDefs;

        var includeSet = include != null
            ? new HashSet<string>(include, StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (var fieldDef in fieldDefs)
        {
            if (includeSet != null && !includeSet.Contains(fieldDef.Name))
                continue;

            dbCmd.Parameters.Clear();
            var listInterface = fieldDef.FieldType.GetTypeWithGenericInterfaceOf(typeof(IList<>));
            if (listInterface != null)
            {
                await loadRef.SetRefFieldList(fieldDef, listInterface.GetGenericArguments()[0], token).ConfigAwait();
            }
            else if (fieldDef.FieldReference != null)
            {
                await loadRef.SetFieldReference(fieldDef, fieldDef.FieldReference, token).ConfigAwait();
            }
            else
            {
                await loadRef.SetRefField(fieldDef, fieldDef.FieldType, token).ConfigAwait();
            }
        }
    }

    internal static async Task<List<Into>> LoadListWithReferences<Into, From>(this IDbCommand dbCmd, SqlExpression<From> expr = null, IEnumerable<string> include = null, CancellationToken token = default)
    {
        var loadList = new LoadListAsync<Into, From>(dbCmd, expr);

        var fieldDefs = loadList.FieldDefs;

        var includeSet = include != null 
            ? new HashSet<string>(include, StringComparer.OrdinalIgnoreCase)
            : null;
            
        foreach (var fieldDef in fieldDefs)
        {
            if (includeSet != null && !includeSet.Contains(fieldDef.Name))
                continue;

            var listInterface = fieldDef.FieldType.GetTypeWithGenericInterfaceOf(typeof(IList<>));
            if (listInterface != null)
            {
                await loadList.SetRefFieldListAsync(fieldDef, listInterface.GetGenericArguments()[0], token).ConfigAwait();
            }
            else if (fieldDef.FieldReference != null)
            {
                await loadList.SetFieldReferenceAsync(fieldDef, fieldDef.FieldReference, token).ConfigAwait();
            }
            else
            {
                await loadList.SetRefFieldAsync(fieldDef, fieldDef.FieldType, token).ConfigAwait();
            }
        }

        return loadList.ParentResults;
    }
}
