using System;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.SqlServer.Converters;

public class SqlServerGuidConverter : GuidConverter
{
    public override string ColumnDefinition => "UniqueIdentifier";

    public override string ToQuotedString(Type fieldType, object value)
    {
        var guidValue = (Guid)value;
        return $"CAST('{guidValue}' AS UNIQUEIDENTIFIER)";
    }
}
