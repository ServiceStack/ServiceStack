using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.MySql.Converters;

public class MySqlDateTimeOffsetConverter : DateTimeOffsetConverter
{
    public override string ColumnDefinition => "VARCHAR(255)";
    public override DbType DbType => DbType.String;
}