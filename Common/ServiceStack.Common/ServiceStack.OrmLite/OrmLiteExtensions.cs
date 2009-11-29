using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.Common.Utils;
using ServiceStack.DataAccess;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite
{
	public static class OrmLiteExtensions
	{
		public const string IdField = "Id";

		private static IOrmLiteDialectProvider dialectProvider;
		public static IOrmLiteDialectProvider DialectProvider
		{
			get
			{
				if (dialectProvider == null)
				{
					throw new ArgumentNullException("DialectProvider",
						"You must set the singleton 'OrmLiteExtensions.DialectProvider' to use the OrmLiteExtensions");
				}
				return dialectProvider;
			}
			set
			{
				dialectProvider = value;
			}
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(OrmLiteExtensions));

		private static readonly object ReadLock = new object();

		private static readonly Dictionary<Type, List<FieldDefinition>> TypeFieldDefinitionsMap
			= new Dictionary<Type, List<FieldDefinition>>();

		public class FieldDefinition
		{
			public string Name { get; set; }

			public Type FieldType { get; set; }

			public PropertyInfo PropertyInfo { get; set; }

			public bool IsPrimaryKey { get; set; }

			public bool AutoIncrement { get; set; }

			public bool IsNullable { get; set; }

			public bool IsUnique { get; set; }

			public Func<object, Type, object> ConvertValueFn { get; set; }

			public Func<object, Type, string> QuoteValueFn { get; set; }

			//TODO: use cached delegates, keep reflection around as it works everywhere
			public void SetValue(object obj, object value)
			{
				var convertedValue = ConvertValueFn(value, FieldType);
				this.PropertyInfo.GetSetMethod().Invoke(obj, new[] { convertedValue });
			}

			public string GetQuotedValue(object obj)
			{
				var value = this.PropertyInfo.GetGetMethod().Invoke(obj, new object[] { });
				return QuoteValueFn(value, FieldType);
			}
		}

		private static bool IsNullableType(Type theType)
		{
			return (theType.IsGenericType
					&& theType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)));
		}

		private static List<FieldDefinition> GetFieldDefinitions(Type type)
		{
			lock (ReadLock)
			{
				List<FieldDefinition> fieldDefinitions;
				if (!TypeFieldDefinitionsMap.TryGetValue(type, out fieldDefinitions))
				{
					fieldDefinitions = new List<FieldDefinition>();

					var objProperties = type.GetProperties(
						BindingFlags.Public | BindingFlags.Instance).ToList();

					var hasIdField = CheckForIdField(objProperties);

					var i = 0;
					foreach (var propertyInfo in objProperties)
					{
						var isFirst = i++ == 0;

						var isPrimaryKey = propertyInfo.Name == IdField
										   || (!hasIdField && isFirst);

						var isNullableType = IsNullableType(propertyInfo.PropertyType);

						var isNullable = !propertyInfo.PropertyType.IsValueType
							|| isNullableType;

						var propertyType = isNullableType
							? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
							: propertyInfo.PropertyType;

						var autoIncrement = isPrimaryKey && propertyType == typeof(int);

						var uniqueAttrs = propertyInfo.GetCustomAttributes(typeof(IndexAttribute), true);
						var isUnique = uniqueAttrs.Count() > 0 && ((IndexAttribute)uniqueAttrs[0]).Unique;

						var fieldDefinition = new FieldDefinition {
							Name = propertyInfo.Name,
							FieldType = propertyType,
							PropertyInfo = propertyInfo,
							IsNullable = isNullable,
							IsPrimaryKey = isPrimaryKey,
							AutoIncrement = autoIncrement,
							IsUnique = isUnique,
							ConvertValueFn = DialectProvider.ConvertDbValue,
							QuoteValueFn = DialectProvider.GetQuotedValue,
						};

						fieldDefinitions.Add(fieldDefinition);
					}

					TypeFieldDefinitionsMap[type] = fieldDefinitions;
				}

				return fieldDefinitions;
			}
		}

		private static Func<int, object> GetValueFn<T>(IDataRecord reader)
		{
			var type = typeof(T);

			if (type == typeof(string))
				return reader.GetString;

			if (type == typeof(short))
				return i => reader.GetInt16(i);

			if (type == typeof(int))
				return i => reader.GetInt32(i);

			if (type == typeof(long))
				return i => reader.GetInt64(i);

			if (type == typeof(bool))
				return i => reader.GetBoolean(i);

			if (type == typeof(DateTime))
				return i => reader.GetDateTime(i);

			if (type == typeof(Guid))
				return i => reader.GetGuid(i);

			if (type == typeof(float))
				return i => reader.GetFloat(i);

			if (type == typeof(double))
				return i => reader.GetDouble(i);

			if (type == typeof(decimal))
				return i => reader.GetDecimal(i);

			return reader.GetValue;
		}

		/// <summary>
		/// Not using Linq.Where() and manually iterating through objProperties just to avoid dependencies on System.Xml??
		/// </summary>
		/// <param name="objProperties">The obj properties.</param>
		/// <returns></returns>
		private static bool CheckForIdField(IEnumerable<PropertyInfo> objProperties)
		{
			foreach (var objProperty in objProperties)
			{
				if (objProperty.Name != IdField) continue;
				return true;
			}
			return false;
		}

		public static string ToCreateTableStatement(this Type tableType)
		{
			var sbColumns = new StringBuilder();

			var indexAttrs = tableType.GetCustomAttributes(typeof(IndexAttribute), true).ToList();

			foreach (var fieldDef in GetFieldDefinitions(tableType))
			{
				if (sbColumns.Length != 0) sbColumns.Append(", ");

				var columnDefinition = DialectProvider.GetColumnDefinition(
					fieldDef.Name,
					fieldDef.FieldType,
					fieldDef.IsPrimaryKey,
					fieldDef.AutoIncrement,
					fieldDef.IsNullable);

				sbColumns.AppendLine(columnDefinition);

				var propertyAttrs = fieldDef.PropertyInfo.GetCustomAttributes(typeof(IndexAttribute), true);
				foreach (var attr in propertyAttrs)
				{
					var indexAttr = (IndexAttribute)attr;
					indexAttrs.Add(new IndexAttribute(indexAttr.Unique, fieldDef.Name));
				}
			}

			var sql = new StringBuilder(string.Format("CREATE TABLE \"{0}\" ({1}); \n", tableType.Name, sbColumns));

			foreach (var attr in indexAttrs)
			{
				var indexAttr = (IndexAttribute)attr;

				var indexName = string.Format("{0}idx_{1}_{2}", indexAttr.Unique ? "u" : "",
					tableType.Name, string.Join("_", indexAttr.FieldNames.ToArray())).ToLower();

				var indexNames = string.Join(" ASC, ", indexAttr.FieldNames.ToArray());

				sql.AppendFormat("CREATE {0} INDEX {1} ON \"{2}\" ({3} ASC); \n",
					indexAttr.Unique ? "UNIQUE" : "", indexName, tableType.Name, indexNames);
			}

			//Log.DebugFormat(sql.ToString());

			return sql.ToString();
		}

		public static void CreateTable<T>(this IDbCommand dbCommand, bool overwrite)
			where T : new()
		{
			var tableType = typeof(T);
			if (overwrite)
			{
				try
				{
					dbCommand.CommandText = string.Format("DROP TABLE \"{0}\";", tableType.Name);
					dbCommand.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					Log.DebugFormat("Cannot drop non-existing table '{0}': {1}", tableType.Name, ex.Message);
				}
			}

			try
			{
				dbCommand.CommandText = ToCreateTableStatement(tableType);
				dbCommand.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				//ignore Sqlite table already exists error
				const string SqliteTableExistsError = "already exists";
				const string SqlServerAlreadyExistsError = "There is already an object named";
				if (ex.Message.Contains(SqliteTableExistsError)
					|| ex.Message.Contains(SqlServerAlreadyExistsError))
				{
					Log.DebugFormat("Ignoring existing table '{0}': {1}", tableType.Name, ex.Message);
					return;
				}
				throw;
			}
		}

		public static string ToSelectStatement(this Type tableType)
		{
			return ToSelectStatement(tableType, null);
		}

		public static string ToSelectStatement(this Type tableType, string filter, params object[] filterParams)
		{
			var sql = new StringBuilder();
			sql.AppendFormat("SELECT * FROM \"{0}\"", tableType.Name);
			if (!string.IsNullOrEmpty(filter))
			{
				filter = filter.SqlFormat(filterParams);
				sql.Append(" WHERE ");
				sql.Append(filter);
			}
			return sql.ToString();
		}

		public static List<T> Select<T>(this IDbCommand dbCommand)
			where T : new()
		{
			return Select<T>(dbCommand, null);
		}

		public static List<T> Select<T>(this IDbCommand dbCommand, string filter, params object[] filterParams)
			where T : new()
		{
			dbCommand.CommandText = ToSelectStatement(typeof(T), filter, filterParams);
			using (var reader = dbCommand.ExecuteReader())
			{
				return reader.ConvertToList<T>();
			}
		}

		public static IEnumerable<T> Each<T>(this IDbCommand dbCommand)
			where T : new()
		{
			return Each<T>(dbCommand, null);
		}

		public static IEnumerable<T> Each<T>(this IDbCommand dbCommand, string filter, params object[] filterParams)
			where T : new()
		{
			dbCommand.CommandText = ToSelectStatement(typeof(T), filter, filterParams);
			using (var reader = dbCommand.ExecuteReader())
			{
				while (reader.Read())
				{
					var row = new T();
					row.PopulateWithSqlReader(reader);
					yield return row;
				}
			}
		}

		public static T First<T>(this IDbCommand dbCommand, string filter, params object[] filterParams)
			where T : new()
		{
			return First<T>(dbCommand, filter.SqlFormat(filterParams));
		}

		public static T First<T>(this IDbCommand dbCommand, string filter)
			where T : new()
		{
			var result = FirstOrDefault<T>(dbCommand, filter);
			if (Equals(result, default(T)))
			{
				throw new ArgumentNullException(string.Format(
					"{0}: '{1}' does not exist", typeof(T).Name, filter));
			}
			return result;
		}

		public static T FirstOrDefault<T>(this IDbCommand dbCommand, string filter, params object[] filterParams)
			where T : new()
		{
			return FirstOrDefault<T>(dbCommand, filter.SqlFormat(filterParams));
		}

		public static T FirstOrDefault<T>(this IDbCommand dbCommand, string filter)
			where T : new()
		{
			dbCommand.CommandText = ToSelectStatement(typeof(T), filter);
			using (var dbReader = dbCommand.ExecuteReader())
			{
				return dbReader.ConvertTo<T>();
			}
		}

		public static T GetById<T>(this IDbCommand dbCommand, object idValue)
			where T : new()
		{
			return First<T>(dbCommand, "Id = {0}".SqlFormat(idValue));
		}

		public static T GetByIdOrDefault<T>(this IDbCommand dbCommand, object idValue)
			where T : new()
		{
			return FirstOrDefault<T>(dbCommand, "Id = {0}".SqlFormat(idValue));
		}

		public static List<T> GetByIds<T>(this IDbCommand dbCommand, IEnumerable idValues)
			where T : new()
		{
			var sql = GetIdsInSql(idValues);
			if (sql == null) return new List<T>();

			return Select<T>(dbCommand, string.Format("Id IN ({0})", sql));
		}

		public static T ConvertTo<T>(this IDataReader dataReader)
			where T : new()
		{
			using (dataReader)
			{
				if (dataReader.Read())
				{
					var row = new T();
					row.PopulateWithSqlReader(dataReader);
					return row;
				}
				return default(T);
			}
		}

		public static List<T> ConvertToList<T>(this IDataReader dataReader)
			where T : new()
		{
			var to = new List<T>();
			using (dataReader)
			{
				while (dataReader.Read())
				{
					var row = new T();
					row.PopulateWithSqlReader(dataReader);
					to.Add(row);
				}
			}
			return to;
		}

		public static T PopulateWithSqlReader<T>(this T objWithProperties, IDataReader dataReader)
		{
			var i = 0;
			var tableType = objWithProperties.GetType();
			foreach (var fieldDef in GetFieldDefinitions(tableType))
			{
				var value = dataReader.GetValue(i++);

				fieldDef.SetValue(objWithProperties, value);
			}
			return objWithProperties;
		}

		public static T GetScalar<T>(this IDbCommand dbCmd, string sql, params object[] sqlParams)
		{
			dbCmd.CommandText = sql.SqlFormat(sqlParams);
			using (var reader = dbCmd.ExecuteReader())
			{
				return GetScalar<T>(reader);
			}
		}

		public static T GetScalar<T>(this IDataReader reader)
		{
			while (reader.Read())
			{
				return StringConverterUtils.Parse<T>(reader.GetValue(0).ToString());
			}
			return default(T);
		}

		public static long GetLastInsertId(this IDbCommand dbCmd)
		{
			return DialectProvider.GetLastInsertId(dbCmd);
		}

		public static List<T> GetFirstColumn<T>(this IDbCommand dbCmd, string sql, params object[] sqlParams)
		{
			dbCmd.CommandText = sql.SqlFormat(sqlParams);
			using (var dbReader = dbCmd.ExecuteReader())
			{
				return GetFirstColumn<T>(dbReader);
			}
		}

		public static List<T> GetFirstColumn<T>(this IDataReader reader)
		{
			var columValues = new List<T>();
			var getValueFn = GetValueFn<T>(reader);
			while (reader.Read())
			{
				var value = getValueFn(0);
				columValues.Add((T)value);
			}
			return columValues;
		}

		public static HashSet<T> GetFirstColumnDistinct<T>(this IDbCommand dbCmd, string sql, params object[] sqlParams)
		{
			dbCmd.CommandText = sql.SqlFormat(sqlParams);
			using (var dbReader = dbCmd.ExecuteReader())
			{
				return GetFirstColumnDistinct<T>(dbReader);
			}
		}

		public static HashSet<T> GetFirstColumnDistinct<T>(this IDataReader reader)
		{
			var columValues = new HashSet<T>();
			var getValueFn = GetValueFn<T>(reader);
			while (reader.Read())
			{
				var value = getValueFn(0);
				columValues.Add((T)value);
			}
			return columValues;
		}

		public static Dictionary<K, List<V>> GetLookup<K, V>(this IDbCommand dbCmd, string sql, params object[] sqlParams)
		{
			dbCmd.CommandText = sql.SqlFormat(sqlParams);
			using (var dbReader = dbCmd.ExecuteReader())
			{
				return GetLookup<K,V>(dbReader);
			}
		}

		public static Dictionary<K, List<V>> GetLookup<K, V>(this IDataReader reader)
		{
			var lookup = new Dictionary<K, List<V>>();
			
			List<V> values;
			var getKeyFn = GetValueFn<K>(reader);
			var getValueFn = GetValueFn<V>(reader);
			while (reader.Read())
			{
				var key = (K)getKeyFn(0);
				var value = (V)getValueFn(1);

				if (!lookup.TryGetValue(key, out values))
				{
					values = new List<V>();
					lookup[key] = values;
				}
				values.Add(value);
			}

			return lookup;
		}

		public static string ToInsertRowStatement(this object objWithProperties)
		{
			var sbColumnNames = new StringBuilder();
			var sbColumnValues = new StringBuilder();

			var tableType = objWithProperties.GetType();
			foreach (var fieldDef in GetFieldDefinitions(tableType))
			{
				if (fieldDef.AutoIncrement) continue;

				if (sbColumnNames.Length > 0) sbColumnNames.Append(",");
				if (sbColumnValues.Length > 0) sbColumnValues.Append(",");

				try
				{
					sbColumnNames.Append(string.Format("\"{0}\"", fieldDef.Name));
					sbColumnValues.Append(fieldDef.GetQuotedValue(objWithProperties));
				}
				catch (Exception ex)
				{
					Log.Error("ERROR in ToInsertRowStatement(): " + ex.Message, ex);
					throw ex;
				}
			}

			var sql = string.Format("INSERT INTO \"{0}\" ({1}) VALUES ({2});",
									tableType.Name, sbColumnNames, sbColumnValues);

			//Log.DebugFormat(sql);

			return sql;
		}

		public static void Insert<T>(this IDbCommand dbCommand, T obj)
			where T : new()
		{
			dbCommand.CommandText = ToInsertRowStatement(obj);
			dbCommand.ExecuteNonQuery();
		}

		public static string ToUpdateRowStatement(this object objWithProperties)
		{
			var sqlFilter = new StringBuilder();
			var sql = new StringBuilder();

			var tableType = objWithProperties.GetType();
			foreach (var fieldDef in GetFieldDefinitions(tableType))
			{
				try
				{
					if (fieldDef.IsPrimaryKey)
					{
						if (sqlFilter.Length > 0) sqlFilter.Append(" AND ");

						sqlFilter.AppendFormat("\"{0}\" = {1}", fieldDef.Name, fieldDef.GetQuotedValue(objWithProperties));

						continue;
					}

					if (sql.Length > 0) sql.Append(",");
					sql.AppendFormat("\"{0}\" = {1}", fieldDef.Name, fieldDef.GetQuotedValue(objWithProperties));
				}
				catch (Exception ex)
				{
					Log.Error("ERROR in ToUpdateRowStatement(): " + ex.Message, ex);
				}
			}

			var updateSql = string.Format("UPDATE \"{0}\" SET {1} WHERE {2}",
				tableType.Name, sql, sqlFilter);

			return updateSql;
		}

		public static void Update<T>(this IDbCommand dbCommand, T obj)
			where T : new()
		{
			dbCommand.CommandText = ToUpdateRowStatement(obj);
			dbCommand.ExecuteNonQuery();
		}

		public static string ToDeleteRowStatement(this object objWithProperties)
		{
			var sqlFilter = new StringBuilder();

			var tableType = objWithProperties.GetType();
			foreach (var fieldDef in GetFieldDefinitions(tableType))
			{
				try
				{
					if (fieldDef.IsPrimaryKey)
					{
						if (sqlFilter.Length > 0) sqlFilter.Append(" AND ");

						sqlFilter.AppendFormat("\"{0}\" = {1}", fieldDef.Name, fieldDef.GetQuotedValue(objWithProperties));
					}
				}
				catch (Exception ex)
				{
					Log.Error("ERROR in ToDeleteRowStatement(): " + ex.Message, ex);
				}
			}

			var deleteSql = string.Format("DELETE FROM \"{0}\" WHERE {1}",
				tableType.Name, sqlFilter);

			return deleteSql;
		}

		public static void Delete<T>(this IDbCommand dbCommand, T obj)
			where T : new()
		{
			dbCommand.CommandText = ToDeleteRowStatement(obj);
			dbCommand.ExecuteNonQuery();
		}

		public static void DeleteById<T>(this IDbCommand dbCommand, object id)
			where T : new()
		{
			var tableType = typeof(T);

			dbCommand.CommandText = string.Format("DELETE FROM \"{0}\" WHERE Id = {1}",
				tableType.Name, DialectProvider.GetQuotedValue(id, id.GetType()));

			dbCommand.ExecuteNonQuery();
		}

		public static void DeleteByIds<T>(this IDbCommand dbCommand, IEnumerable idValues)
			where T : new()
		{
			var sql = GetIdsInSql(idValues);
			if (sql == null) return;

			var tableType = typeof(T);

			dbCommand.CommandText = string.Format("DELETE FROM \"{0}\" WHERE Id IN ({1})",
				tableType.Name, sql);

			dbCommand.ExecuteNonQuery();
		}

		private static string GetIdsInSql(IEnumerable idValues)
		{
			var sql = new StringBuilder();
			foreach (var idValue in idValues)
			{
				if (sql.Length > 0) sql.Append(",");
				sql.AppendFormat("{0}".SqlFormat(idValue));
			}
			return sql.Length == 0 ? null : sql.ToString();
		}

		public static void Save<T>(this IDbCommand dbCommand, T obj)
			where T : new()
		{
			var id = IdUtils.GetId(obj);
			var existingRow = dbCommand.GetByIdOrDefault<T>(id);
			if (existingRow == null)
			{
				dbCommand.Insert(obj);
			}
			else
			{
				dbCommand.Update(obj);
			}
		}

		public static void SaveAll<T>(this IDbCommand dbCommand, IEnumerable<T> objs)
			where T : new()
		{
			var saveRows = objs.ToList();

			var firstRow = saveRows.FirstOrDefault();
			if (Equals(firstRow, default(T))) return;

			var defaultIdValue = ReflectionUtils.GetDefaultValue(IdUtils.GetId(firstRow).GetType());
			var idMap = saveRows.Where(x => !defaultIdValue.Equals(IdUtils.GetId(x)))
				.ToDictionary(x => IdUtils.GetId(x));

			var existingRowsMap = dbCommand.GetByIds<T>(idMap.Keys).ToDictionary(x => IdUtils.GetId(x));

			using (var dbTrans = dbCommand.Connection.BeginTransaction())
			{
				dbCommand.Transaction = dbTrans;

				foreach (var saveRow in saveRows)
				{
					var id = IdUtils.GetId(saveRow);
					if (id != defaultIdValue && existingRowsMap.ContainsKey(id))
					{
						dbCommand.Update(saveRow);
					}
					else
					{
						dbCommand.Insert(saveRow);
					}
				}

				dbTrans.Commit();
			}
		}

		public static IDbConnection ToDbConnection(this string dbConnectionStringOrFilePath)
		{
			return DialectProvider.CreateConnection(dbConnectionStringOrFilePath, null);
		}

		public static IDbConnection OpenDbConnection(this string dbConnectionStringOrFilePath)
		{
			try
			{
				var sqlConn = dbConnectionStringOrFilePath.ToDbConnection();
				sqlConn.Open();
				return sqlConn;
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public static IDbConnection OpenReadOnlyDbConnection(this string dbConnectionStringOrFilePath)
		{
			try
			{
				var options = new Dictionary<string, string> { { "Read Only", "True" } };

				var sqlConn = DialectProvider.CreateConnection(dbConnectionStringOrFilePath, options);
				sqlConn.Open();
				return sqlConn;
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public static string SqlFormat(this string sqlText, params object[] sqlParams)
		{
			var escapedParams = new List<string>();
			foreach (var sqlParam in sqlParams)
			{
				if (sqlParam == null)
				{
					escapedParams.Add("NULL");
				}
				else
				{
					escapedParams.Add(DialectProvider.GetQuotedValue(sqlParam, sqlParam.GetType()));
				}
			}
			return string.Format(sqlText, escapedParams.ToArray());
		}

		public static string SqlJoin<T>(this List<T> values)
		{
			var sb = new StringBuilder();
			foreach (var value in values)
			{
				if (sb.Length > 0) sb.Append(",");
				sb.Append(DialectProvider.GetQuotedValue(value, value.GetType()));
			}

			return sb.ToString();
		}

	}
}