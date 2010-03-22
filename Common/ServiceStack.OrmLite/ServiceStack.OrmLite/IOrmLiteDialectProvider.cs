//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Collections.Generic;
using System.Data;

namespace ServiceStack.OrmLite
{
	public interface IOrmLiteDialectProvider
	{
		int DefaultStringLength { get; set; }
		
		bool UseUnicode { get; set; }

		string EscapeParam(object paramValue);

		object ConvertDbValue(object value, Type type);

		string GetQuotedValue(object value, Type fieldType);

		IDbConnection CreateConnection(string filePath, Dictionary<string, string> options);

		string GetColumnDefinition(
			string fieldName, Type fieldType, bool isPrimaryKey, bool autoIncrement, 
			bool isNullable, int? fieldLength, string defaultValue);

		long GetLastInsertId(IDbCommand command);
	}
}