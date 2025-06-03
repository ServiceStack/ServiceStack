#nullable enable
using System;
using ServiceStack.OrmLite.Firebird;

namespace ServiceStack.OrmLite;

public static class FirebirdConfiguration
{
    public static Firebird4OrmLiteDialectProvider Configure(Firebird4OrmLiteDialectProvider dialect)
    {
        // New defaults for new Apps
        dialect.UseJson = true;
        return dialect;
    }

    public static Firebird4OrmLiteDialectProvider UseFirebird(this OrmLiteConfigOptions config, string? connectionString, Action<Firebird4OrmLiteDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(new Firebird4OrmLiteDialectProvider());
        configure?.Invoke(dialect);
        config.Init(connectionString, dialect);
        return dialect;
    }

    public static OrmLiteConfigurationBuilder AddFirebird(this OrmLiteConfigurationBuilder builder, 
        string namedConnection, string? connectionString, Action<Firebird4OrmLiteDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = Configure(new Firebird4OrmLiteDialectProvider());
        configure?.Invoke(dialect);
        builder.DbFactory.RegisterConnection(namedConnection, connectionString, dialect);
        return builder;
    }
}
