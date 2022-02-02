using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Oracle.Converters
{
    public class OracleDateTimeConverter : DateTimeConverter
    {
        public override string ColumnDefinition
        {
            get { return "TIMESTAMP"; }
        }

        public OracleOrmLiteDialectProvider OracleDialect
        {
            get { return (OracleOrmLiteDialectProvider)DialectProvider;}
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            return OracleDialect.GetQuotedDateTimeValue((DateTime)value);
        }
    }
}