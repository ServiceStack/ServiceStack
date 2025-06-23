using System;
using System.Globalization;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters
{
    public class KingbaseSqlDateTimeOffsetConverter : DateTimeOffsetConverter
    {
        public override string ColumnDefinition => "timestamp with time zone";

        public override string ToQuotedString(Type fieldType, object value)
        {
            var dateValue = (DateTimeOffset) value;
            const string iso8601Format = "yyyy-MM-dd HH:mm:ss.fff zzz";
            return base.DialectProvider.GetQuotedValue(dateValue.ToString(iso8601Format, CultureInfo.InvariantCulture), typeof(string));
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            return value;
        }
    }
}
