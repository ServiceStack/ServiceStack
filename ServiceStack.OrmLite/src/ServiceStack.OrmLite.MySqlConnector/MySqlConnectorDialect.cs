using ServiceStack.OrmLite.MySql;

namespace ServiceStack.OrmLite
{
    public static class MySqlConnectorDialect
    {
        public static IOrmLiteDialectProvider Provider => MySqlConnectorDialectProvider.Instance;
        public static MySqlConnectorDialectProvider Instance => MySqlConnectorDialectProvider.Instance;
    }
}