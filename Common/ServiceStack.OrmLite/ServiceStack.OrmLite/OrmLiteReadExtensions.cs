using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ServiceStack.Common.Text;
using ServiceStack.Common.Utils;

namespace ServiceStack.OrmLite
{
	public static class OrmLiteReadExtensions
	{
		public static Func<int, object> GetValueFn<T>(IDataRecord reader)
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


		public static string ToSelectStatement(this Type tableType)
		{
			return ToSelectStatement(tableType, null);
		}

		public static string ToSelectStatement(this Type tableType, string sqlFilter, params object[] filterParams)
		{
			var sql = new StringBuilder();
			const string SelectStatement = "SELECT ";

			var isFullSelectStatement = 
				!string.IsNullOrEmpty(sqlFilter)
				&& sqlFilter.Length > SelectStatement.Length
				&& sqlFilter.Substring(0, SelectStatement.Length).ToUpper().Equals(SelectStatement);

			if (isFullSelectStatement) return sqlFilter.SqlFormat(filterParams);

			var modelDef = tableType.GetModelDefinition();
			sql.AppendFormat("SELECT {0} FROM \"{1}\"", tableType.GetColumnNames(), modelDef.ModelName);
			if (!string.IsNullOrEmpty(sqlFilter))
			{
				sqlFilter = sqlFilter.SqlFormat(filterParams);
				sql.Append(" WHERE ");
				sql.Append(sqlFilter);
			}

			return sql.ToString();
		}

		public static List<T> Select<T>(this IDbCommand dbCommand)
			where T : new()
		{
			return Select<T>(dbCommand, (string)null);
		}

		public static List<T> Select<T>(this IDbCommand dbCommand, string sqlFilter, params object[] filterParams)
			where T : new()
		{
			dbCommand.CommandText = ToSelectStatement(typeof(T), sqlFilter, filterParams);
			using (var reader = dbCommand.ExecuteReader())
			{
				return reader.ConvertToList<T>();
			}
		}

		public static List<TModel> Select<TModel>(this IDbCommand dbCommand, Type fromTableType)
			where TModel : new()
		{
			return Select<TModel>(dbCommand, fromTableType, null);
		}

		public static List<TModel> Select<TModel>(this IDbCommand dbCommand, Type fromTableType, string sqlFilter, params object[] filterParams)
			where TModel : new()
		{
			var sql = new StringBuilder();
			var modelType = typeof(TModel);
			sql.AppendFormat("SELECT {0} FROM \"{1}\"", modelType.GetColumnNames(), fromTableType.GetModelDefinition().ModelName);
			if (!string.IsNullOrEmpty(sqlFilter))
			{
				sqlFilter = sqlFilter.SqlFormat(filterParams);
				sql.Append(" WHERE ");
				sql.Append(sqlFilter);
			}

			dbCommand.CommandText = sql.ToString();
			using (var reader = dbCommand.ExecuteReader())
			{
				return reader.ConvertToList<TModel>();
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
			var modelDef = typeof (T).GetModelDefinition();
			return First<T>(dbCommand, modelDef.PrimaryKey.FieldName + " = {0}".SqlFormat(idValue));
		}

		public static T GetByIdOrDefault<T>(this IDbCommand dbCommand, object idValue)
			where T : new()
		{
			var modelDef = typeof(T).GetModelDefinition();
			return FirstOrDefault<T>(dbCommand, modelDef.PrimaryKey.FieldName + " = {0}".SqlFormat(idValue));
		}

		public static List<T> GetByIds<T>(this IDbCommand dbCommand, IEnumerable idValues)
			where T : new()
		{
			var sql = idValues.GetIdsInSql();
			if (sql == null) return new List<T>();

			var modelDef = typeof(T).GetModelDefinition();
			return Select<T>(dbCommand, string.Format(modelDef.PrimaryKey.FieldName + " IN ({0})", sql));
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
				return TypeSerializer.DeserializeFromString<T>(reader.GetValue(0).ToString());
			}
			return default(T);
		}

		public static long GetLastInsertId(this IDbCommand dbCmd)
		{
			return OrmLiteConfig.DialectProvider.GetLastInsertId(dbCmd);
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
				return GetLookup<K, V>(dbReader);
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
	}
}