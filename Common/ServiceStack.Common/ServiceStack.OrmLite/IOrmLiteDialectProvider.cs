using System;
using System.Data;

namespace ServiceStack.OrmLite
{
	public interface IOrmLiteDialectProvider
	{
		string EscapeParam(object paramValue);

		object ConvertDbValue(object value, Type type);

		string GetQuotedValue(object value, Type type);

		IDbConnection CreateConnection(string connectionString);

		string GetColumnDefinition(string fieldName, Type fieldType, bool isPrimaryKey, bool autoIncrement, bool isNullable);
	}
}