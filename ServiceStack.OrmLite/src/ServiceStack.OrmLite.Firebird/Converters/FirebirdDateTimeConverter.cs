using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Firebird.Converters
{
    public class FirebirdDateTimeConverter : DateTimeConverter
    {
        // "LOCALTIME" is a context variable (current time, no TZ), NOT a valid column type -> FB rejects it in
        // DDL (SQL error -104, token unknown). A .NET DateTime (no offset) maps to TIMESTAMP (WITHOUT TIME ZONE;
        // plain TIMESTAMP is still no-TZ in FB4/5). WITH-TIME-ZONE is the separate DateTimeOffset mapping.
        public override string ColumnDefinition
        {
            get { return "TIMESTAMP"; }
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