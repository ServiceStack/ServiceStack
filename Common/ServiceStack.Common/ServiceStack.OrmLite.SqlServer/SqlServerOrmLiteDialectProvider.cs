using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace ServiceStack.OrmLite.SqlServer
{
	public class SqlServerOrmLiteDialectProvider 
		: OrmLiteDialectProviderBase
	{
		public SqlServerOrmLiteDialectProvider()
		{
			base.DateTimeColumnDefinition = base.StringColumnDefinition;
			base.BoolColumnDefinition = base.IntColumnDefinition;
			base.GuidColumnDefinition = "UniqueIdentifier";

			base.InitColumnTypeMap();
		}

		public override IDbConnection CreateConnection(string connectionString, Dictionary<string, string> options)
		{
			var isFullConnectionString = connectionString.Contains(";");

			if (!isFullConnectionString)
			{
				var filePath = connectionString;

				var filePathWithExt = filePath.ToLower().EndsWith(".mdf")
					? filePath 
					: filePath + ".mdf";

				var fileName = Path.GetFileName(filePathWithExt);
				var dbName = fileName.Substring(0, fileName.Length - ".mdf".Length);

				connectionString = string.Format(
				@"Data Source=.\SQLEXPRESS;AttachDbFilename={0};Initial Catalog={1};Integrated Security=True;User Instance=True;",
					filePathWithExt, dbName);
			}

			if (options != null)
			{
				foreach (var option in options)
				{
					if (option.Key.ToLower() == "read only")
					{
						if (option.Value.ToLower() == "true")
						{
							connectionString += "Mode=Read Only;";
						}
						continue;
					}
					connectionString += option.Key + "=" + option.Value + ";";
				}
			}

			return new SqlConnection(connectionString);
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
			if (value == null) return "NULL";

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