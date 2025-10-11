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
using System.Globalization;
using System.Reflection;
using System.Text;
using ServiceStack.Logging;
using System.Linq;
using ServiceStack.OrmLite.Support;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

public delegate object GetValueDelegate(int i);

public static class OrmLiteReadCommandExtensions
{
    internal static ILog Log => OrmLiteLog.Log;

    internal static IDataReader ExecReader(this IDbCommand dbCmd, string sql)
    {
        dbCmd.CommandText = sql;

        if (Log.IsDebugEnabled)
            Log.DebugCommand(dbCmd);

        OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

        return dbCmd.WithLog(dbCmd.ExecuteReader());
    }

    internal static IDataReader ExecReader(this IDbCommand dbCmd, string sql, CommandBehavior commandBehavior)
    {
        dbCmd.CommandText = sql;

        if (Log.IsDebugEnabled)
            Log.DebugCommand(dbCmd);

        OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

        return dbCmd.WithLog(dbCmd.ExecuteReader(commandBehavior));
    }

    internal static IDataReader ExecReader(this IDbCommand dbCmd, string sql, IEnumerable<IDataParameter> parameters)
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

        return dbCmd.WithLog(dbCmd.ExecuteReader());
    }

    internal static List<T> Select<T>(this IDbCommand dbCmd)
    {
        return Select<T>(dbCmd, (string)null);
    }

    internal static void SetFilter<T>(this IDbCommand dbCmd, string name, object value)
    {
        var dialectProvider = dbCmd.GetDialectProvider();

        dbCmd.Parameters.Clear();
        var p = dbCmd.CreateParameter();
        p.ParameterName = name;
        p.Direction = ParameterDirection.Input;
        dialectProvider.InitDbParam(p, value.GetType(), value);

        dbCmd.Parameters.Add(p);
        dbCmd.CommandText = GetFilterSql<T>(dbCmd);
    }

    internal static IDbCommand SetFilters<T>(this IDbCommand dbCmd, object anonType, bool excludeDefaults)
    {
        string ignore = null;
        dbCmd.SetParameters<T>(anonType, excludeDefaults, ref ignore); //needs to be called first
        dbCmd.CommandText = dbCmd.GetFilterSql<T>();
        return dbCmd;
    }

    internal static void PopulateWith(this IDbCommand dbCmd, ISqlExpression expression, QueryType queryType)
    {
        dbCmd.CommandText = expression.ToSelectStatement(queryType); //needs to evaluate SQL before setting params
        dbCmd.SetParameters(expression.Params);
    }

    internal static IDbCommand SetParameters<T>(this IDbCommand dbCmd, object anonType, bool excludeDefaults, ref string sql) => 
        dbCmd.SetParameters(typeof(T), anonType, excludeDefaults, ref sql);

    internal static IDbCommand SetParameters(this IDbCommand dbCmd, IEnumerable<IDbDataParameter> sqlParams)
    {
        if (sqlParams == null)
            return dbCmd;

        try
        {
            dbCmd.Parameters.Clear();
            foreach (var sqlParam in sqlParams)
            {
                dbCmd.Parameters.Add(sqlParam);
            }
        }
        catch (Exception ex)
        {
            //SQL Server + PostgreSql doesn't allow re-using db params in multiple queries
            if (Log.IsDebugEnabled)
                Log.Debug("Exception trying to reuse db params, executing with cloned params instead", ex);

            dbCmd.Parameters.Clear();
            foreach (var sqlParam in sqlParams)
            {
                var p = dbCmd.CreateParameter();
                p.PopulateWith(sqlParam);
                dbCmd.Parameters.Add(p);
            }
        }

        return dbCmd;
    }

    private static IEnumerable GetMultiValues(object value)
    {
        if (value is SqlInValues inValues)
            return inValues.GetValues();

        return (value is IEnumerable enumerable &&
                !(enumerable is string ||
                  enumerable is IEnumerable<KeyValuePair<string, object>> ||
                  enumerable is byte[])
            ) ? enumerable : null;
    }

    internal static IDbCommand SetParameters(this IDbCommand dbCmd, Dictionary<string, object> dict, bool excludeDefaults, ref string sql)
    {
        if (dict == null)
            return dbCmd;

        dbCmd.Parameters.Clear();
        var dialectProvider = dbCmd.GetDialectProvider();

        var paramIndex = 0;
        var sqlCopy = sql; //C# doesn't allow changing ref params in lambda's

        foreach (var kvp in dict)
        {
            var value = kvp.Value;
            var propName = kvp.Key;
            if (excludeDefaults && value == null) continue;
                
            var inValues = sql != null ? GetMultiValues(value) : null;
            if (inValues != null)
            {
                var propType = value?.GetType() ?? typeof(object);
                var sb = StringBuilderCache.Allocate();
                foreach (var item in inValues)
                {
                    var p = dbCmd.CreateParameter();
                    p.ParameterName = "v" + paramIndex++;

                    if (sb.Length > 0)
                        sb.Append(',');
                    sb.Append(dialectProvider.ParamString + p.ParameterName);

                    p.Direction = ParameterDirection.Input;
                    dialectProvider.InitDbParam(p, item.GetType());

                    dialectProvider.SetParamValue(p, item, item.GetType());

                    dbCmd.Parameters.Add(p);
                }

                var sqlIn = StringBuilderCache.ReturnAndFree(sb);
                if (string.IsNullOrEmpty(sqlIn))
                    sqlIn = "NULL";
                sqlCopy = sqlCopy?.Replace(dialectProvider.ParamString + propName, sqlIn);
                if (dialectProvider.ParamString != "@")
                    sqlCopy = sqlCopy?.Replace("@" + propName, sqlIn);
            }
            else
            {
                var p = dbCmd.CreateParameter();
                p.ParameterName = propName;
    
                p.Direction = ParameterDirection.Input;
                p.Value = value ?? DBNull.Value;
                if (value != null)
                    dialectProvider.InitDbParam(p, value.GetType());
    
                dbCmd.Parameters.Add(p);
            }
        }

        sql = sqlCopy;

        return dbCmd;
    }

    internal static IDbCommand SetParameters(this IDbCommand dbCmd, Type type, object anonType, bool excludeDefaults, ref string sql)
    {
        if (anonType.AssertAnonObject() == null)
            return dbCmd;

        dbCmd.Parameters.Clear();

        var modelDef = type.GetModelDefinition();
        var dialectProvider = dbCmd.GetDialectProvider();
        var fieldMap = type.IsUserType() //Ensure T != Scalar<int>()
            ? dialectProvider.GetFieldDefinitionMap(modelDef)
            : null;

        var sqlCopy = sql; //C# doesn't allow changing ref params in lambda's
        Dictionary<string, PropertyAccessor> anonTypeProps = null;

        var paramIndex = 0;
        anonType.ToObjectDictionary().ForEachParam(modelDef, excludeDefaults, (propName, columnName, value) =>
        {
            var propType = value?.GetType() ?? ((anonTypeProps ??= TypeProperties.Get(anonType.GetType()).PropertyMap)
                .TryGetValue(propName, out var pType)
                    ? pType.PropertyInfo.PropertyType
                    : typeof(object));
            var inValues = GetMultiValues(value);
            if (inValues != null)
            {
                var sb = StringBuilderCache.Allocate();
                foreach (var item in inValues)
                {
                    var p = dbCmd.CreateParameter();
                    p.ParameterName = "v" + paramIndex++;

                    if (sb.Length > 0)
                        sb.Append(',');
                    sb.Append(dialectProvider.ParamString + p.ParameterName);

                    p.Direction = ParameterDirection.Input;
                    dialectProvider.InitDbParam(p, item.GetType());

                    dialectProvider.SetParamValue(p, item, item.GetType());

                    dbCmd.Parameters.Add(p);
                }

                var sqlIn = StringBuilderCache.ReturnAndFree(sb);
                if (string.IsNullOrEmpty(sqlIn))
                    sqlIn = "NULL";
                sqlCopy = sqlCopy?.Replace(dialectProvider.ParamString + propName, sqlIn);
                if (dialectProvider.ParamString != "@")
                    sqlCopy = sqlCopy?.Replace("@" + propName, sqlIn);
            }
            else
            {
                var p = dbCmd.CreateParameter();
                p.ParameterName = propName;
                p.Direction = ParameterDirection.Input;
                dialectProvider.InitDbParam(p, propType);

                FieldDefinition fieldDef = null;
                fieldMap?.TryGetValue(columnName, out fieldDef);

                dialectProvider.SetParamValue(p, value, propType, fieldDef);

                dbCmd.Parameters.Add(p);
            }
        });

        sql = sqlCopy;
        return dbCmd;
    }

    internal static void SetParamValue(this IOrmLiteDialectProvider dialectProvider, IDbDataParameter p, object value, Type propType, FieldDefinition fieldDef=null)
    {
        if (fieldDef != null)
        {
            value = dialectProvider.GetFieldValue(fieldDef, value);
            var valueType = value?.GetType();
            if (valueType != null && valueType != propType)
                dialectProvider.InitDbParam(p, valueType);
        }
        else
        {
            value = dialectProvider.GetFieldValue(propType, value);
            var valueType = value?.GetType();
            if (valueType != null && valueType != propType)
                dialectProvider.InitDbParam(p, valueType);
        }

        p.Value = value == null
            ? DBNull.Value
            : p.DbType == DbType.String
                ? value.ToString()
                : value;
    }

    internal delegate void ParamIterDelegate(string propName, string columnName, object value);

    internal static void ForEachParam(this Dictionary<string,object> values, ModelDefinition modelDef, bool excludeDefaults, ParamIterDelegate fn)
    {
        if (values == null)
            return;

        foreach (var kvp in values)
        {
            var value = kvp.Value;

            if (excludeDefaults && (value == null || value.Equals(value.GetType().GetDefaultValue())))
                continue;

            var targetField = modelDef?.FieldDefinitions.FirstOrDefault(f => string.Equals(f.Name, kvp.Key));
            var columnName = !string.IsNullOrEmpty(targetField?.Alias)
                ? targetField.Alias
                : kvp.Key;

            fn(kvp.Key, columnName, value);
        }
    }

    internal static List<string> AllFields<T>(this object anonType)
    {
        var ret = new List<string>();
        anonType.ToObjectDictionary().ForEachParam(typeof(T).GetModelDefinition(), excludeDefaults: false, fn: (propName, columnName, value) => ret.Add(propName));
        return ret;
    }

    internal static Dictionary<string, object> AllFieldsMap<T>(this object anonType)
    {
        var ret = new Dictionary<string, object>();
        anonType.ToObjectDictionary()
            .ForEachParam(typeof(T).GetModelDefinition(), excludeDefaults: false, fn: 
                (propName, columnName, value) => ret[propName] = value);
        return ret;
    }

    internal static Dictionary<string, object> NonDefaultsOnly(this Dictionary<string, object> fieldValues)
    {
        var map = new Dictionary<string, object>();
        foreach (var entry in fieldValues)
        {
            if (entry.Value != null)
            {
                var type = entry.Value.GetType();
                if (!type.IsValueType || !entry.Value.Equals(type.GetDefaultValue()))
                {
                    map[entry.Key] = entry.Value;
                }
            }
        }
        return map;
    }

    public static IDbCommand SetFilters<T>(this IDbCommand dbCmd, object anonType)
    {
        return dbCmd.SetFilters<T>(anonType, excludeDefaults: false);
    }

    public static void ClearFilters(this IDbCommand dbCmd)
    {
        dbCmd.Parameters.Clear();
    }

    internal static string GetFilterSql<T>(this IDbCommand dbCmd)
    {
        var dialectProvider = dbCmd.GetDialectProvider();
        var modelDef = typeof(T).GetModelDefinition();

        var sb = StringBuilderCache.Allocate();
        foreach (IDbDataParameter p in dbCmd.Parameters)
        {
            if (sb.Length > 0)
                sb.Append(" AND ");

            var fieldName = p.ParameterName;
            var fieldDef = modelDef.GetFieldDefinition(fieldName);
            if (fieldDef != null)
                fieldName = fieldDef.FieldName;

            sb.Append(fieldDef != null 
                ? dialectProvider.GetQuotedColumnName(fieldDef)
                : dialectProvider.GetQuotedColumnName(fieldName));

            p.ParameterName = dialectProvider.SanitizeFieldNameForParamName(fieldName);

            sb.Append(" = ");
            sb.Append(dialectProvider.GetParam(p.ParameterName));
        }

        return dialectProvider.ToSelectStatement(typeof(T), StringBuilderCache.ReturnAndFree(sb));
    }

//        internal static bool CanReuseParam<T>(this IDbCommand dbCmd, string paramName)
//        {
//            return (dbCmd.Parameters.Count == 1
//                    && ((IDbDataParameter)dbCmd.Parameters[0]).ParameterName == paramName
//                    && lastQueryType != typeof(T));
//        }

    internal static List<T> SelectByIds<T>(this IDbCommand dbCmd, IEnumerable idValues)
    {
        var sqlIn = dbCmd.SetIdsInSqlParams(idValues);
        return string.IsNullOrEmpty(sqlIn)
            ? new List<T>()
            : Select<T>(dbCmd, dbCmd.GetDialectProvider().GetQuotedColumnName(ModelDefinition<T>.Definition.PrimaryKey) + " IN (" + sqlIn + ")");
    }

    internal static T SingleById<T>(this IDbCommand dbCmd, object value)
    {
        if (value == null) 
            throw new ArgumentNullException(nameof(value));
        SetFilter<T>(dbCmd, ModelDefinition<T>.PrimaryKeyName, value);
        return dbCmd.ConvertTo<T>();
    }

    internal static T SingleWhere<T>(this IDbCommand dbCmd, string name, object value)
    {
        SetFilter<T>(dbCmd, name, value);
        return dbCmd.ConvertTo<T>();
    }

    internal static T Single<T>(this IDbCommand dbCmd, object anonType)
    {
        dbCmd.SetFilters<T>(anonType, excludeDefaults: false);

        return dbCmd.ConvertTo<T>();
    }

    internal static T Single<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
    {
        dbCmd.SetParameters(sqlParams);

        return OrmLiteUtils.IsScalar<T>()
            ? dbCmd.Scalar<T>(sql)
            : dbCmd.ConvertTo<T>(dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql));
    }

    internal static T Single<T>(this IDbCommand dbCmd, string sql, object anonType)
    {
        dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);

        return OrmLiteUtils.IsScalar<T>()
            ? dbCmd.Scalar<T>(sql)
            : dbCmd.ConvertTo<T>(dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql));
    }

    internal static List<T> Where<T>(this IDbCommand dbCmd, string name, object value)
    {
        SetFilter<T>(dbCmd, name, value);
        return dbCmd.ConvertToList<T>();
    }

    internal static List<T> Where<T>(this IDbCommand dbCmd, object anonType)
    {
        dbCmd.SetFilters<T>(anonType);

        return dbCmd.ConvertToList<T>();
    }

    internal static List<T> Select<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
    {
        dbCmd.CommandText = dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql);
        if (sqlParams != null) dbCmd.SetParameters(sqlParams);

        return dbCmd.ConvertToList<T>();
    }

    internal static List<T> Select<T>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);
        dbCmd.CommandText = dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql);

        return dbCmd.ConvertToList<T>();
    }

    internal static List<T> Select<T>(this IDbCommand dbCmd, string sql, Dictionary<string, object> dict)
    {
        if (dict != null) SetParameters(dbCmd, dict, (bool)false, sql:ref sql);
        dbCmd.CommandText = dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql);

        return dbCmd.ConvertToList<T>();
    }

    internal static List<TModel> Select<TModel>(this IDbCommand dbCmd, Type fromTableType)
    {
        return Select<TModel>(dbCmd, fromTableType, null);
    }

    internal static List<T> Select<T>(this IDbCommand dbCmd, Type fromTableType, string sql, object anonType = null)
    {
        if (anonType != null) dbCmd.SetParameters(fromTableType, anonType, excludeDefaults: false, sql: ref sql);
        dbCmd.CommandText = ToSelect<T>(dbCmd.GetDialectProvider(), fromTableType, sql);

        return dbCmd.ConvertToList<T>();
    }

    internal static string ToSelect<TModel>(IOrmLiteDialectProvider dialectProvider, Type fromTableType, string sqlFilter)
    {
        var sql = StringBuilderCache.Allocate();
        var modelDef = ModelDefinition<TModel>.Definition;
        sql.Append(
            $"SELECT {dialectProvider.GetColumnNames(modelDef)} " +
            $"FROM {dialectProvider.GetQuotedTableName(fromTableType.GetModelDefinition())}");

        if (string.IsNullOrEmpty(sqlFilter))
            return StringBuilderCache.ReturnAndFree(sql);

        sql.Append(" WHERE ");
        sql.Append(sqlFilter);
        return StringBuilderCache.ReturnAndFree(sql);
    }

    internal static List<T> SqlList<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
    {
        dbCmd.CommandText = sql;

        return dbCmd.SetParameters(sqlParams).ConvertToList<T>();
    }

    internal static List<T> SqlList<T>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);
        dbCmd.CommandText = sql;

        return dbCmd.ConvertToList<T>();
    }

    internal static List<T> SqlList<T>(this IDbCommand dbCmd, string sql, Dictionary<string, object> dict)
    {
        if (dict != null) SetParameters(dbCmd, dict, false, sql:ref sql);
        dbCmd.CommandText = sql;

        return dbCmd.ConvertToList<T>();
    }

    internal static List<T> SqlList<T>(this IDbCommand dbCmd, string sql, Action<IDbCommand> dbCmdFilter)
    {
        dbCmdFilter?.Invoke(dbCmd);
        dbCmd.CommandText = sql;

        return dbCmd.ConvertToList<T>();
    }

    internal static List<T> SqlColumn<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
    {
        dbCmd.SetParameters(sqlParams).CommandText = sql;
        return dbCmd.ConvertToList<T>();
    }

    public static List<T> SqlColumn<T>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql).CommandText = sql;
        return dbCmd.ConvertToList<T>();
    }

    internal static List<T> SqlColumn<T>(this IDbCommand dbCmd, string sql, Dictionary<string, object> dict)
    {
        if (dict != null) SetParameters(dbCmd, dict, false, sql:ref sql);
        dbCmd.CommandText = sql;

        return dbCmd.ConvertToList<T>();
    }

    internal static T SqlScalar<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
    {
        return dbCmd.SetParameters(sqlParams).Scalar<T>(sql);
    }

    internal static T SqlScalar<T>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);

        return dbCmd.Scalar<T>(sql);
    }

    internal static T SqlScalar<T>(this IDbCommand dbCmd, string sql, Dictionary<string, object> dict)
    {
        if (dict != null) SetParameters(dbCmd, dict, false, sql:ref sql);

        return dbCmd.Scalar<T>(sql);
    }

    internal static List<T> SelectNonDefaults<T>(this IDbCommand dbCmd, object filter)
    {
        dbCmd.SetFilters<T>(filter, excludeDefaults: true);

        return dbCmd.ConvertToList<T>();
    }

    internal static List<T> SelectNonDefaults<T>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: true, sql: ref sql);

        return dbCmd.ConvertToList<T>(dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql));
    }

    internal static IEnumerable<T> SelectLazy<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
    {
        foreach (var p in dbCmd.SetParameters(sqlParams).SelectLazy<T>(sql)) yield return p;
    }

    internal static IEnumerable<T> SelectLazy<T>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);
        var dialectProvider = dbCmd.GetDialectProvider();
        dbCmd.CommandText = dialectProvider.ToSelectStatement(typeof(T), sql);

        var resultsFilter = OrmLiteConfig.ResultsFilter;
        if (resultsFilter != null)
        {
            foreach (var item in resultsFilter.GetList<T>(dbCmd))
            {
                yield return item;
            }
            yield break;
        }

        using (var reader = dbCmd.ExecuteReader())
        {
            var indexCache = reader.GetIndexFieldsCache(ModelDefinition<T>.Definition, dialectProvider);
            var values = new object[reader.FieldCount];
            while (reader.Read())
            {
                var row = OrmLiteUtils.CreateInstance<T>();
                row.PopulateWithSqlReader(dialectProvider, reader, indexCache, values);
                yield return row;
            }
        }
    }

    internal static IEnumerable<T> ColumnLazy<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
    {
        foreach (var p in dbCmd.SetParameters(sqlParams).ColumnLazy<T>(sql)) yield return p;
    }

    internal static IEnumerable<T> ColumnLazy<T>(this IDbCommand dbCmd, string sql, object anonType)
    {
        foreach (var p in dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql).ColumnLazy<T>(sql)) yield return p;
    }

    private static IEnumerable<T> ColumnLazy<T>(this IDbCommand dbCmd, string sql)
    {
        var dialectProvider = dbCmd.GetDialectProvider();
        dbCmd.CommandText = dialectProvider.ToSelectStatement(typeof(T), sql);

        if (OrmLiteConfig.ResultsFilter != null)
        {
            foreach (var item in OrmLiteConfig.ResultsFilter.GetColumn<T>(dbCmd))
            {
                yield return item;
            }
            yield break;
        }

        using (var reader = dbCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var value = dialectProvider.FromDbValue(reader, 0, typeof(T));
                if (value == DBNull.Value)
                    yield return default(T);
                else
                    yield return (T)value;
            }
        }
    }

    internal static IEnumerable<T> WhereLazy<T>(this IDbCommand dbCmd, object anonType)
    {
        dbCmd.SetFilters<T>(anonType);

        if (OrmLiteConfig.ResultsFilter != null)
        {
            foreach (var item in OrmLiteConfig.ResultsFilter.GetList<T>(dbCmd))
            {
                yield return item;
            }
            yield break;
        }

        var dialectProvider = dbCmd.GetDialectProvider();
        using (var reader = dbCmd.ExecuteReader())
        {
            var indexCache = reader.GetIndexFieldsCache(ModelDefinition<T>.Definition, dialectProvider);
            var values = new object[reader.FieldCount];
            while (reader.Read())
            {
                var row = OrmLiteUtils.CreateInstance<T>();
                row.PopulateWithSqlReader(dialectProvider, reader, indexCache, values);
                yield return row;
            }
        }
    }

    internal static IEnumerable<T> SelectLazy<T>(this IDbCommand dbCmd)
    {
        return SelectLazy<T>(dbCmd, null);
    }

    internal static T Scalar<T>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);

        return dbCmd.Scalar<T>(sql);
    }

    internal static T Scalar<T>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider)
    {
        while (reader.Read())
        {
            return ToScalar<T>(dialectProvider, reader);
        }

        return default(T);
    }

    internal static T ToScalar<T>(IOrmLiteDialectProvider dialectProvider, IDataReader reader, int columnIndex = 0)
    {
        var nullableType = Nullable.GetUnderlyingType(typeof(T));
        if (nullableType != null)
        {
            object oValue = reader.GetValue(columnIndex);
            if (oValue == DBNull.Value)
                return default(T);
        }

        var underlyingType = nullableType ?? typeof(T);
        if (underlyingType == typeof(object))
            return (T)reader.GetValue(0);

        var converter = dialectProvider.GetConverterBestMatch(underlyingType);
        if (converter != null)
        {
            object oValue = converter.GetValue(reader, columnIndex, null);
            if (oValue == null)
                return default(T);

            var convertedValue = converter.FromDbValue(underlyingType, oValue);
            return convertedValue == null ? default(T) : (T)convertedValue;
        }

        return (T)reader.GetValue(0);
    }

    internal static long LastInsertId(this IDbCommand dbCmd)
    {
        if (OrmLiteConfig.ResultsFilter != null)
            return OrmLiteConfig.ResultsFilter.GetLastInsertId(dbCmd);

        return dbCmd.GetDialectProvider().GetLastInsertId(dbCmd);
    }

    internal static List<T> Column<T>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);

        return dbCmd.Column<T>(dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql));
    }

    internal static List<T> Column<T>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider)
    {
        var columValues = new List<T>();

        while (reader.Read())
        {
            var value = dialectProvider.FromDbValue(reader, 0, typeof(T));
            if (value == DBNull.Value || value == null)
                value = default(T);

            columValues.Add((T)value);
        }
        return columValues;
    }

    internal static HashSet<T> ColumnDistinct<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
    {
        return dbCmd.SetParameters(sqlParams).ColumnDistinct<T>(sql);
    }

    internal static HashSet<T> ColumnDistinct<T>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        return dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql).ColumnDistinct<T>(sql);
    }

    internal static HashSet<T> ColumnDistinct<T>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider)
    {
        var columValues = new HashSet<T>();
        while (reader.Read())
        {
            var value = dialectProvider.FromDbValue(reader, 0, typeof(T));
            if (value == DBNull.Value)
                value = default(T);

            columValues.Add((T)value);
        }
        return columValues;
    }

    public static Dictionary<K, List<V>> Lookup<K, V>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        return dbCmd.SetParameters(anonType.ToObjectDictionary(), false, sql:ref sql).Lookup<K, V>(sql);
    }

    internal static Dictionary<K, List<V>> Lookup<K, V>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider)
    {
        var lookup = new Dictionary<K, List<V>>();

        while (reader.Read())
        {
            var key = (K)dialectProvider.FromDbValue(reader, 0, typeof(K));
            var value = (V)dialectProvider.FromDbValue(reader, 1, typeof(V));

            if (!lookup.TryGetValue(key, out var values))
            {
                values = new List<V>();
                lookup[key] = values;
            }
            values.Add(value);
        }

        return lookup;
    }

    internal static Dictionary<K, V> Dictionary<K, V>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        if (anonType != null) SetParameters(dbCmd, anonType.ToObjectDictionary(), excludeDefaults: false, sql:ref sql);

        return dbCmd.Dictionary<K, V>(sql);
    }

    internal static Dictionary<K, V> Dictionary<K, V>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider)
    {
        var map = new Dictionary<K, V>();

        while (reader.Read())
        {
            var key = (K)dialectProvider.FromDbValue(reader, 0, typeof(K));
            var value = (V)dialectProvider.FromDbValue(reader, 1, typeof(V));

            map.Add(key, value);
        }

        return map;
    }

    internal static List<KeyValuePair<K, V>> KeyValuePairs<K, V>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        if (anonType != null) SetParameters(dbCmd, anonType.ToObjectDictionary(), excludeDefaults: false, sql:ref sql);

        return dbCmd.KeyValuePairs<K, V>(sql);
    }

    internal static List<KeyValuePair<K, V>> KeyValuePairs<K, V>(this IDataReader reader, IOrmLiteDialectProvider dialectProvider)
    {
        var to = new List<KeyValuePair<K, V>>();

        while (reader.Read())
        {
            var key = (K)dialectProvider.FromDbValue(reader, 0, typeof(K));
            var value = (V)dialectProvider.FromDbValue(reader, 1, typeof(V));

            to.Add(new KeyValuePair<K,V>(key, value));
        }

        return to;
    }

    internal static bool Exists<T>(this IDbCommand dbCmd, object anonType)
    {
        string sql = null;
        if (anonType != null) SetParameters(dbCmd, anonType.ToObjectDictionary(), excludeDefaults: true, sql:ref sql);

        sql = GetFilterSql<T>(dbCmd);

        var result = dbCmd.Scalar(sql);
        return result != null;
    }

    internal static bool Exists<T>(this IDbCommand dbCmd, string sql, object anonType = null)
    {
        if (anonType != null) SetParameters(dbCmd, anonType.ToObjectDictionary(), (bool)false, sql:ref sql);

        var result = dbCmd.Scalar(dbCmd.GetDialectProvider().ToSelectStatement(typeof(T), sql));
        return result != null;
    }

    internal static bool ExistsById<T>(this IDbCommand dbCmd, object value)
    {
        if (value == null) 
            throw new ArgumentNullException(nameof(value));
        
        var modelDef = ModelDefinition<T>.Definition;
        var pkName = ModelDefinition<T>.PrimaryKeyName;
        var dialect = dbCmd.GetDialectProvider();
        var result = dbCmd.SqlScalar<int>(
            "SELECT 1 FROM " + dialect.GetQuotedTableName(modelDef) + 
            " WHERE " + dialect.GetQuotedColumnName(modelDef.PrimaryKey) + " = " + dialect.GetParam(pkName), 
            new Dictionary<string,object> {
                [pkName] = value
            });
        return result == 1;
    }

    // procedures ...		
    internal static List<TOutputModel> SqlProcedure<TOutputModel>(this IDbCommand dbCommand, object fromObjWithProperties)
    {
        return SqlProcedureFmt<TOutputModel>(dbCommand, fromObjWithProperties, String.Empty);
    }

    internal static List<TOutputModel> SqlProcedureFmt<TOutputModel>(this IDbCommand dbCmd,
        object fromObjWithProperties,
        string sqlFilter,
        params object[] filterParams)
    {
        var modelType = typeof(TOutputModel);

        string sql = dbCmd.GetDialectProvider().ToSelectFromProcedureStatement(
            fromObjWithProperties, modelType, sqlFilter, filterParams);

        return dbCmd.ConvertToList<TOutputModel>(sql);
    }

    public static long LongScalar(this IDbCommand dbCmd)
    {
        var result = dbCmd.ExecuteScalar();
        return ToLong(result);
    }

    internal static long ToLong(int result) => result;
        
    internal static long ToLong(object result) => result switch
    {
        DBNull => 0,
        int i => i,
        decimal d => Convert.ToInt64(d),
        ulong u => (long)u,
        _ => Convert.ToInt64(result)
    };

    internal static T LoadSingleById<T>(this IDbCommand dbCmd, object value, string[] include = null)
    {
        var row = dbCmd.SingleById<T>(value);
        if (row == null)
            return default;

        dbCmd.LoadReferences(row, include);

        return row;
    }

    public static void LoadReferences<T>(this IDbCommand dbCmd, T instance, IEnumerable<string> include = null)
    {
        var loadRef = new LoadReferencesSync<T>(dbCmd, instance);
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
                loadRef.SetRefFieldList(fieldDef, listInterface.GetGenericArguments()[0]);
            }
            else if (fieldDef.FieldReference != null)
            {
                loadRef.SetFieldReference(fieldDef, fieldDef.FieldReference);
            }
            else
            {
                loadRef.SetRefField(fieldDef, fieldDef.FieldType);
            }
        }
    }

    internal static List<Into> LoadListWithReferences<Into, From>(this IDbCommand dbCmd, SqlExpression<From> expr = null, IEnumerable<string> include = null)
    {
        var loadList = new LoadListSync<Into, From>(dbCmd, expr);
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
                loadList.SetRefFieldList(fieldDef, listInterface.GetGenericArguments()[0]);
            }
            else if (fieldDef.FieldReference != null)
            {
                loadList.SetFieldReference(fieldDef, fieldDef.FieldReference);
            }
            else
            {
                loadList.SetRefField(fieldDef, fieldDef.FieldType);
            }
        }

        return loadList.ParentResults;
    }

    public static FieldDefinition GetRefFieldDef(this ModelDefinition modelDef, ModelDefinition refModelDef, Type refType)
    {
        var refField = GetRefFieldDefIfExists(modelDef, refModelDef);
        if (refField == null)
            throw new ArgumentException($"Cant find '{modelDef.ModelName + "Id"}' Property on Type '{refType.Name}'");
        return refField;
    }

    public static FieldDefinition GetExplicitRefFieldDefIfExists(this ModelDefinition modelDef, ModelDefinition refModelDef)
    {
        var refField = refModelDef.FieldDefinitions.FirstOrDefault(x => x.ForeignKey != null && x.ForeignKey.ReferenceType == modelDef.ModelType && modelDef.IsRefField(x))
                       ?? refModelDef.FieldDefinitions.FirstOrDefault(x => x.ForeignKey != null && x.ForeignKey.ReferenceType == modelDef.ModelType);

        return refField;
    }

    public static FieldDefinition GetRefFieldDefIfExists(this ModelDefinition modelDef, ModelDefinition refModelDef)
    {
        var refField = GetExplicitRefFieldDefIfExists(modelDef, refModelDef)
                       ?? refModelDef.FieldDefinitions.FirstOrDefault(modelDef.IsRefField);

        return refField;
    }

    public static FieldDefinition GetSelfRefFieldDefIfExists(this ModelDefinition modelDef, ModelDefinition refModelDef, FieldDefinition fieldDef)
    {
        if (fieldDef?.ReferenceSelfId != null)
            return modelDef.FieldDefinitions.FirstOrDefault(x => x.Name == fieldDef.ReferenceSelfId);
            
        var refField = (fieldDef != null
                           ? modelDef.FieldDefinitions.FirstOrDefault(x =>
                               x.ForeignKey != null && x.ForeignKey.ReferenceType == refModelDef.ModelType &&
                               fieldDef.IsSelfRefField(x))
                           : null)
                       ?? modelDef.FieldDefinitions.FirstOrDefault(x => x.ForeignKey != null && x.ForeignKey.ReferenceType == refModelDef.ModelType)
                       ?? modelDef.FieldDefinitions.FirstOrDefault(refModelDef.IsRefField);
        return refField;
    }

    public static IDbDataParameter AddParam(this IDbCommand dbCmd,
        string name,
        object value = null,
        ParameterDirection direction = ParameterDirection.Input,
        DbType? dbType = null,
        byte? precision = null,
        byte? scale = null,
        int? size=null, 
        Action<IDbDataParameter> paramFilter = null)
    {
        var p = dbCmd.CreateParam(name, value, direction, dbType, precision, scale, size);
        paramFilter?.Invoke(p);
        dbCmd.Parameters.Add(p);
        return p;
    }

    public static IDbDataParameter CreateParam(this IDbCommand dbCmd,
        string name,
        object value = null,
        ParameterDirection direction = ParameterDirection.Input,
        DbType? dbType = null,
        byte? precision=null,
        byte? scale=null,
        int? size=null)
    {
        var p = dbCmd.CreateParameter();
        var dialectProvider = dbCmd.GetDialectProvider();
        p.ParameterName = dialectProvider.GetParam(name);
        p.Direction = direction;

        if (p.DbType == DbType.String)
        {
            p.Size = dialectProvider.GetStringConverter().StringLength;
            if (value is string strValue && strValue.Length > p.Size)
                p.Size = strValue.Length;
        }

        if (value != null)
        {
            p.Value = value;
            dialectProvider.InitDbParam(p, value.GetType());
        }
        else
        {
            p.Value = DBNull.Value;
        }

        if (dbType != null)
            p.DbType = dbType.Value;

        if (precision != null)
            p.Precision = precision.Value;

        if (scale != null)
            p.Scale = scale.Value;

        if (size != null)
            p.Size = size.Value;

        return p;
    }

    internal static IDbCommand SqlProc(this IDbCommand dbCmd, string name, object inParams = null, bool excludeDefaults = false)
    {
        dbCmd.CommandType = CommandType.StoredProcedure;
        dbCmd.CommandText = name;

        string sql = null;
        dbCmd.SetParameters(inParams.ToObjectDictionary(), excludeDefaults, sql:ref sql);

        return dbCmd;
    }
}