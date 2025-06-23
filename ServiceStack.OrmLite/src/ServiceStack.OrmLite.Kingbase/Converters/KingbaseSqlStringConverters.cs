using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters
{
    public class KingbaseSqlStringConverter : StringConverter
    {
        public override string ColumnDefinition => "TEXT";

        //https://dba.stackexchange.com/questions/189876/size-limit-of-character-varying-postgresql
        public override int MaxVarCharLength => UseUnicode ? 10485760 / 2 : 10485760;

        public override string GetColumnDefinition(int? stringLength)
        {
            //PostgreSQL doesn't support NVARCHAR when UseUnicode = true so just use TEXT
            if (stringLength == null || stringLength == StringLengthAttribute.MaxText)
                return ColumnDefinition;

            return $"VARCHAR({stringLength.Value})";
        }
    }

    public class PostgreSqlCharArrayConverter : CharArrayConverter
    {
        public override string ColumnDefinition => "TEXT";

        public override string GetColumnDefinition(int? stringLength)
        {
            return ColumnDefinition;
        }
    }
}