using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.PostgreSQL.Converters
{
    public class PostgreSqlBoolConverter : BoolConverter
    {
        public override string ColumnDefinition
        {
            get { return "BOOLEAN"; }
        }
    }
}