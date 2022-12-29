using ServiceStack.OrmLite.MySql;

namespace ServiceStack.OrmLite
{
    public static class MySqlDialect
    {
        public static IOrmLiteDialectProvider Provider => MySqlDialectProvider.Instance;
        public static MySqlDialectProvider Instance => MySqlDialectProvider.Instance;
    }
    
    public static class MySql55Dialect
    {
        public static IOrmLiteDialectProvider Provider => MySql55DialectProvider.Instance;
        public static MySql55DialectProvider Instance => MySql55DialectProvider.Instance;
    }
}