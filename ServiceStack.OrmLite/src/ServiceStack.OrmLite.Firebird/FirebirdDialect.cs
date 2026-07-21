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

public static class Firebird5Dialect
{
    public static IOrmLiteDialectProvider Provider => Firebird5OrmLiteDialectProvider.Instance;
    public static Firebird5OrmLiteDialectProvider Instance => Firebird5OrmLiteDialectProvider.Instance;
}