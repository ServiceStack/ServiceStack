using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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

			var indexAttrs = tableType.GetCustomAttributes(typeof(IndexAttribute), true).ToList();

			foreach (var fieldDef in tableType.GetFieldDefinitions())
			{
				if (sbColumns.Length != 0) sbColumns.Append(", ");

				var columnDefinition = OrmLiteConfig.DialectProvider.GetColumnDefinition(
					fieldDef.Name,
					fieldDef.FieldType,
					fieldDef.IsPrimaryKey,
					fieldDef.AutoIncrement,
					fieldDef.IsNullable, 
					fieldDef.FieldLength, 
					fieldDef.DefaultValue);

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

		public static void CreateTable(this IDbCommand dbCommand, bool overwrite, Type tableType)
		{
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
				const string sqliteTableExistsError = "already exists";
				const string sqlServerAlreadyExistsError = "There is already an object named";
				if (ex.Message.Contains(sqliteTableExistsError)
				    || ex.Message.Contains(sqlServerAlreadyExistsError))
				{
					Log.DebugFormat("Ignoring existing table '{0}': {1}", tableType.Name, ex.Message);
					return;
				}
				throw;
			}
		}


		public static T PopulateWithSqlReader<T>(this T objWithProperties, IDataReader dataReader)
		{
			var i = 0;
			var tableType = objWithProperties.GetType();
			foreach (var fieldDef in tableType.GetFieldDefinitions())
			{
				var value = dataReader.GetValue(i++);

				fieldDef.SetValue(objWithProperties, value);
			}
			return objWithProperties;
		}


		public static string ToInsertRowStatement(this object objWithProperties)
		{
			var sbColumnNames = new StringBuilder();
			var sbColumnValues = new StringBuilder();

			var tableType = objWithProperties.GetType();
			foreach (var fieldDef in tableType.GetFieldDefinitions())
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
			foreach (var fieldDef in tableType.GetFieldDefinitions())
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
			foreach (var fieldDef in tableType.GetFieldDefinitions())
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
				tableType.Name, OrmLiteConfig.DialectProvider.GetQuotedValue(id, id.GetType()));

			dbCommand.ExecuteNonQuery();
		}

		public static void DeleteByIds<T>(this IDbCommand dbCommand, IEnumerable idValues)
			where T : new()
		{
			var sql = idValues.GetIdsInSql();
			if (sql == null) return;

			var tableType = typeof(T);

			dbCommand.CommandText = string.Format("DELETE FROM \"{0}\" WHERE Id IN ({1})",
				tableType.Name, sql);

			dbCommand.ExecuteNonQuery();
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

	}
}