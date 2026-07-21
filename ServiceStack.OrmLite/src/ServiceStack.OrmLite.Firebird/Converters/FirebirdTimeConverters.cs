#if NET6_0_OR_GREATER
using System;
using System.Data;
using System.Globalization;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Firebird.Converters
{
    // .NET DateOnly -> Firebird DATE. The core DateOnlyConverter derives from DateTimeConverter, so without this
    // override it emits "DATETIME" (not a Firebird type -> CreateTable fails, SQL error -607).
    public class FirebirdDateOnlyConverter : DateOnlyConverter
    {
        public override string ColumnDefinition => "DATE";

        public override string ToQuotedString(Type fieldType, object value)
            => "'" + ((DateOnly)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "'";
    }

    // .NET TimeOnly -> Firebird TIME. The core TimeOnlyConverter stores ticks in a BIGINT, which (a) isn't a TIME
    // column and (b) breaks against real TIME columns: on write the client can't put a long ticks value into a TIME
    // param, and on read ConvertNumber(long, TimeSpan) throws when the value comes back as a TimeSpan.
    // Fix: map TIME, bind a DateTime carrying the time-of-day (accepted by the FB client for TIME on write), and read
    // back a TimeSpan/DateTime (legacy long-ticks still handled via base).
    public class FirebirdTimeOnlyConverter : TimeOnlyConverter
    {
        public override string ColumnDefinition => "TIME";
        public override DbType DbType => DbType.Time;

        public override object ToDbValue(Type fieldType, object value)
            => new DateTime(1, 1, 1).Add(((TimeOnly)value).ToTimeSpan());

        public override object FromDbValue(Type fieldType, object value) => value switch
        {
            TimeSpan ts => TimeOnly.FromTimeSpan(ts),
            DateTime dt => TimeOnly.FromDateTime(dt),
            _ => base.FromDbValue(fieldType, value), // legacy: long ticks in a BIGINT column
        };

        public override string ToQuotedString(Type fieldType, object value)
            => "'" + ((TimeOnly)value).ToString("HH:mm:ss.ffff", CultureInfo.InvariantCulture) + "'";
    }
}
#endif
