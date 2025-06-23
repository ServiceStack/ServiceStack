using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters.MySql
{
    public class KingbaseMySqlBoolConverter : BoolAsIntConverter
    {
        public override string ColumnDefinition => "BOOLEAN";
    }
}