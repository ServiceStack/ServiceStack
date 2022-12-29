using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ServiceStack.Data;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    internal static class WriteExpressionCommandExtensions
    {
        public static int UpdateOnlyFields<T>(this IDbCommand dbCmd,
            T model,
            SqlExpression<T> onlyFields,
            Action<IDbCommand> commandFilter = null)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            
            UpdateOnlySql(dbCmd, model, onlyFields);
            commandFilter?.Invoke(dbCmd);
            return dbCmd.ExecNonQuery();
        }

        internal static void UpdateOnlySql<T>(this IDbCommand dbCmd, T model, SqlExpression<T> onlyFields)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            
            OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, model);

            var fieldsToUpdate = onlyFields.UpdateFields.Count == 0
                ? onlyFields.GetAllFields()
                : onlyFields.UpdateFields;

            onlyFields.CopyParamsTo(dbCmd);

            dbCmd.GetDialectProvider().PrepareUpdateRowStatement(dbCmd, model, fieldsToUpdate);

            if (!onlyFields.WhereExpression.IsNullOrEmpty())
                dbCmd.CommandText += " " + onlyFields.WhereExpression;
        }

        internal static int UpdateOnlyFields<T>(this IDbCommand dbCmd, T obj,
            Expression<Func<T, object>> onlyFields = null,
            Expression<Func<T, bool>> where = null,
            Action<IDbCommand> commandFilter = null)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            
            if (onlyFields == null)
                throw new ArgumentNullException(nameof(onlyFields));

            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            q.Update(onlyFields);
            q.Where(where);
            return dbCmd.UpdateOnlyFields(obj, q, commandFilter);
        }

        internal static int UpdateOnlyFields<T>(this IDbCommand dbCmd, T obj,
            string[] onlyFields = null,
            Expression<Func<T, bool>> where = null,
            Action<IDbCommand> commandFilter = null)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            
            if (onlyFields == null)
                throw new ArgumentNullException(nameof(onlyFields));

            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            q.Update(onlyFields);
            q.Where(where);
            return dbCmd.UpdateOnlyFields(obj, q, commandFilter);
        }

        internal static int UpdateOnly<T>(this IDbCommand dbCmd,
            Expression<Func<T>> updateFields,
            SqlExpression<T> q,
            Action<IDbCommand> commandFilter = null)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            
            var cmd = dbCmd.InitUpdateOnly(updateFields, q);
            commandFilter?.Invoke(cmd);
            return cmd.ExecNonQuery();
        }

        internal static IDbCommand InitUpdateOnly<T>(this IDbCommand dbCmd, Expression<Func<T>> updateFields, SqlExpression<T> q)
        {
            if (updateFields == null)
                throw new ArgumentNullException(nameof(updateFields));

            OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, updateFields.EvalFactoryFn());

            q.CopyParamsTo(dbCmd);

            var updateFieldValues = updateFields.AssignedValues();
            dbCmd.GetDialectProvider().PrepareUpdateRowStatement<T>(dbCmd, updateFieldValues, q.WhereExpression);

            return dbCmd;
        }

        internal static int UpdateOnly<T>(this IDbCommand dbCmd,
            Expression<Func<T>> updateFields,
            string whereExpression,
            IEnumerable<IDbDataParameter> dbParams,
            Action<IDbCommand> commandFilter = null)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            
            var cmd = dbCmd.InitUpdateOnly(updateFields, whereExpression, dbParams);
            commandFilter?.Invoke(cmd);
            return cmd.ExecNonQuery();
        }

        internal static IDbCommand InitUpdateOnly<T>(this IDbCommand dbCmd, Expression<Func<T>> updateFields, string whereExpression, IEnumerable<IDbDataParameter> sqlParams)
        {
            if (updateFields == null)
                throw new ArgumentNullException(nameof(updateFields));

            OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, updateFields.EvalFactoryFn());

            dbCmd.SetParameters(sqlParams);

            var updateFieldValues = updateFields.AssignedValues();
            dbCmd.GetDialectProvider().PrepareUpdateRowStatement<T>(dbCmd, updateFieldValues, whereExpression);

            return dbCmd;
        }
        
        public static int UpdateAdd<T>(this IDbCommand dbCmd,
            Expression<Func<T>> updateFields,
            SqlExpression<T> q,
            Action<IDbCommand> commandFilter)
        {
            var cmd = dbCmd.InitUpdateAdd(updateFields, q);
            commandFilter?.Invoke(cmd);
            return cmd.ExecNonQuery();
        }

        internal static IDbCommand InitUpdateAdd<T>(this IDbCommand dbCmd, Expression<Func<T>> updateFields, SqlExpression<T> q)
        {
            if (updateFields == null)
                throw new ArgumentNullException(nameof(updateFields));

            OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, updateFields.EvalFactoryFn());

            q.CopyParamsTo(dbCmd);

            var updateFieldValues = updateFields.AssignedValues();
            dbCmd.GetDialectProvider().PrepareUpdateRowAddStatement<T>(dbCmd, updateFieldValues, q.WhereExpression);

            return dbCmd;
        }

        public static int UpdateOnly<T>(this IDbCommand dbCmd,
            Dictionary<string, object> updateFields,
            Expression<Func<T, bool>> where,
            Action<IDbCommand> commandFilter = null)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            
            if (updateFields == null)
                throw new ArgumentNullException(nameof(updateFields));

            OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, updateFields.ToFilterType<T>());

            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            q.Where(where);
            q.PrepareUpdateStatement(dbCmd, updateFields);
            return dbCmd.UpdateAndVerify<T>(commandFilter, updateFields.ContainsKey(ModelDefinition.RowVersionName));
        }

        internal static string GetUpdateOnlyWhereExpression<T>(this IOrmLiteDialectProvider dialectProvider, 
            Dictionary<string, object> updateFields, out object[] args)
        {
            var modelDef = typeof(T).GetModelDefinition();
            var pkField = modelDef.PrimaryKey;
            if (pkField == null)
                throw new NotSupportedException($"'{typeof(T).Name}' does not have a primary key");

            var idValue = updateFields.TryRemove(pkField.Name, out var nameValue)
                ? nameValue
                : pkField.Alias != null && updateFields.TryRemove(pkField.Alias, out var aliasValue)
                    ? aliasValue
                    : null;

            if (idValue == null)
            {
                var caseInsensitiveMap =
                    new Dictionary<string, object>(updateFields, StringComparer.InvariantCultureIgnoreCase);
                idValue = caseInsensitiveMap.TryRemove(pkField.Name, out nameValue)
                    ? nameValue
                    : pkField.Alias != null && caseInsensitiveMap.TryRemove(pkField.Alias, out aliasValue)
                        ? aliasValue
                        : new NotSupportedException(
                            $"UpdateOnly<{typeof(T).Name}> requires a '{pkField.Name}' Primary Key Value");
            }

            if (modelDef.RowVersion == null || !updateFields.TryGetValue(ModelDefinition.RowVersionName, out var rowVersion))
            {
                args = new[] { idValue };
                return "(" + dialectProvider.GetQuotedColumnName(pkField.FieldName) + " = {0})";
            }

            args = new[] { idValue, rowVersion };
            return "(" + dialectProvider.GetQuotedColumnName(pkField.FieldName) + " = {0} AND " + dialectProvider.GetRowVersionColumn(modelDef.RowVersion) + " = {1})";
        }

        public static int UpdateOnly<T>(this IDbCommand dbCmd,
            Dictionary<string, object> updateFields,
            Action<IDbCommand> commandFilter = null)
        {
            return dbCmd.UpdateOnlyReferences<T>(updateFields, dbFields => {
                var whereExpr = dbCmd.GetDialectProvider().GetUpdateOnlyWhereExpression<T>(dbFields, out var exprArgs);
                dbCmd.PrepareUpdateOnly<T>(dbFields, whereExpr, exprArgs);
                return dbCmd.UpdateAndVerify<T>(commandFilter, dbFields.ContainsKey(ModelDefinition.RowVersionName));
            });
        }

        public static int UpdateOnly<T>(this IDbCommand dbCmd,
            Dictionary<string, object> updateFields,
            string whereExpression,
            object[] whereParams,
            Action<IDbCommand> commandFilter = null)
        {
            return dbCmd.UpdateOnlyReferences<T>(updateFields, dbFields => {
                dbCmd.PrepareUpdateOnly<T>(dbFields, whereExpression, whereParams);
                return dbCmd.UpdateAndVerify<T>(commandFilter, dbFields.ContainsKey(ModelDefinition.RowVersionName));
            });
        }

        public static int UpdateOnlyReferences<T>(this IDbCommand dbCmd,
            Dictionary<string, object> updateFields, Func<Dictionary<string, object>, int> fn)
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

            var ret = fn(dbFields);

            if (hasReferences)
            {
                var instance = updateFields.FromObjectDictionary<T>();
                dbCmd.SaveAllReferences(instance);
            }
            return ret;
        }

        
        internal static void PrepareUpdateOnly<T>(this IDbCommand dbCmd, Dictionary<string, object> updateFields, string whereExpression, object[] whereParams)
        {
            if (updateFields == null)
                throw new ArgumentNullException(nameof(updateFields));

            OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, updateFields.ToFilterType<T>());

            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            q.Where(whereExpression, whereParams);
            q.PrepareUpdateStatement(dbCmd, updateFields);
        }

        public static int UpdateNonDefaults<T>(this IDbCommand dbCmd, T item, Expression<Func<T, bool>> where)
        {
            OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, item);

            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            q.Where(@where);
            q.PrepareUpdateStatement(dbCmd, item, excludeDefaults: true);
            return dbCmd.ExecNonQuery();
        }

        public static int Update<T>(this IDbCommand dbCmd, T item, Expression<Func<T, bool>> expression, Action<IDbCommand> commandFilter = null)
        {
            OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, item);

            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            q.Where(expression);
            q.PrepareUpdateStatement(dbCmd, item);
            commandFilter?.Invoke(dbCmd);
            return dbCmd.ExecNonQuery();
        }

        public static int Update<T>(this IDbCommand dbCmd, object updateOnly, Expression<Func<T, bool>> where = null, Action<IDbCommand> commandFilter = null)
        {
            OrmLiteUtils.AssertNotAnonType<T>();
            
            OrmLiteConfig.UpdateFilter?.Invoke(dbCmd, updateOnly.ToFilterType<T>());

            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            var whereSql = q.Where(where).WhereExpression;
            q.CopyParamsTo(dbCmd);
            var hadRowVersion = dbCmd.PrepareUpdateAnonSql<T>(dbCmd.GetDialectProvider(), updateOnly, whereSql);

            return dbCmd.UpdateAndVerify<T>(commandFilter, hadRowVersion);
        }

        internal static bool PrepareUpdateAnonSql<T>(this IDbCommand dbCmd, IOrmLiteDialectProvider dialectProvider, object updateOnly, string whereSql)
        {
            var sql = StringBuilderCache.Allocate();
            var modelDef = typeof(T).GetModelDefinition();
            var fields = modelDef.FieldDefinitionsArray;

            var fieldDefs = new List<FieldDefinition>();
            if (updateOnly is IDictionary d)
            {
                foreach (DictionaryEntry entry in d)
                {
                    var fieldDef = modelDef.GetFieldDefinition((string)entry.Key);
                    if (fieldDef == null || fieldDef.ShouldSkipUpdate()) 
                        continue;
                    fieldDefs.Add(fieldDef);
                }
            }
            else
            {
                foreach (var setField in updateOnly.GetType().GetPublicProperties())
                {
                    var fieldDef = fields.FirstOrDefault(x =>
                        string.Equals(x.Name, setField.Name, StringComparison.OrdinalIgnoreCase));
                    if (fieldDef == null || fieldDef.ShouldSkipUpdate())
                        continue;
                    fieldDefs.Add(fieldDef);
                }
            }
            
            var hadRowVersion = false;
            foreach (var fieldDef in fieldDefs)
            {
                var value = fieldDef.GetValue(updateOnly);
                if (fieldDef.IsPrimaryKey || fieldDef.AutoIncrement || fieldDef.IsRowVersion)
                {
                    if (fieldDef.IsRowVersion)
                        hadRowVersion = true;

                    whereSql += string.IsNullOrEmpty(whereSql) ? "WHERE " : " AND ";
                    whereSql += $"{dialectProvider.GetQuotedColumnName(fieldDef.FieldName)} = {dialectProvider.AddQueryParam(dbCmd, value, fieldDef).ParameterName}";
                    continue;
                }

                if (sql.Length > 0)
                    sql.Append(", ");

                sql
                    .Append(dialectProvider.GetQuotedColumnName(fieldDef.FieldName))
                    .Append("=")
                    .Append(dialectProvider.GetUpdateParam(dbCmd, value, fieldDef));
            }

            dbCmd.CommandText = $"UPDATE {dialectProvider.GetQuotedTableName(modelDef)} " +
                                $"SET {StringBuilderCache.ReturnAndFree(sql)} {whereSql}";

            return hadRowVersion;
        }

        public static long InsertOnly<T>(this IDbCommand dbCmd, T obj, string[] onlyFields, bool selectIdentity)
        {
            OrmLiteConfig.InsertFilter?.Invoke(dbCmd, obj);

            var dialectProvider = dbCmd.GetDialectProvider();
            var sql = dialectProvider.ToInsertRowStatement(dbCmd, obj, onlyFields);

            dialectProvider.SetParameterValues<T>(dbCmd, obj);

            if (selectIdentity)
                return dbCmd.ExecLongScalar(sql + dialectProvider.GetLastInsertIdSqlSuffix<T>());

            return dbCmd.ExecuteSql(sql);
        }

        public static long InsertOnly<T>(this IDbCommand dbCmd, Expression<Func<T>> insertFields, bool selectIdentity)
        {
            dbCmd.InitInsertOnly(insertFields);

            if (selectIdentity)
                return dbCmd.ExecLongScalar(dbCmd.CommandText + dbCmd.GetDialectProvider().GetLastInsertIdSqlSuffix<T>());

            return dbCmd.ExecuteNonQuery();
        }

        internal static IDbCommand InitInsertOnly<T>(this IDbCommand dbCmd, Expression<Func<T>> insertFields)
        {
            if (insertFields == null)
                throw new ArgumentNullException(nameof(insertFields));

            OrmLiteConfig.InsertFilter?.Invoke(dbCmd, insertFields.EvalFactoryFn());

            var fieldValuesMap = insertFields.AssignedValues();
            dbCmd.GetDialectProvider().PrepareInsertRowStatement<T>(dbCmd, fieldValuesMap);
            return dbCmd;
        }

        public static int Delete<T>(this IDbCommand dbCmd, Expression<Func<T, bool>> where, Action<IDbCommand> commandFilter = null)
        {
            var ev = dbCmd.GetDialectProvider().SqlExpression<T>();
            ev.Where(where);
            return dbCmd.Delete(ev, commandFilter);
        }

        public static int Delete<T>(this IDbCommand dbCmd, SqlExpression<T> where, Action<IDbCommand> commandFilter = null)
        {
            var sql = where.ToDeleteRowStatement();
            return dbCmd.ExecuteSql(sql, where.Params, commandFilter);
        }

        public static int DeleteWhere<T>(this IDbCommand dbCmd, string whereFilter, object[] whereParams)
        {
            var q = dbCmd.GetDialectProvider().SqlExpression<T>();
            q.Where(whereFilter, whereParams);
            var sql = q.ToDeleteRowStatement();
            return dbCmd.ExecuteSql(sql, q.Params);
        }
    }
}

