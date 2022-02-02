using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    public class SqlServerRowVersionConverter : RowVersionConverter
    {
        public override string ColumnDefinition => "rowversion";
    }
}