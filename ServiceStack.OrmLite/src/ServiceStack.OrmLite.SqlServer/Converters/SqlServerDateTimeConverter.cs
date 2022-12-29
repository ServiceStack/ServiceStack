using System;
using ServiceStack.OrmLite.Converters;
using System.Globalization;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    public class SqlServerDateTimeConverter : DateTimeConverter
    {
        const string DateTimeFormat = "yyyyMMdd HH:mm:ss.fff";

        public override string ToQuotedString(Type fieldType, object value)
        {
            return DateTimeFmt((DateTime)value, DateTimeFormat);
        }

        public override object FromDbValue(Type fieldType, object value)
        {            
            if (value is string str && DateTime.TryParseExact(str, DateTimeFormat, null, DateTimeStyles.None, out var date))
                return date;
            
            return base.FromDbValue(fieldType, value);
        }
    }
}