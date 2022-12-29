using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.MySql.Converters
{
    public class MySqlGuidConverter : GuidConverter
    {
        public override string ColumnDefinition => "CHAR(36)";

        public override string ToQuotedString(Type fieldType, object value)
        {
            var guid = (Guid)value;
            return DialectProvider.GetQuotedValue(guid.ToString("d"), typeof(string));
        }
    }
}