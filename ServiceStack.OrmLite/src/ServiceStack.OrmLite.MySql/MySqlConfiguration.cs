#nullable enable

using System;
using ServiceStack.OrmLite.MySql;

namespace ServiceStack.OrmLite;

public static class MySqlConfiguration
{
    public static MySqlDialectProvider Configure(MySqlDialectProvider dialect)
    {
        dialect.UseJson = true;
        return dialect;
    }

    public static MySqlDialectProvider UseMySql(this OrmLiteConfigOptions config, string? connectionString, Action<MySqlDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(new MySqlDialectProvider());
        configure?.Invoke(dialect);
        config.Init(connectionString, dialect);
        return dialect;
    }

    public static OrmLiteConfigurationBuilder AddMySql(this OrmLiteConfigurationBuilder builder, 
        string namedConnection, string? connectionString, Action<MySqlDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(new MySqlDialectProvider());
        configure?.Invoke(dialect);
        builder.DbFactory.RegisterConnection(namedConnection, connectionString, dialect);
        return builder;
    }
}
