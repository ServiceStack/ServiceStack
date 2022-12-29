using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.PostgreSQL.Converters
{
    public class PostgreSqlDateTimeConverter : DateTimeConverter
    {
        public override string ColumnDefinition => "timestamp";
    }
}