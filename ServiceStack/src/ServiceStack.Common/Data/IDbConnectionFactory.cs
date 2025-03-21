﻿using System.Data;

namespace ServiceStack.Data;

public interface IDbConnectionFactory
{
    IDbConnection OpenDbConnection();
    IDbConnection CreateDbConnection();
}

public interface IDbConnectionFactoryExtended : IDbConnectionFactory
{
    IDbConnection OpenDbConnection(string namedConnection);

    IDbConnection OpenDbConnectionString(string connectionString);
    IDbConnection OpenDbConnectionString(string connectionString, string providerName);

    IDbConnection Use(IDbConnection connection, IDbTransaction trans=null);
}