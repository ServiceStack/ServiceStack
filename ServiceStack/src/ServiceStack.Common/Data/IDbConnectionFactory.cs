using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Data;

public interface IDbConnectionFactory
{
    IDbConnection OpenDbConnection();
    IDbConnection CreateDbConnection();
}

public interface IDbConnectionFactoryExtended : IDbConnectionFactory
{
    IDbConnection OpenDbConnection(Action<IDbConnection> configure);
    Task<IDbConnection> OpenDbConnectionAsync(CancellationToken token = default);
    Task<IDbConnection> OpenDbConnectionAsync(Action<IDbConnection> configure, CancellationToken token = default);
    
    IDbConnection OpenDbConnection(string namedConnection);
    IDbConnection OpenDbConnection(string namedConnection, Action<IDbConnection> configure);
    Task<IDbConnection> OpenDbConnectionAsync(string namedConnection, CancellationToken token = default);
    Task<IDbConnection> OpenDbConnectionAsync(string namedConnection, Action<IDbConnection> configure, CancellationToken token = default);

    IDbConnection OpenDbConnectionString(string connectionString);
    IDbConnection OpenDbConnectionString(string connectionString, Action<IDbConnection> configure);
    Task<IDbConnection> OpenDbConnectionStringAsync(string connectionString, CancellationToken token = default);
    Task<IDbConnection> OpenDbConnectionStringAsync(string connectionString, Action<IDbConnection> configure, CancellationToken token = default);
    
    IDbConnection OpenDbConnectionString(string connectionString, string providerName);
    IDbConnection OpenDbConnectionString(string connectionString, string providerName, Action<IDbConnection> configure);

    Task<IDbConnection> OpenDbConnectionStringAsync(string connectionString, string providerName, CancellationToken token = default);
    Task<IDbConnection> OpenDbConnectionStringAsync(string connectionString, string providerName, Action<IDbConnection> configure, CancellationToken token = default);

    IDbConnection Use(IDbConnection connection, IDbTransaction trans=null);
}