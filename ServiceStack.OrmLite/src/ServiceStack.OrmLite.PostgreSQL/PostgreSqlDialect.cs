using ServiceStack.OrmLite.PostgreSQL;

namespace ServiceStack.OrmLite;

public static class PostgreSqlDialect
{
    public static IOrmLiteDialectProvider Provider => PostgreSqlDialectProvider.Instance;
    public static PostgreSqlDialectProvider Instance => PostgreSqlDialectProvider.Instance;
    public static PostgreSqlDialectProvider Create() => new();
}