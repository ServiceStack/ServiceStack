using KdbndpTypes;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters.MySql
{
    public class KingbaseMySqlBoolConverter : BoolAsIntConverter, IKingbaseConverter
    {
        public override string ColumnDefinition => "BOOLEAN";
        public KdbndpDbType KdbndpDbType => KdbndpDbType.Boolean;
    }
}