using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.DataAccess;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite
{
	public static class OrmLiteWriteExtensions
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(OrmLiteWriteExtensions));

		public static string ToCreateTableStatement(this Type tableType)
		{
			var sbColumns = new StringBuilder();
			var sbConstraints = new StringBuilder();

			var modelDef = tableType.GetModelDefinition();
			foreach (var fieldDef in modelDef.FieldDefinitions)
			{
				if (sbColumns.Length != 0) sbColumns.Append(", ");

				var columnDefinition = OrmLiteConfig.DialectProvider.GetColumnDefinition(
					fieldDef.FieldName,
					fieldDef.FieldType,
					fieldDef.IsPrimaryKey,
					fieldDef.AutoIncrement,
					fieldDef.IsNullable,
					fieldDef.FieldLength,
					fieldDef.DefaultValue);

				sbColumns.AppendLine(columnDefinition);

				if (fieldDef.ReferencesType == null) continue;

				var refModelDef = fieldDef.ReferencesType.GetModelDefinition();
				sbConstraints.AppendFormat(", CONSTRAINT \"FK_{0}_{1}\" FOREIGN KEY (\"{2}\") REFERENCES \"{3}\" (\"{4}\") \n",
					modelDef.ModelName, refModelDef.ModelName, fieldDef.FieldName, refModelDef.ModelName, modelDef.PrimaryKey.FieldName);
			}

			var sql = new StringBuilder(string.Format(
				"CREATE TABLE \"{0}\" ({1}{2}); \n", modelDef.ModelName, sbColumns, sbConstraints));

			return sql.ToString();
		}

		public static List<string> ToCreateIndexStatements(this Type tableType)
		{
			var sqlIndexes = new List<string>();

			var modelDef = tableType.GetModelDefinition();
			foreach (var fieldDef in modelDef.FieldDefinitions)
			{
				if (!fieldDef.IsIndexed) continue;

				var indexName = GetIndexName(fieldDef.IsUnique, modelDef.ModelName.SafeVarName(), fieldDef.FieldName);

				sqlIndexes.Add(
					ToCreateIndexStatement(fieldDef.IsUnique, indexName, modelDef.ModelName, fieldDef.FieldName));
			}

			foreach (var compositeIndex in modelDef.CompositeIndexes)
			{
				var indexName = GetIndexName(compositeIndex.Unique, modelDef.ModelName.SafeVarName(),
					string.Join("_", compositeIndex.FieldNames.ToArray()));

				var indexNames = string.Join("\" ASC, \"", compositeIndex.FieldNames.ToArray());

				sqlIndexes.Add(
					ToCreateIndexStatement(compositeIndex.Unique, indexName, modelDef.ModelName, indexNames));
			}

			return sqlIndexes;
		}

		private static string GetIndexName(bool isUnique, string modelName, string fieldName)
		{
			return string.Format("{0}idx_{1}_{2}", isUnique ? "u" : "", modelName, fieldName).ToLower();
		}

		private static string ToCreateIndexStatement(bool isUnique, string indexName, string modelName, string fieldName)
		{
			return string.Format("CREATE {0} INDEX {1} ON \"{2}\" (\"{3}\" ASC); \n",
					isUnique ? "UNIQUE" : "", indexName, modelName, fieldName);
		}

		public static void CreateTables(this IDbCommand dbCommand, bool overwrite, params Type[] tableTypes)
		{
			foreach (var tableType in tableTypes)
			{
				CreateTable(dbCommand, overwrite, tableType);
			}
		}

		public static void CreateTable<T>(this IDbCommand dbCommand, bool overwrite)
			where T : new()
		{
			var tableType = typeof(T);
			CreateTable(dbCommand, overwrite, tableType);
		}

		public static void CreateTable(this IDbCommand dbCommand, bool overwrite, Type modelType)
		{
			var modelDef = modelType.GetModelDefinition();
			if (overwrite)
			{
				try
				{
					dbCommand.CommandText = string.Format("DROP TABLE \"{0}\";", modelDef.ModelName);
					dbCommand.ExecuteNonQuery();
				}
				catch (Exception ex)
				{
					Log.DebugFormat("Cannot drop non-existing table '{0}': {1}", modelDef.ModelName, ex.Message);
				}
			}

			try
			{
				dbCommand.CommandText = ToCreateTableStatement(modelType);
				dbCommand.ExecuteNonQuery();

				var sqlIndexes = ToCreateIndexStatements(modelType);
				foreach (var sqlIndex in sqlIndexes)
				{
					try
					{
						dbCommand.CommandText = sqlIndex;
						dbCommand.ExecuteNonQuery();
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
			}
			catch (Exception ex)
			{
				if (IgnoreAlreadyExistsError(ex))
				{
					Log.DebugFormat("Ignoring existing table '{0}': {1}", modelDef.ModelName, ex.Message);
					return;
				}
				throw;
			}

		}

		private static bool IgnoreAlreadyExistsError(Exception ex)
		{
			//ignore Sqlite table already exists error
			const string sqliteAlreadyExistsError = "already exists";
			const string sqlServerAlreadyExistsError = "There is already an object named";
			return ex.Message.Contains(sqliteAlreadyExistsError)
			       || ex.Message.Contains(sqlServerAlreadyExistsError);
		}

		public static T PopulateWithSqlReader<T>(this T objWithProperties, IDataReader dataReader)
		{
			var i = 0;
			var tableType = objWithProperties.GetType();

			var modelDef = tableType.GetModelDefinition();
			foreach (var fieldDef in modelDef.FieldDefinitions)
			{
				var value = dataReader.GetValue(i++);

				try
				{
					fieldDef.SetValue(objWithProperties, value);
				}
				catch (Exception ex)
				{
					throw;
				}
			}
			return objWithProperties;
		}


		public static string ToInsertRowStatement(this object objWithProperties)
		{
			var sbColumnNames = new StringBuilder();
			var sbColumnValues = new StringBuilder();

			var tableType = objWithProperties.GetType();
			var modelDef = tableType.GetModelDefinition();
			foreach (var fieldDef in modelDef.FieldDefinitions)
			{
				if (fieldDef.AutoIncrement) continue;

				if (sbColumnNames.Length > 0) sbColumnNames.Append(",");
				if (sbColumnValues.Length > 0) sbColumnValues.Append(",");

				try
				{
					sbColumnNames.Append(string.Format("\"{0}\"", fieldDef.FieldName));
					sbColumnValues.Append(fieldDef.GetQuotedValue(objWithProperties));
				}
				catch (Exception ex)
				{
					Log.Error("ERROR in ToInsertRowStatement(): " + ex.Message, ex);
					throw;
				}
			}

			var sql = string.Format("INSERT INTO \"{0}\" ({1}) VALUES ({2});",
									modelDef.ModelName, sbColumnNames, sbColumnValues);

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
			var modelDef = tableType.GetModelDefinition();
			foreach (var fieldDef in modelDef.FieldDefinitions)
			{
				try
				{
					if (fieldDef.IsPrimaryKey)
					{
						if (sqlFilter.Length > 0) sqlFilter.Append(" AND ");

						sqlFilter.AppendFormat("\"{0}\" = {1}", fieldDef.FieldName, fieldDef.GetQuotedValue(objWithProperties));
							
						continue;
					}

					if (sql.Length > 0) sql.Append(",");
					sql.AppendFormat("\"{0}\" = {1}", fieldDef.FieldName, fieldDef.GetQuotedValue(objWithProperties));
				}
				catch (Exception ex)
				{
					Log.Error("ERROR in ToUpdateRowStatement(): " + ex.Message, ex);
				}
			}

			var updateSql = string.Format("UPDATE \"{0}\" SET {1} WHERE {2}",
				modelDef.ModelName, sql, sqlFilter);

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
			var modelDef = tableType.GetModelDefinition();
			foreach (var fieldDef in modelDef.FieldDefinitions)
			{
				try
				{
					if (fieldDef.IsPrimaryKey)
					{
						if (sqlFilter.Length > 0) sqlFilter.Append(" AND ");

						sqlFilter.AppendFormat("\"{0}\" = {1}", fieldDef.FieldName, fieldDef.GetQuotedValue(objWithProperties));
					}
				}
				catch (Exception ex)
				{
					Log.Error("ERROR in ToDeleteRowStatement(): " + ex.Message, ex);
				}
			}

			var deleteSql = string.Format("DELETE FROM \"{0}\" WHERE {1}",
				modelDef.ModelName, sqlFilter);

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
			var modelDef = typeof(T).GetModelDefinition();

			dbCommand.CommandText = string.Format("DELETE FROM \"{0}\" WHERE \"{1}\" = {2}",
				modelDef.ModelName, modelDef.PrimaryKey.FieldName, 
				OrmLiteConfig.DialectProvider.GetQuotedValue(id, id.GetType()));

			dbCommand.ExecuteNonQuery();
		}

		public static void DeleteByIds<T>(this IDbCommand dbCommand, IEnumerable idValues)
			where T : new()
		{
			var sql = idValues.GetIdsInSql();
			if (sql == null) return;

			var modelDef = typeof(T).GetModelDefinition();

			dbCommand.CommandText = string.Format("DELETE FROM \"{0}\" WHERE \"{1}\" IN ({2})",
				modelDef.ModelName, modelDef.PrimaryKey.FieldName, sql);

			dbCommand.ExecuteNonQuery();
		}

		public static void DeleteAll<T>(this IDbCommand dbCommand)
		{
			DeleteAll(dbCommand, typeof(T));
		}

		public static void DeleteAll(this IDbCommand dbCommand, Type tableType)
		{
			Delete(dbCommand, tableType, null);
		}

		public static void Delete<T>(this IDbCommand dbCommand, string sqlFilter, params object[] filterParams)
			where T : new()
		{
			Delete(dbCommand, typeof(T), sqlFilter, filterParams);
		}

		public static void Delete(this IDbCommand dbCommand, Type tableType, string sqlFilter, params object[] filterParams)
		{
			dbCommand.CommandText = ToDeleteStatement(tableType, sqlFilter, filterParams);
			dbCommand.ExecuteNonQuery();
		}

		public static string ToDeleteStatement(this Type tableType, string sqlFilter, params object[] filterParams)
		{
			var sql = new StringBuilder();
			const string DeleteStatement = "DELETE ";

			var isFullDeleteStatement = 
				!string.IsNullOrEmpty(sqlFilter)
				&& sqlFilter.Length > DeleteStatement.Length
				&& sqlFilter.Substring(0, DeleteStatement.Length).ToUpper().Equals(DeleteStatement);

			if (isFullDeleteStatement) return sqlFilter.SqlFormat(filterParams);

			var modelDef = tableType.GetModelDefinition();
			sql.AppendFormat("DELETE FROM \"{0}\"", modelDef.ModelName);
			if (!string.IsNullOrEmpty(sqlFilter))
			{
				sqlFilter = sqlFilter.SqlFormat(filterParams);
				sql.Append(" WHERE ");
				sql.Append(sqlFilter);
			}

			return sql.ToString();
		}

		public static void Save<T>(this IDbCommand dbCommand, T obj)
			where T : new()
		{
			var id = IdUtils.GetId(obj);
			var existingRow = dbCommand.GetByIdOrDefault<T>(id);
			if (Equals(existingRow, default(T)))
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

	}
}