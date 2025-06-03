using ServiceStack.OrmLite.Firebird;

namespace ServiceStack.OrmLite;

public static class FirebirdDialect
{
    public static IOrmLiteDialectProvider Provider => FirebirdOrmLiteDialectProvider.Instance;
    public static FirebirdOrmLiteDialectProvider Instance => FirebirdOrmLiteDialectProvider.Instance;
}

public static class Firebird4Dialect
{
    public static IOrmLiteDialectProvider Provider => Firebird4OrmLiteDialectProvider.Instance;
    public static Firebird4OrmLiteDialectProvider Instance => Firebird4OrmLiteDialectProvider.Instance;
}