using System;
using Kdbndp;
using ServiceStack.OrmLite.PostgreSQL;

namespace ServiceStack.OrmLite.Kingbase;

public static class KingbaseDialect
{
    public static IOrmLiteDialectProvider ProviderForMySql =>
        KingbaseDialectProvider.InstanceForMysql;

    public static KingbaseDialectProvider Create(DbMode dbMode)
    {
        switch (dbMode)
        {
            case DbMode.Mysql:
                return new KingbaseDialectProvider(MySqlConnectorDialect.Provider);
            case DbMode.Pg:
                return new KingbaseDialectProvider(PostgreSqlDialectProvider.Instance);
            default:
                throw new NotSupportedException();
        }
    }

    public static KingbaseDialectProvider Create(IOrmLiteDialectProvider flavorProvider)
    {
        return new KingbaseDialectProvider(flavorProvider);
    }
}