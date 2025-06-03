#nullable enable
using System;
using ServiceStack.OrmLite.Oracle;

namespace ServiceStack.OrmLite;

public static class OracleConfig
{
    public static Oracle18OrmLiteDialectProvider Configure(Oracle18OrmLiteDialectProvider dialect)
    {
        // New defaults for new Apps
        dialect.UseJson = true;
        return dialect;
    }

    public static Oracle18OrmLiteDialectProvider UseOracle(this OrmLiteConfigOptions config, string? connectionString, Action<Oracle18OrmLiteDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(new Oracle18OrmLiteDialectProvider());
        configure?.Invoke(dialect);
        config.Init(connectionString, dialect);
        return dialect;
    }

    public static OrmLiteConfigurationBuilder AddOracle(this OrmLiteConfigurationBuilder builder, 
        string namedConnection, string? connectionString, Action<Oracle18OrmLiteDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(new Oracle18OrmLiteDialectProvider());
        configure?.Invoke(dialect);
        builder.DbFactory.RegisterConnection(namedConnection, connectionString, dialect);
        return builder;
    }
}
