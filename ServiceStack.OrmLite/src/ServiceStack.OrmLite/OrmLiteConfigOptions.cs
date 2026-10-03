#nullable enable

using System;
using System.Collections.Generic;
using ServiceStack.Data;

namespace ServiceStack.OrmLite;

public class OrmLiteConfigOptions
{
    public IDbConnectionFactory? DbFactory { private set; get; }
    
    public void Init(string connectionString, IOrmLiteDialectProvider dialectProvider)
    {
        if (DbFactory != null)
            throw new InvalidOperationException("DbFactory is already set");
        DbFactory = new OrmLiteConnectionFactory(connectionString, dialectProvider);
    } 
}

public class OrmLiteConfigurationBuilder(IDbConnectionFactory dbFactory)
{
    public IDbConnectionFactory DbFactory { get; } = dbFactory;
    
    public OrmLiteConfigurationBuilder AddConnection(string name, string? connectionString, IOrmLiteDialectProvider? dialectProvider = null)
    {
        DbFactory.RegisterConnection(name, connectionString, dialectProvider ?? OrmLiteConfig.DialectProvider);
        return this;
    }

    /// <summary>
    /// Register a read replica of the main connection, e.g. a PostgreSQL standby, which uses its dialect and
    /// dbFactory.OpenReadOnlyDbConnection() opens
    /// </summary>
    public OrmLiteConfigurationBuilder AddReadReplica(string? connectionString)
    {
        DbFactory.RegisterReadReplica(connectionString ?? throw new ArgumentNullException(nameof(connectionString)));
        return this;
    }

    /// <summary>
    /// Register a read replica of a named connection, which uses its dialect and
    /// dbFactory.OpenReadOnlyDbConnection(namedConnection) opens
    /// </summary>
    public OrmLiteConfigurationBuilder AddReadReplica(string namedConnection, string? connectionString)
    {
        DbFactory.RegisterReadReplica(namedConnection,
            connectionString ?? throw new ArgumentNullException(nameof(connectionString)));
        return this;
    }
}
