using System;
using System.Data;
using System.Reflection;
using System.Text;
using Mono.Data.Sqlite;

namespace ServiceStack.OrmLite.Sqlite
{
	public class SqliteOrmLiteDialectProvider : OrmLiteDialectProviderBase
	{
		public SqliteOrmLiteDialectProvider()
		{
			base.DateTimeColumnDefinition = base.StringColumnDefinition;
			base.BoolColumnDefinition = base.IntColumnDefinition;
			base.GuidColumnDefinition = "CHAR(32)";

			base.InitColumnTypeMap();
		}

		public static string CreateFullTextCreateTableStatement(object objectWithProperties)
		{
			var sbColumns = new StringBuilder();
			foreach (var propertyInfo in objectWithProperties.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				var columnDefinition = (sbColumns.Length == 0)
                   	? string.Format("{0} TEXT PRIMARY KEY", propertyInfo.Name)
                   	: string.Format(", {0} TEXT", propertyInfo.Name);

				sbColumns.AppendLine(columnDefinition);
			}

			var tableName = objectWithProperties.GetType().Name;
			var sql = string.Format("CREATE VIRTUAL TABLE \"{0}\" USING FTS3 ({1});", tableName, sbColumns);

			return sql;
		}

		public override IDbConnection CreateConnection(string connectionString)
		{
			var isFullConnectionString = connectionString.Contains(";");

			if (!isFullConnectionString)
			{
				connectionString =
					@"Data Source=" + connectionString + ";Version=3;New=True;Compress=True;";
			}

			return new SqliteConnection(connectionString);
		}

		public override object ConvertDbValue(object value, Type type)
		{
			if (value == null) return null;

			if (type == typeof(bool))
			{
				var intVal = int.Parse(value.ToString());
				return intVal != 0;
			}

			return base.ConvertDbValue(value, type);
		}

		public override string GetQuotedValue(object value, Type type)
		{
			if (type == typeof(Guid))
			{
				var guidValue = (Guid)value;
				return base.GetQuotedValue(guidValue.ToString("N"), typeof(string));
			}
			if (type == typeof(DateTime))
			{
				var dateValue = (DateTime)value;
				const string iso8601Format = "yyyy-MM-dd HH:mm:ss.fffffff";
				return base.GetQuotedValue(dateValue.ToString(iso8601Format), typeof(string));
			}
			if (type == typeof(bool))
			{
				var boolValue = (bool)value;
				return base.GetQuotedValue(boolValue ? 1 : 0, typeof(int));
			}

			return base.GetQuotedValue(value, type);
		}
	}
}