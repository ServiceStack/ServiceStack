using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters;

public class KingbaseSqlGuidConverter : GuidConverter
{
    public override string ColumnDefinition => "UUID";

    public override string ToQuotedString(Type fieldType, object value)
    {
        var guidValue = (Guid)value;
        return base.DialectProvider.GetQuotedValue(guidValue.ToString("N"), typeof(string));
    }
}