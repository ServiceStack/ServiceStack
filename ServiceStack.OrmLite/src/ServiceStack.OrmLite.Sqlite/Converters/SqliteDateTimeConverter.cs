using System;
using System.Data;
using System.Globalization;
using ServiceStack.OrmLite.Converters;
using ServiceStack.Text.Common;

namespace ServiceStack.OrmLite.Sqlite.Converters
{
    public class SqliteNativeDateTimeConverter : DateTimeConverter
    {
        public override string ColumnDefinition => "VARCHAR(8000)";

        public override DbType DbType => DbType.DateTime;

        public override string ToQuotedString(Type fieldType, object value)
        {
            var dateTime = (DateTime)value;
            if (DateStyle == DateTimeKind.Unspecified)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
            }
            else if (DateStyle == DateTimeKind.Local && dateTime.Kind == DateTimeKind.Unspecified)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();
            }
            else if (DateStyle == DateTimeKind.Utc)
            {
                dateTime = dateTime.Kind == DateTimeKind.Local
                    ? DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime()
                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

                return DialectProvider.GetQuotedValue(dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), typeof(string));
            }

            var dateStr = DateTimeSerializer.ToLocalXsdDateTimeString(dateTime);
            dateStr = dateStr.Replace("T", " ");
            const int tzPos = 6; //"-00:00".Length;
            var timeZoneMod = dateStr.Substring(dateStr.Length - tzPos, 1);
            if (timeZoneMod == "+" || timeZoneMod == "-")
            {
                dateStr = dateStr.Substring(0, dateStr.Length - tzPos);
            }

            return DialectProvider.GetQuotedValue(dateStr, typeof(string));
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            var dateTime = (DateTime)value;

            if (DateStyle == DateTimeKind.Unspecified)
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);

            return base.FromDbValue(dateTime);
        }

        public override object GetValue(IDataReader reader, int columnIndex, object[] values)
        {
            try
            {
                if (values != null && values[columnIndex] == DBNull.Value)
                    return null;

                return reader.GetDateTime(columnIndex);
            }
            catch (Exception ex)
            {
                var value = base.GetValue(reader, columnIndex, values);
                if (value == null)
                    return null;

                if (!(value is string dateStr))
                    throw new Exception($"Converting from {value.GetType().Name} to DateTime is not supported");

                Log.Warn("Error reading string as DateTime in Sqlite: " + dateStr, ex);
                return DateTime.Parse(dateStr);
            }
        }
    }

    /// <summary>
    /// New behavior from using System.Data.SQLite.Core
    /// </summary>
    public class SqliteCoreDateTimeConverter : SqliteNativeDateTimeConverter
    {
        public override object ToDbValue(Type fieldType, object value)
        {
            var dateTime = (DateTime)value;
            if (DateStyle == DateTimeKind.Utc)
            {
                if (dateTime.Kind == DateTimeKind.Local)
                {
                    dateTime = dateTime.ToUniversalTime();
                }
                else if (dateTime.Kind == DateTimeKind.Unspecified)
                {
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                }
            }
            else if (DateStyle == DateTimeKind.Local && dateTime.Kind != DateTimeKind.Local)
            {
                dateTime = dateTime.Kind == DateTimeKind.Utc
                    ? dateTime.ToLocalTime()
                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();
            }
            else if (DateStyle == DateTimeKind.Unspecified && dateTime.Kind == DateTimeKind.Utc)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
            }

            return dateTime;
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            var dateTime = (DateTime)value;

            if (DateStyle == DateTimeKind.Utc)
            {
#if NETCORE
                //.NET Core returns correct Local time but as Unspecified so change to Local and Convert to UTC
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
#else
                dateTime = dateTime.ToUniversalTime();
#endif
            }

            if (DateStyle == DateTimeKind.Local && dateTime.Kind != DateTimeKind.Local)
            {
                dateTime = dateTime.Kind == DateTimeKind.Utc
                    ? dateTime.ToLocalTime()
                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
            }

            return dateTime;
        }

        public override object GetValue(IDataReader reader, int columnIndex, object[] values)
        {
            var ret = base.GetValue(reader, columnIndex, values);
            return ret;
        }
    }

    public class SqliteDataDateTimeConverter : SqliteCoreDateTimeConverter
    {
        public override object FromDbValue(Type fieldType, object value)
        {
            var dateTime = (DateTime)value;

            if (DateStyle == DateTimeKind.Utc)
            {
                //.NET Core returns correct Local time but as Unspecified so change to Local and Convert to UTC
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc); // don't convert
            }

            if (DateStyle == DateTimeKind.Local && dateTime.Kind != DateTimeKind.Local)
            {
                dateTime = dateTime.Kind == DateTimeKind.Utc
                    ? dateTime.ToLocalTime()
                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
            }

            return dateTime;
        }
    }

    public class SqliteWindowsDateTimeConverter : SqliteNativeDateTimeConverter
    {
        public override object FromDbValue(Type fieldType, object value)
        {
            var dateTime = (DateTime)value;

            if (DateStyle == DateTimeKind.Utc)
                dateTime = dateTime.ToUniversalTime();

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