using System;
using System.Data;

namespace ServiceStack.Data;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly Func<IDbConnection> connectionFactoryFn;

    public DbConnectionFactory(Func<IDbConnection> connectionFactoryFn)
    {
        this.connectionFactoryFn = connectionFactoryFn ?? throw new ArgumentNullException(nameof(connectionFactoryFn));
    }

    public IDbConnection OpenDbConnection()
    {
        var dbConn = CreateDbConnection() ?? throw new InvalidOperationException("connectionFactoryFn returned null IDbConnection.");
        dbConn.Open();
        return dbConn;
    }

    public IDbConnection CreateDbConnection()
    {
        return connectionFactoryFn();
    }
}
