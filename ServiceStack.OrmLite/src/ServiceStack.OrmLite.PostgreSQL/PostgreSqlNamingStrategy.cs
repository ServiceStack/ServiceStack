using ServiceStack.Text;

namespace ServiceStack.OrmLite.PostgreSQL
{
    public class PostgreSqlNamingStrategy : OrmLiteNamingStrategyBase
    {
        public override string GetTableName(string name)
        {
            return name.ToLowercaseUnderscore();
        }

        public override string GetColumnName(string name)
        {
            return name.ToLowercaseUnderscore();
        }

        public override string GetSchemaName(string name)
        {
            return name.ToLowercaseUnderscore();
        }
    }
}