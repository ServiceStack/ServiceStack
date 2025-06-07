using System;
using System.Data;

namespace ServiceStack.Data;

public class DbConnectionFactory(Func<IDbConnection> connectionFactoryFn) : IDbConnectionFactory
{
    public IDbConnection OpenDbConnection()
    {
        var dbConn = CreateDbConnection();
        dbConn.Open();
        return dbConn;
    }

    public IDbConnection CreateDbConnection()
    {
        return connectionFactoryFn();
    }
}
