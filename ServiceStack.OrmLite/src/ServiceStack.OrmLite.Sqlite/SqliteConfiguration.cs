#nullable enable
using System;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite;

public static class SqliteConfiguration
{
    public static SqliteOrmLiteDialectProviderBase Configure(SqliteOrmLiteDialectProviderBase dialect)
    {
        // New defaults for new Apps
        dialect.UseJson = true;
        dialect.UseUtc = true;
        dialect.EnableWal = true;
        dialect.EnableForeignKeys = true;
        return dialect;
    }

    public static SqliteOrmLiteDialectProviderBase UseSqlite(this OrmLiteConfigOptions config, string? connectionString, Action<SqliteOrmLiteDialectProviderBase>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(SqliteDialect.Create());
        configure?.Invoke(dialect);
        config.Init(connectionString, dialect);
        return dialect;
    }

    public static OrmLiteConfigurationBuilder AddSqlite(this OrmLiteConfigurationBuilder builder, 
        string namedConnection, string? connectionString, Action<SqliteOrmLiteDialectProviderBase>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(SqliteDialect.Create());
        configure?.Invoke(dialect);
        builder.DbFactory.RegisterConnection(namedConnection, connectionString, dialect);
        return builder;
    }
}
