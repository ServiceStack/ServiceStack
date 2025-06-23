using Kdbndp;

namespace ServiceStack.OrmLite.Kingbase;

public static class KingbaseDialect
{
    public static IOrmLiteDialectProvider ProviderForMySql =>
        KingbaseDialectProvider.InstanceForMysql;

    public static KingbaseDialectProvider InstanceForMySql =>
        KingbaseDialectProvider.InstanceForMysql;

    public static KingbaseDialectProvider Create(DbMode dbMode) => new(dbMode);
}