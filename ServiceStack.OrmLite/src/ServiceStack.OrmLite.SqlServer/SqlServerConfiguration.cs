#nullable enable
using System;
using ServiceStack.OrmLite.SqlServer;

namespace ServiceStack.OrmLite;

public static class SqlServerConfiguration
{
    public static SqlServerOrmLiteDialectProvider Configure(SqlServerOrmLiteDialectProvider dialect)
    {
        // New defaults for new Apps
        dialect.UseJson = true;
        return dialect;
    }

    /// <summary>
    /// Configure to use the latest version of SQL Server
    /// </summary>
    public static SqlServer2022OrmLiteDialectProvider UseSqlServer(this OrmLiteConfigOptions config, string? connectionString, Action<SqlServer2022OrmLiteDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = (SqlServer2022OrmLiteDialectProvider)Configure(new SqlServer2022OrmLiteDialectProvider());
        configure?.Invoke(dialect);
        config.Init(connectionString, dialect);
        return dialect;
    }

    /// <summary>
    /// Configure to use the latest version of SQL Server
    /// </summary>
    public static TVersion UseSqlServer<TVersion>(this OrmLiteConfigOptions config, string? connectionString, Action<TVersion>? configure=null)
        where TVersion : SqlServerOrmLiteDialectProvider, new()
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = (TVersion)Configure(new TVersion());
        configure?.Invoke(dialect);
        config.Init(connectionString, dialect);
        return dialect;
    }

    /// <summary>
    /// Add a connection to the latest version of SQL Server
    /// </summary>
    public static OrmLiteConfigurationBuilder AddSqlServer(this OrmLiteConfigurationBuilder builder, 
        string namedConnection, string? connectionString, Action<SqlServer2022OrmLiteDialectProvider>? configure=null)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = (SqlServer2022OrmLiteDialectProvider)Configure(new SqlServer2022OrmLiteDialectProvider());
        configure?.Invoke(dialect);
        builder.DbFactory.RegisterConnection(namedConnection, connectionString, dialect);
        return builder;
    }

    public static OrmLiteConfigurationBuilder AddSqlServer<TVersion>(this OrmLiteConfigurationBuilder builder, 
        string namedConnection, string? connectionString, Action<TVersion>? configure=null)
        where TVersion : SqlServerOrmLiteDialectProvider, new()
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        var dialect = (TVersion)Configure(new TVersion());
        configure?.Invoke(dialect);
        builder.DbFactory.RegisterConnection(namedConnection, connectionString, dialect);
        return builder;
    }
}
