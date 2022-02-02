using System;
using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite
{
    public static class OrmLiteDialectProviderExtensions
    {
        public static string GetParam(this IOrmLiteDialectProvider dialect, string name, string format)
        {
            var ret = dialect.ParamString + (dialect.ParamNameFilter?.Invoke(name) ?? name);
            return format == null
                ? ret
                : string.Format(format, ret);
        }

        public static string GetParam(this IOrmLiteDialectProvider dialect, string name)
        {
            return dialect.ParamString + (dialect.ParamNameFilter?.Invoke(name) ?? name);
        }

        public static string GetParam(this IOrmLiteDialectProvider dialect, int indexNo = 0)
        {
            return dialect.ParamString + indexNo;
        }

        public static string ToFieldName(this IOrmLiteDialectProvider dialect, string paramName)
        {
            return paramName.Substring(dialect.ParamString.Length);
        }

        public static string FmtTable(this string tableName, IOrmLiteDialectProvider dialect = null)
        {
            return (dialect ?? OrmLiteConfig.DialectProvider).NamingStrategy.GetTableName(tableName);
        }

        public static string FmtColumn(this string columnName, IOrmLiteDialectProvider dialect=null)
        {
            return (dialect ?? OrmLiteConfig.DialectProvider).NamingStrategy.GetColumnName(columnName);
        }

        public static string GetQuotedColumnName(this IOrmLiteDialectProvider dialect, 
            FieldDefinition fieldDef)
        {
            return dialect.GetQuotedColumnName(fieldDef.FieldName);
        }

        public static string GetQuotedColumnName(this IOrmLiteDialectProvider dialect,
            ModelDefinition tableDef, FieldDefinition fieldDef)
        {
            return dialect.GetQuotedTableName(tableDef) +
                "." +
                dialect.GetQuotedColumnName(fieldDef.FieldName);
        }

        public static string GetQuotedColumnName(this IOrmLiteDialectProvider dialect,
            ModelDefinition tableDef, string tableAlias, FieldDefinition fieldDef)
        {
            if (tableAlias == null)
                return dialect.GetQuotedColumnName(tableDef, fieldDef);
            
            return dialect.GetQuotedTableName(tableAlias) //aliases shouldn't have schemas
                   + "." +
                   dialect.GetQuotedColumnName(fieldDef.FieldName);
        }

        public static string GetQuotedColumnName(this IOrmLiteDialectProvider dialect,
            ModelDefinition tableDef, string fieldName)
        {
            return dialect.GetQuotedTableName(tableDef) +
                   "." +
                   dialect.GetQuotedColumnName(fieldName);
        }

        public static string GetQuotedColumnName(this IOrmLiteDialectProvider dialect,
            ModelDefinition tableDef, string tableAlias, string fieldName)
        {
            if (tableAlias == null)
                return dialect.GetQuotedColumnName(tableDef, fieldName);
            
            return dialect.GetQuotedTableName(tableAlias) //aliases shouldn't have schemas 
                + "." +
                dialect.GetQuotedColumnName(fieldName);
        }

        public static object FromDbValue(this IOrmLiteDialectProvider dialect, 
            IDataReader reader, int columnIndex, Type type)
        {
            return dialect.FromDbValue(dialect.GetValue(reader, columnIndex, type), type);
        }

        public static IOrmLiteConverter GetConverter<T>(this IOrmLiteDialectProvider dialect)
        {
            return dialect.GetConverter(typeof(T));
        }

        public static bool HasConverter(this IOrmLiteDialectProvider dialect, Type type)
        {
            return dialect.GetConverter(type) != null;
        }

        public static StringConverter GetStringConverter(this IOrmLiteDialectProvider dialect)
        {
            return (StringConverter)dialect.GetConverter(typeof(string));
        }

        public static DecimalConverter GetDecimalConverter(this IOrmLiteDialectProvider dialect)
        {
            return (DecimalConverter)dialect.GetConverter(typeof(decimal));
        }

        public static DateTimeConverter GetDateTimeConverter(this IOrmLiteDialectProvider dialect)
        {
            return (DateTimeConverter)dialect.GetConverter(typeof(DateTime));
        }

        public static bool IsMySqlConnector(this IOrmLiteDialectProvider dialect) => 
            dialect.GetType().Name == "MySqlConnectorDialectProvider";

        public static void InitDbParam(this IOrmLiteDialectProvider dialect, IDbDataParameter dbParam, Type columnType)
        {
            var converter = dialect.GetConverterBestMatch(columnType);
            converter.InitDbParam(dbParam, columnType);
        }

        public static void InitDbParam(this IOrmLiteDialectProvider dialect, IDbDataParameter dbParam, Type columnType, object value)
        {
            var converter = dialect.GetConverterBestMatch(columnType);
            converter.InitDbParam(dbParam, columnType);
            dbParam.Value = converter.ToDbValue(columnType, value);
        }

        public static string SqlSpread<T>(this IOrmLiteDialectProvider dialect, params T[] values) =>
            OrmLiteUtils.SqlJoin(values, dialect);

    }
}