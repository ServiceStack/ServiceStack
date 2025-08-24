using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.MySql.Converters;

public class MySqlBoolConverter : BoolAsIntConverter
{
    public override string ColumnDefinition => "tinyint(1)";
}