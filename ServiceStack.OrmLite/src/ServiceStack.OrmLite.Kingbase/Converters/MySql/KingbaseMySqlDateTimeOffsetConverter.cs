using System;
using ServiceStack.OrmLite.MySql.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters.MySql;

public sealed class KingbaseMySqlDateTimeOffsetConverter : MySqlDateTimeOffsetConverter
{
    public override object ToDbValue(Type fieldType, object value)
    {
        var convertedValue = DialectProvider.StringSerializer.SerializeToString(value);
        return convertedValue;
    }
}