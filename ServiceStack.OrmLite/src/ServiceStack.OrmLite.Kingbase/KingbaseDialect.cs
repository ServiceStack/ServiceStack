namespace ServiceStack.OrmLite.Kingbase;

public static class KingbaseDialect
{
    public static IOrmLiteDialectProvider ProviderForMySqlConnector =>
        KingbaseDialectProvider.InstanceForMySqlConnector;

    public static KingbaseDialectProvider InstanceForMySqlConnector =>
        KingbaseDialectProvider.InstanceForMySqlConnector;

    public static KingbaseDialectProvider Create(IOrmLiteDialectProvider flavorProvider) => new(flavorProvider);
}