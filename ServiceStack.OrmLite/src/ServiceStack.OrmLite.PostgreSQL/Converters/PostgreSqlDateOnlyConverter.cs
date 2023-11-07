using System;
using ServiceStack.OrmLite.Converters;

#if NET6_0_OR_GREATER
namespace ServiceStack.OrmLite.PostgreSQL.Converters
{
    public class PostgreSqlDateOnlyConverter : PostgreSqlDateTimeConverter
    {
        public override string ToQuotedString(Type fieldType, object value)
        {
            var dateOnly = (DateOnly)value;
            return DateTimeFmt(dateOnly.ToDateTime(default, DateTimeKind.Utc), "yyyy-MM-dd HH:mm:ss.fff");
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            var dateOnly = (DateOnly)value;
            var dateTime = dateOnly.ToDateTime(default, DateTimeKind.Utc);
            if (dateTime.Kind != DateTimeKind.Utc)
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return dateTime;
        }

        public override object FromDbValue(object value)
        {
            var dateTime = (DateTime)base.FromDbValue(value);
            if (dateTime.Kind == DateTimeKind.Unspecified)
                dateTime = dateTime.ToLocalTime();
            var dateOnly = DateOnly.FromDateTime(dateTime);
            return dateOnly;
        }
    }
}
#endif
