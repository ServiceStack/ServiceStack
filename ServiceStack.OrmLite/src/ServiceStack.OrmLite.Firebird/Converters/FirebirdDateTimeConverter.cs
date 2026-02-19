using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Firebird.Converters
{
    public class FirebirdDateTimeConverter : DateTimeConverter
    {
        public override string ColumnDefinition
        {
            get { return "LOCALTIME"; }
        }

        public override string ToQuotedString(Type fieldType, object value)
        {
            var dateTime = (DateTime)value;
            var iso8601Format = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
                .EndsWith("00:00:00.000")
                ? "yyyy-MM-dd"
                : "yyyy-MM-dd HH:mm:ss.fff";

            return DateTimeFmt(dateTime, iso8601Format);
        }
    }
}