using System;
using ServiceStack.OrmLite.Converters;
using MySql.Data.Types;

namespace ServiceStack.OrmLite.MySql.Converters
{
    public class MySqlDateTimeConverter : MySqlDateTimeConverterBase
    {
        public override object FromDbValue(object value)
        {
            // TODO throws error if connection string option not set - https://stackoverflow.com/questions/5754822/unable-to-convert-mysql-date-time-value-to-system-datetime
            if (value is MySqlDateTime time)
            {
                return time.GetDateTime();
            }
            return base.FromDbValue(value);
        }
    }

    public class MySql55DateTimeConverter : MySqlDateTimeConverter
    {
        /// <summary>
        /// CURRENT_TIMESTAMP as a default for DATETIME type is only available in 10.x. If you're using 5.5, it should a TIMESTAMP column
        /// </summary>
        public override string ColumnDefinition => "TIMESTAMP";
    }
}