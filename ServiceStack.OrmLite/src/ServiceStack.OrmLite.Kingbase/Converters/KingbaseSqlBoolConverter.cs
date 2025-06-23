using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters
{
    public class KingbaseSqlBoolConverter : BoolConverter
    {
        public override string ColumnDefinition
        {
            get { return "BOOLEAN"; }
        }
    }
}