using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ServiceStack.OrmLite
{
	public static class OrmLiteUtilExtensions
	{
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

		internal static string GetColumnNames(this Type tableType)
		{
			var sqlColumns = new StringBuilder();
			tableType.GetModelDefinition().FieldDefinitions
				.ForEach(x => sqlColumns.AppendFormat(
					"{0}\"{1}\" ", sqlColumns.Length > 0 ? "," : "", x.Name));

			return sqlColumns.ToString();
		}

		internal static string GetIdsInSql(this IEnumerable idValues)
		{
			var sql = new StringBuilder();
			foreach (var idValue in idValues)
			{
				if (sql.Length > 0) sql.Append(",");
				sql.AppendFormat("{0}".SqlFormat(idValue));
			}
			return sql.Length == 0 ? null : sql.ToString();
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
					escapedParams.Add(OrmLiteConfig.DialectProvider.GetQuotedValue(sqlParam, sqlParam.GetType()));
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
				sb.Append(OrmLiteConfig.DialectProvider.GetQuotedValue(value, value.GetType()));
			}

			return sb.ToString();
		}
	}
}