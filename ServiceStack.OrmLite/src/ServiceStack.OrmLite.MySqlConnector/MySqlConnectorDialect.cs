using ServiceStack.OrmLite.MySql;

namespace ServiceStack.OrmLite
{
    public static class MySqlConnectorDialect
    {
        public static IOrmLiteDialectProvider Provider => MySqlConnectorDialectProvider.Instance;
    }
}