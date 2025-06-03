#nullable enable

using System;
using ServiceStack.OrmLite.PostgreSQL;

namespace ServiceStack.OrmLite;

public static class PostgreSqlConfiguration
{
    public static PostgreSqlDialectProvider Configure(PostgreSqlDialectProvider dialect)
    {
        dialect.UseJson = true;
        return dialect;
    }

    public static PostgreSqlDialectProvider UsePostgres(this OrmLiteConfigOptions config, string? connectionString, Action<PostgreSqlDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(new PostgreSqlDialectProvider());
        configure?.Invoke(dialect);
        config.Init(connectionString, dialect);
        return dialect;
    }

    public static OrmLiteConfigurationBuilder AddPostgres(this OrmLiteConfigurationBuilder builder, 
        string namedConnection, string? connectionString, Action<PostgreSqlDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(new PostgreSqlDialectProvider());
        configure?.Invoke(dialect);
        builder.DbFactory.RegisterConnection(namedConnection, connectionString, dialect);
        return builder;
    }
}
