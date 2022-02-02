using System;
using System.Data;
using System.Globalization;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    public class SqlServerDateTime2Converter : SqlServerDateTimeConverter
    {
        public override string ColumnDefinition => "DATETIME2";

        const string DateTimeFormat = "yyyyMMdd HH:mm:ss.fffffff";

        public override DbType DbType => DbType.DateTime2;

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
