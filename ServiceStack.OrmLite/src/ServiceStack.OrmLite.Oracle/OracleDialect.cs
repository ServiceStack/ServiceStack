using ServiceStack.OrmLite.Oracle;

namespace ServiceStack.OrmLite
{
    public class OracleDialect
    {
        public static IOrmLiteDialectProvider Provider => OracleOrmLiteDialectProvider.Instance;
        public static OracleOrmLiteDialectProvider Instance => OracleOrmLiteDialectProvider.Instance;
    }
}