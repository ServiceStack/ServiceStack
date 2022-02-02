using System;
using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Sqlite.Converters
{
    public class SqliteDateTimeOffsetConverter : DateTimeOffsetConverter
    {
        public override string ColumnDefinition
        {
            get { return "VARCHAR(8000)"; }
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            var dateTimeOffsetValue = (DateTimeOffset)value;
            return base.DialectProvider.GetQuotedValue(dateTimeOffsetValue.ToString("o"), typeof(string));
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            var dateTimeOffsetValue = (DateTimeOffset)value;
            return dateTimeOffsetValue.ToString("o");
        }
    }
}