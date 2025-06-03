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
}
