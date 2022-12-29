using System;
using System.Data;
using System.Globalization;
using ServiceStack.Logging;
using ServiceStack.Text.Common;

namespace ServiceStack.OrmLite.Converters
{
    public class DateTimeConverter : OrmLiteConverter
    {
        public override string ColumnDefinition => "DATETIME";

        public override DbType DbType => DbType.DateTime;

        public DateTimeKind DateStyle { get; set; }

        public override string ToQuotedString(Type fieldType, object value)
        {
            var dateTime = (DateTime)value;
            return DateTimeFmt(dateTime, "yyyy-MM-dd HH:mm:ss.fff");
        }

        public virtual string DateTimeFmt(DateTime dateTime, string dateTimeFormat)
        {
            if (DateStyle == DateTimeKind.Utc && dateTime.Kind == DateTimeKind.Local)
                dateTime = dateTime.ToUniversalTime();

            if (DateStyle == DateTimeKind.Local && dateTime.Kind != DateTimeKind.Local)
                dateTime = dateTime.ToLocalTime();

            return DialectProvider.GetQuotedValue(dateTime.ToString(dateTimeFormat, CultureInfo.InvariantCulture), typeof(string));
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            var dateTime = (DateTime)value;
            if (DateStyle == DateTimeKind.Utc && dateTime.Kind == DateTimeKind.Local)
            {
                dateTime = dateTime.ToUniversalTime();
            }
            else if (DateStyle == DateTimeKind.Local && dateTime.Kind != DateTimeKind.Local)
            {
                dateTime = dateTime.Kind == DateTimeKind.Utc
                    ? dateTime.ToLocalTime()
                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();
            }

            return dateTime;
        }

        public override object FromDbValue(Type fieldType, object value)
        {                                                                                                                                                                                                                                                                                                 
            if (value is string strValue)
            {
                value = DateTimeSerializer.ParseShortestXsdDateTime(strValue);
            }

            return FromDbValue(value);
        }

        public virtual object FromDbValue(object value)
        {
            var dateTime = (DateTime)value;
            if (DateStyle == DateTimeKind.Utc)
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            if (DateStyle == DateTimeKind.Local && dateTime.Kind != DateTimeKind.Local)
            {
                dateTime = dateTime.Kind == DateTimeKind.Utc
                    ? dateTime.ToLocalTime()
                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
            }

            return dateTime;
        }
    }
}