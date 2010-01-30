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
using System.Text;
using ServiceStack.Common.Text;
using ServiceStack.Common.Utils;

namespace ServiceStack.OrmLite
{
	public abstract class OrmLiteDialectProviderBase
		: IOrmLiteDialectProvider
	{
		#region ADO.NET supported types
		/* ADO.NET UNDERSTOOD DATA TYPES:
			COUNTER	DbType.Int64
			AUTOINCREMENT	DbType.Int64
			IDENTITY	DbType.Int64
			LONG	DbType.Int64
			TINYINT	DbType.Byte
			INTEGER	DbType.Int64
			INT	DbType.Int32
			VARCHAR	DbType.String
			NVARCHAR	DbType.String
			CHAR	DbType.String
			NCHAR	DbType.String
			TEXT	DbType.String
			NTEXT	DbType.String
			STRING	DbType.String
			DOUBLE	DbType.Double
			FLOAT	DbType.Double
			REAL	DbType.Single
			BIT	DbType.Boolean
			YESNO	DbType.Boolean
			LOGICAL	DbType.Boolean
			BOOL	DbType.Boolean
			NUMERIC	DbType.Decimal
			DECIMAL	DbType.Decimal
			MONEY	DbType.Decimal
			CURRENCY	DbType.Decimal
			TIME	DbType.DateTime
			DATE	DbType.DateTime
			TIMESTAMP	DbType.DateTime
			DATETIME	DbType.DateTime
			BLOB	DbType.Binary
			BINARY	DbType.Binary
			VARBINARY	DbType.Binary
			IMAGE	DbType.Binary
			GENERAL	DbType.Binary
			OLEOBJECT	DbType.Binary
			GUID	DbType.Guid
			UNIQUEIDENTIFIER	DbType.Guid
			MEMO	DbType.String
			NOTE	DbType.String
			LONGTEXT	DbType.String
			LONGCHAR	DbType.String
			SMALLINT	DbType.Int16
			BIGINT	DbType.Int64
			LONGVARCHAR	DbType.String
			SMALLDATE	DbType.DateTime
			SMALLDATETIME	DbType.DateTime
		 */
		#endregion

		public string AutoIncrementDefinition = "AUTOINCREMENT"; //SqlServer express limit
		public string StringColumnDefinition = "VARCHAR(8000)"; //SqlServer express limit
		public string StringLengthColumnDefinitionFormat = "VARCHAR({0})";
		public string IntColumnDefinition = "INTEGER";
		public string LongColumnDefinition = "BIGINT";
		public string GuidColumnDefinition = "GUID";
		public string BoolColumnDefinition = "BOOL";
		public string RealColumnDefinition = "DOUBLE";
		public string DecimalColumnDefinition = "DECIMAL";
		public string BlobColumnDefinition = "BLOB";
		public string DateTimeColumnDefinition = "DATETIME";

		protected Dictionary<Type, string> ColumnTypeMap;

		public virtual bool UseUnicode
		{
			set
			{
				this.StringColumnDefinition = value
					? "NVARCHAR(8000)"
					: "VARCHAR(8000)";

				this.StringLengthColumnDefinitionFormat = value
					? "NVARCHAR({0})"
					: "VARCHAR({0})";
			}
		}

		protected OrmLiteDialectProviderBase()
		{
			InitColumnTypeMap();
		}

		protected void InitColumnTypeMap()
		{
			ColumnTypeMap = new Dictionary<Type, string>
        	{
        		{ typeof(string), StringColumnDefinition },
        		{ typeof(char), StringColumnDefinition },
        		{ typeof(char[]), StringColumnDefinition },

        		{ typeof(bool), BoolColumnDefinition },

        		{ typeof(Guid), GuidColumnDefinition },

        		{ typeof(DateTime), DateTimeColumnDefinition },
        		{ typeof(TimeSpan), DateTimeColumnDefinition },

        		{ typeof(byte), IntColumnDefinition },
        		{ typeof(sbyte), IntColumnDefinition },
        		{ typeof(short), IntColumnDefinition },
        		{ typeof(ushort), IntColumnDefinition },
        		{ typeof(int), IntColumnDefinition },
        		{ typeof(uint), IntColumnDefinition },
        		{ typeof(long), LongColumnDefinition },
        		{ typeof(ulong), LongColumnDefinition },

        		{ typeof(float), RealColumnDefinition },
        		{ typeof(double), RealColumnDefinition },
        		{ typeof(decimal), DecimalColumnDefinition },

        		{ typeof(byte[]), BlobColumnDefinition },
        	};
		}

		public string DefaultValueFormat = " DEFAULT ({0})";

		public virtual bool ShouldQuoteValue(Type fieldType)
		{
			string fieldDefinition;
			if (!ColumnTypeMap.TryGetValue(fieldType, out fieldDefinition))
			{
				fieldDefinition = this.GetUndefinedColumnDefintion(fieldType);
			}

			return fieldDefinition != IntColumnDefinition
				   && fieldDefinition != LongColumnDefinition
				   && fieldDefinition != RealColumnDefinition
				   && fieldDefinition != DecimalColumnDefinition
				   && fieldDefinition != BoolColumnDefinition;
		}

		public virtual object ConvertDbValue(object value, Type type)
		{
			if (value == null || value.GetType() == typeof(DBNull)) return null;

			if (type == typeof(string))
			{
				return value;
			}

			var convertedValue = TypeSerializer.DeserializeFromString(value.ToString(), type);
			return convertedValue;
		}

		public virtual string GetQuotedValue(object value, Type fieldType)
		{
			if (value == null) return "NULL";

			if (!fieldType.UnderlyingSystemType.IsValueType && fieldType != typeof(string))
			{
				if (TypeSerializer.CanCreateFromString(fieldType))
				{
					return "'" + EscapeParam(TypeSerializer.SerializeToString(value)) + "'";
				}

				throw new NotSupportedException(
					string.Format("Property of type: {0} is not supported", fieldType.FullName));
			}

			return ShouldQuoteValue(fieldType)
					? "'" + EscapeParam(value) + "'"
					: value.ToString();
		}

		public abstract IDbConnection CreateConnection(string filePath, Dictionary<string, string> options);

		public virtual string EscapeParam(object paramValue)
		{
			return paramValue.ToString().Replace("'", "''");
		}

		protected virtual string GetUndefinedColumnDefintion(Type fieldType)
		{
			if (TypeSerializer.CanCreateFromString(fieldType))
			{
				return this.StringColumnDefinition;
			}

			throw new NotSupportedException(
				string.Format("Property of type: {0} is not supported", fieldType.FullName));
		}

		public virtual string GetColumnDefinition(string fieldName, Type fieldType, bool isPrimaryKey, bool autoIncrement, bool isNullable, int? fieldLength, string defaultValue)
		{
			string fieldDefinition;

			if (fieldType == typeof(string) && fieldLength.HasValue)
			{
				fieldDefinition = string.Format(StringLengthColumnDefinitionFormat, fieldLength.Value);
			}
			else
			{
				if (!ColumnTypeMap.TryGetValue(fieldType, out fieldDefinition))
				{
					fieldDefinition = this.GetUndefinedColumnDefintion(fieldType);
				}
			}

			var sql = new StringBuilder();
			sql.AppendFormat("\"{0}\" {1}", fieldName, fieldDefinition);

			if (isPrimaryKey)
			{
				sql.Append(" PRIMARY KEY");
				if (autoIncrement)
				{
					sql.Append(" ").Append(AutoIncrementDefinition);
				}
			}
			else
			{
				if (isNullable)
				{
					sql.Append(" NULL");
				}
				else
				{
					sql.Append(" NOT NULL");
				}
			}

			if (!string.IsNullOrEmpty(defaultValue))
			{
				sql.AppendFormat(DefaultValueFormat, defaultValue);
			}

			return sql.ToString();
		}

		public abstract long GetLastInsertId(IDbCommand command);
	}
}