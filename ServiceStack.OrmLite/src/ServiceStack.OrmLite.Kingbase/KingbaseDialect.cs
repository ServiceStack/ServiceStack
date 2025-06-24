using System;
using Kdbndp;
using ServiceStack.OrmLite.Oracle;
using ServiceStack.OrmLite.PostgreSQL;

namespace ServiceStack.OrmLite.Kingbase;

public static class KingbaseDialect
{
    /// <summary>
    /// default postgreSQL dialect provider for KingbaseES, which is compatible with PostgreSQL.
    /// </summary>
    public static IOrmLiteDialectProvider Instance = Create(DbMode.Pg);

    /// <summary>
    /// default postgreSQL dialect provider for KingbaseES, which is compatible with PostgreSQL.
    /// </summary>
    public static KingbaseDialectProvider Provider = Create(DbMode.Pg);

    /// <summary>
    /// mysql connector
    /// </summary>
    public static IOrmLiteDialectProvider MySql = Create(DbMode.Mysql);

    /// <summary>
    /// oracle 11
    /// </summary>
    public static IOrmLiteDialectProvider Oracle = Create(DbMode.Oracle);

    // /// <summary>
    // /// sql server 2012
    // /// </summary>
    // public static IOrmLiteDialectProvider SqlServer = Create(DbMode.SqlServer);

    public static KingbaseDialectProvider Create(DbMode dbMode)
    {
        switch (dbMode)
        {
            case DbMode.Mysql:
                return new KingbaseDialectProvider(MySqlConnectorDialect.Provider);
            case DbMode.Pg:
                return new KingbaseDialectProvider(PostgreSqlDialectProvider.Instance);
            case DbMode.Oracle:
                return new KingbaseDialectProvider(Oracle11OrmLiteDialectProvider.Instance);
            // case DbMode.SqlServer:
            //     return new KingbaseDialectProvider(SqlServer2012Dialect.Provider);
            default:
                throw new NotSupportedException();
        }
    }

    public static KingbaseDialectProvider Create(IOrmLiteDialectProvider flavorProvider)
    {
        return new KingbaseDialectProvider(flavorProvider);
    }
}