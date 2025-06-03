#nullable enable

using System;
using ServiceStack.OrmLite.MySql;

namespace ServiceStack.OrmLite;

public static class MySqlConnectorConfiguration
{
    public static MySqlConnectorDialectProvider Configure(MySqlConnectorDialectProvider dialect)
    {
        dialect.UseJson = true;
        return dialect;
    }

    public static MySqlConnectorDialectProvider UseMySqlConnector(this OrmLiteConfigOptions config, string? connectionString, Action<MySqlConnectorDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(new MySqlConnectorDialectProvider());
        configure?.Invoke(dialect);
        config.Init(connectionString, dialect);
        return dialect;
    }

    public static OrmLiteConfigurationBuilder AddMySqlConnector(this OrmLiteConfigurationBuilder builder, 
        string namedConnection, string? connectionString, Action<MySqlConnectorDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(new MySqlConnectorDialectProvider());
        configure?.Invoke(dialect);
        builder.DbFactory.RegisterConnection(namedConnection, connectionString, dialect);
        return builder;
    }
}
