using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters
{
    public class KingbaseSqlDateTimeConverter : DateTimeConverter
    {
        public override string ColumnDefinition => "timestamp";
    }
}