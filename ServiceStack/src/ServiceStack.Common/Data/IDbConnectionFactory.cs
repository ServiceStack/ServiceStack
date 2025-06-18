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
    Task<IDbConnection> OpenDbConnectionAsync(CancellationToken token = default);
    
    IDbConnection OpenDbConnection(string namedConnection);
    Task<IDbConnection> OpenDbConnectionAsync(string namedConnection, CancellationToken token = default);

    IDbConnection OpenDbConnectionString(string connectionString);
    Task<IDbConnection> OpenDbConnectionStringAsync(string connectionString, CancellationToken token = default);
    
    IDbConnection OpenDbConnectionString(string connectionString, string providerName);

    Task<IDbConnection> OpenDbConnectionStringAsync(string connectionString, string providerName, CancellationToken token = default);

    IDbConnection Use(IDbConnection connection, IDbTransaction trans=null);
}