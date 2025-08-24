using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.MySql.Converters;

public abstract class MySqlDateTimeConverterBase : DateTimeConverter
{
    public int Precision { get; set; } = 0;

    public override string ColumnDefinition => Precision == 0
        ? "DATETIME"
        : $"DATETIME({Precision})";

    public override string ToQuotedString(Type fieldType, object value)
    {
        /*
         * ms not contained in format. MySql ignores ms part anyway
         * for more details see: http://dev.mysql.com/doc/refman/5.1/en/datetime.html
         */
        var dateTime = (DateTime)value;
        var suffix = Precision > 0
            ? "." + new string('f', Precision)
            : "";
        return DateTimeFmt(dateTime, "yyyy-MM-dd HH:mm:ss" + suffix);
    }

}