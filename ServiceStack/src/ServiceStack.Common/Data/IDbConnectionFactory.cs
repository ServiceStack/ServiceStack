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
/// <summary>
/// A connection factory with read replicas, which opens a connection's read replica for queries that can read from
/// one, or the connection itself when it doesn't have one, e.g. OrmLiteConnectionFactory
/// </summary>
public interface IDbReadOnlyConnectionFactory : IDbConnectionFactory
{
    IDbConnection OpenReadOnlyDbConnection(Action<IDbConnection> configure);
    IDbConnection OpenReadOnlyDbConnection(string namedConnection, Action<IDbConnection> configure);
    Task<IDbConnection> OpenReadOnlyDbConnectionAsync(Action<IDbConnection> configure, CancellationToken token = default);
    Task<IDbConnection> OpenReadOnlyDbConnectionAsync(string namedConnection, Action<IDbConnection> configure, CancellationToken token = default);
}
