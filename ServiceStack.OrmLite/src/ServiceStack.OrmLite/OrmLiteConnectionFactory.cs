using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// Allow for mocking and unit testing by providing non-disposing 
/// connection factory with injectable IDbCommand and IDbTransaction proxies
/// </summary>
public class OrmLiteConnectionFactory : IDbConnectionFactoryExtended
{
    public OrmLiteConnectionFactory()
        : this(null, null, true) { }

    public OrmLiteConnectionFactory(string connectionString)
        : this(connectionString, null, true) { }

    public OrmLiteConnectionFactory(string connectionString, IOrmLiteDialectProvider dialectProvider)
        : this(connectionString, dialectProvider, true) { }

    public OrmLiteConnectionFactory(string connectionString, IOrmLiteDialectProvider dialectProvider, bool setGlobalDialectProvider)
    {
        if (connectionString == "DataSource=:memory:")
            connectionString = ":memory:";
        ConnectionString = connectionString;
        AutoDisposeConnection = connectionString != ":memory:";
        this.DialectProvider = dialectProvider ?? OrmLiteConfig.DialectProvider;

        if (setGlobalDialectProvider && dialectProvider != null)
        {
            OrmLiteConfig.DialectProvider = dialectProvider;
        }

        this.ConnectionFilter = x => x;

        JsConfig.InitStatics();
    }

    public IOrmLiteDialectProvider DialectProvider { get; set; }

    public string ConnectionString { get; set; }

    public bool AutoDisposeConnection { get; set; }

    public Func<IDbConnection, IDbConnection> ConnectionFilter { get; set; }

    /// <summary>
    /// Force the IDbConnection to always return this IDbCommand
    /// </summary>
    public IDbCommand AlwaysReturnCommand { get; set; }

    /// <summary>
    /// Force the IDbConnection to always return this IDbTransaction
    /// </summary>
    public IDbTransaction AlwaysReturnTransaction { get; set; }

    public Action<OrmLiteConnection> OnDispose { get; set; }

    private OrmLiteConnection ormLiteConnection;
    private OrmLiteConnection OrmLiteConnection => ormLiteConnection ??= DialectProvider != null 
        ? DialectProvider.CreateOrmLiteConnection(this) 
        : new OrmLiteConnection(this);

    public virtual IDbConnection CreateDbConnection()
    {
        if (this.ConnectionString == null)
            throw new ArgumentNullException("ConnectionString", "ConnectionString must be set");

        var connection = AutoDisposeConnection
            ? DialectProvider.CreateOrmLiteConnection(this)
            : OrmLiteConnection.OpenShared();

        return connection;
    }

    public virtual IDbConnection Use(IDbConnection connection, IDbTransaction trans = null)
    {
        return new OrmLiteConnection(this, connection, trans);
    }

    public static IDbConnection CreateDbConnection(string namedConnection)
    {
        if (namedConnection == null)
            throw new ArgumentNullException(nameof(namedConnection));
            
        if (!NamedConnections.TryGetValue(namedConnection, out var factory))
            throw new KeyNotFoundException("No factory registered is named " + namedConnection);

        IDbConnection connection = factory.AutoDisposeConnection
            ? factory.DialectProvider.CreateOrmLiteConnection(factory, namedConnection)
            : factory.OrmLiteConnection.OpenShared();
        return connection;
    }

    public DbConnection CreateDbWithWriteLock(string namedConnection=null)
    {
        var factory = this;
        if (namedConnection != null && !NamedConnections.TryGetValue(namedConnection, out factory))
            throw new KeyNotFoundException("No factory registered is named " + namedConnection);

        return new SingleWriterDbConnection(factory, Locks.GetDbLock(namedConnection));
    }

    public virtual IDbConnection OpenDbConnection() => OpenOrDispose(CreateDbConnection());

    public virtual IDbConnection OpenDbConnection(Action<IDbConnection> configure) => 
        OpenOrDispose(CreateDbConnection(), configure);

    /// <summary>
    /// Opens the connection, disposing it if configure or Open() throws so it's not leaked
    /// </summary>
    private static IDbConnection OpenOrDispose(IDbConnection connection, Action<IDbConnection> configure = null)
    {
        try
        {
            configure?.Invoke(connection);
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static async Task<IDbConnection> OpenOrDisposeAsync(IDbConnection connection, IOrmLiteDialectProvider dialect,
        Action<IDbConnection> configure, CancellationToken token)
    {
        try
        {
            configure?.Invoke(connection);
            if (connection is OrmLiteConnection ormliteConn)
                await ormliteConn.OpenAsync(token).ConfigAwait();
            else
                await dialect.OpenAsync(connection, token).ConfigAwait();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public virtual async Task<IDbConnection> OpenDbConnectionAsync(CancellationToken token = default) =>
        await OpenOrDisposeAsync(CreateDbConnection(), DialectProvider, null, token).ConfigAwait();

    public virtual async Task<IDbConnection> OpenDbConnectionAsync(Action<IDbConnection> configure, CancellationToken token = default) =>
        await OpenOrDisposeAsync(CreateDbConnection(), DialectProvider, configure, token).ConfigAwait();

    public virtual async Task<IDbConnection> OpenDbConnectionAsync(string namedConnection, CancellationToken token = default) =>
        await OpenOrDisposeAsync(CreateDbConnection(namedConnection), GetNamedDialectProvider(namedConnection), null, token).ConfigAwait();

    public virtual async Task<IDbConnection> OpenDbConnectionAsync(string namedConnection, Action<IDbConnection> configure, CancellationToken token = default) =>
        await OpenOrDisposeAsync(CreateDbConnection(namedConnection), GetNamedDialectProvider(namedConnection), configure, token).ConfigAwait();

    private static IOrmLiteDialectProvider GetNamedDialectProvider(string namedConnection) =>
        NamedConnections.TryGetValue(namedConnection, out var factory) 
            ? factory.DialectProvider 
            : throw new KeyNotFoundException("No factory registered is named " + namedConnection);

    public virtual IDbConnection OpenDbConnectionString(string connectionString)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));

        var connection = DialectProvider.CreateOrmLiteConnection(this);
        connection.ConnectionString = connectionString;

        return OpenOrDispose(connection);
    }

    public virtual IDbConnection OpenDbConnectionString(string connectionString, Action<IDbConnection> configure)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));

        var connection = DialectProvider.CreateOrmLiteConnection(this);
        connection.ConnectionString = connectionString;

        return OpenOrDispose(connection, configure);
    }

    public virtual async Task<IDbConnection> OpenDbConnectionStringAsync(string connectionString, CancellationToken token = default)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));

        var connection = DialectProvider.CreateOrmLiteConnection(this);
        connection.ConnectionString = connectionString;

        return await OpenOrDisposeAsync(connection, DialectProvider, null, token).ConfigAwait();
    }

    public virtual async Task<IDbConnection> OpenDbConnectionStringAsync(string connectionString, Action<IDbConnection> configure, CancellationToken token = default)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));

        var connection = DialectProvider.CreateOrmLiteConnection(this);
        connection.ConnectionString = connectionString;

        return await OpenOrDisposeAsync(connection, DialectProvider, configure, token).ConfigAwait();
    }

    public virtual IDbConnection OpenDbConnectionString(string connectionString, string providerName)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        if (providerName == null)
            throw new ArgumentNullException(nameof(providerName));

        if (!DialectProviders.TryGetValue(providerName, out var dialectProvider))
            throw new ArgumentException($"{providerName} is not a registered DialectProvider");

        var dbFactory = new OrmLiteConnectionFactory(connectionString, dialectProvider, setGlobalDialectProvider:false);

        return dbFactory.OpenDbConnection();
    }

    public virtual IDbConnection OpenDbConnectionString(string connectionString, string providerName, Action<IDbConnection> configure)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        if (providerName == null)
            throw new ArgumentNullException(nameof(providerName));

        if (!DialectProviders.TryGetValue(providerName, out var dialectProvider))
            throw new ArgumentException($"{providerName} is not a registered DialectProvider");

        var dbFactory = new OrmLiteConnectionFactory(connectionString, dialectProvider, setGlobalDialectProvider:false);
        return dbFactory.OpenDbConnection(configure);
    }

    public virtual async Task<IDbConnection> OpenDbConnectionStringAsync(string connectionString, string providerName, CancellationToken token = default)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        if (providerName == null)
            throw new ArgumentNullException(nameof(providerName));

        if (!DialectProviders.TryGetValue(providerName, out var dialectProvider))
            throw new ArgumentException($"{providerName} is not a registered DialectProvider");

        var dbFactory = new OrmLiteConnectionFactory(connectionString, dialectProvider, setGlobalDialectProvider:false);

        return await dbFactory.OpenDbConnectionAsync(token).ConfigAwait();
    }
    public virtual async Task<IDbConnection> OpenDbConnectionStringAsync(string connectionString, string providerName, Action<IDbConnection> configure, CancellationToken token = default)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        if (providerName == null)
            throw new ArgumentNullException(nameof(providerName));

        if (!DialectProviders.TryGetValue(providerName, out var dialectProvider))
            throw new ArgumentException($"{providerName} is not a registered DialectProvider");

        var dbFactory = new OrmLiteConnectionFactory(connectionString, dialectProvider, setGlobalDialectProvider:false);

        return await dbFactory.OpenDbConnectionAsync(token).ConfigAwait();
    }

    public virtual IDbConnection OpenDbConnection(string namedConnection) => 
        OpenOrDispose(CreateDbConnection(namedConnection));

    public virtual IDbConnection OpenDbConnection(string namedConnection, Action<IDbConnection> configure) => 
        OpenOrDispose(CreateDbConnection(namedConnection), configure);

    private static Dictionary<string, IOrmLiteDialectProvider> dialectProviders;
    public static Dictionary<string, IOrmLiteDialectProvider> DialectProviders => dialectProviders ??= new Dictionary<string, IOrmLiteDialectProvider>();

    public virtual void RegisterDialectProvider(string providerName, IOrmLiteDialectProvider dialectProvider)
    {
        DialectProviders[providerName] = dialectProvider;
    }

    private static Dictionary<string, OrmLiteConnectionFactory> namedConnections;
    public static Dictionary<string, OrmLiteConnectionFactory> NamedConnections => namedConnections ??= new Dictionary<string, OrmLiteConnectionFactory>();

    public virtual void RegisterConnection(string namedConnection, string connectionString, IOrmLiteDialectProvider dialectProvider)
    {
        RegisterConnection(namedConnection, new OrmLiteConnectionFactory(connectionString, dialectProvider, setGlobalDialectProvider: false));
    }

    public virtual void RegisterConnection(string namedConnection, OrmLiteConnectionFactory connectionFactory)
    {
        NamedConnections[namedConnection] = connectionFactory;
        Locks.AddLock(namedConnection);
    }

    /// <summary>
    /// The read replica of this connection that OpenReadOnlyDbConnection() opens, e.g. a PostgreSQL standby, or null
    /// when it opens this connection
    /// </summary>
    public OrmLiteConnectionFactory ReadReplica { get; set; }

    /// <summary>
    /// Register a read replica of this connection, which uses its dialect. OpenReadOnlyDbConnection() opens it.
    /// </summary>
    public virtual void RegisterReadReplica(string connectionString) =>
        ReadReplica = CreateReadReplica(this, connectionString);

    /// <summary>
    /// Register a read replica of a named connection, which uses its dialect. OpenReadOnlyDbConnection(namedConnection)
    /// opens it.
    /// </summary>
    public virtual void RegisterReadReplica(string namedConnection, string connectionString)
    {
        var primary = GetNamedConnection(namedConnection);
        primary.ReadReplica = CreateReadReplica(primary, connectionString);
    }

    private static OrmLiteConnectionFactory CreateReadReplica(OrmLiteConnectionFactory primary, string connectionString) =>
        new(connectionString ?? throw new ArgumentNullException(nameof(connectionString)), primary.DialectProvider,
            setGlobalDialectProvider: false) {
            ConnectionFilter = primary.ConnectionFilter,
            OnDispose = primary.OnDispose,
        };

    private static OrmLiteConnectionFactory GetNamedConnection(string namedConnection)
    {
        if (namedConnection == null)
            throw new ArgumentNullException(nameof(namedConnection));
        return NamedConnections.TryGetValue(namedConnection, out var factory)
            ? factory
            : throw new KeyNotFoundException("No factory registered is named " + namedConnection);
    }

    /// <summary>
    /// Open a connection for queries that can read from a replica: its read replica when one is registered, otherwise
    /// this connection. A replica can be behind the primary, so read what was just written from the primary.
    /// </summary>
    public virtual IDbConnection OpenReadOnlyDbConnection() =>
        ReadReplica != null ? ReadReplica.OpenDbConnection() : OpenDbConnection();

    /// <summary>
    /// Open a connection for queries that can read from a replica: the named connection's read replica when one is
    /// registered, otherwise the named connection
    /// </summary>
    public virtual IDbConnection OpenReadOnlyDbConnection(string namedConnection) =>
        GetNamedConnection(namedConnection).ReadReplica is { } replica
            ? replica.OpenDbConnection()
            : OpenDbConnection(namedConnection);

    /// <summary>
    /// Open a connection for queries that can read from a replica: its read replica when one is registered, otherwise
    /// this connection
    /// </summary>
    public virtual Task<IDbConnection> OpenReadOnlyDbConnectionAsync(CancellationToken token = default) =>
        ReadReplica != null ? ReadReplica.OpenDbConnectionAsync(token) : OpenDbConnectionAsync(token);

    /// <summary>
    /// Open a connection for queries that can read from a replica: the named connection's read replica when one is
    /// registered, otherwise the named connection
    /// </summary>
    public virtual Task<IDbConnection> OpenReadOnlyDbConnectionAsync(string namedConnection, CancellationToken token = default) =>
        GetNamedConnection(namedConnection).ReadReplica is { } replica
            ? replica.OpenDbConnectionAsync(token)
            : OpenDbConnectionAsync(namedConnection, token);
}

public static class OrmLiteConnectionFactoryExtensions
{
    /// <summary>
    /// Alias for <see cref="OpenDbConnection(ServiceStack.Data.IDbConnectionFactory,string)"/>
    /// </summary>
    public static IDbConnection Open(this IDbConnectionFactory connectionFactory)
    {
        return connectionFactory.OpenDbConnection();
    }
    public static IDbConnection Open(this IDbConnectionFactory connectionFactory, Action<IDbConnection> configure)
    {
        var db = connectionFactory.CreateDbConnection();
        configure?.Invoke(db);
        db.Open();
        return db;
    }

    /// <summary>
    /// Alias for OpenDbConnectionAsync
    /// </summary>
    public static Task<IDbConnection> OpenDbConnectionAsync(this IDbConnectionFactory connectionFactory, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionAsync(token);
    }
    public static Task<IDbConnection> OpenDbConnectionAsync(this IDbConnectionFactory connectionFactory, Action<IDbConnection> configure, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionAsync(configure, token);
    }
    public static Task<IDbConnection> OpenAsync(this IDbConnectionFactory connectionFactory, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionAsync(token);
    }
    public static Task<IDbConnection> OpenAsync(this IDbConnectionFactory connectionFactory, Action<IDbConnection> configure, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionAsync(configure, token);
    }

    /// <summary>
    /// Alias for OpenDbConnectionAsync
    /// </summary>
    public static Task<IDbConnection> OpenAsync(this IDbConnectionFactory connectionFactory, string namedConnection, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionAsync(namedConnection, token);
    }
    public static Task<IDbConnection> OpenAsync(this IDbConnectionFactory connectionFactory, string namedConnection, Action<IDbConnection> configure, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionAsync(namedConnection, configure, token);
    }

    /// <summary>
    /// Alias for OpenDbConnection
    /// </summary>
    public static IDbConnection Open(this IDbConnectionFactory connectionFactory, string namedConnection)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnection(namedConnection);
    }
    public static IDbConnection Open(this IDbConnectionFactory connectionFactory, string namedConnection, Action<IDbConnection> configure)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnection(namedConnection, configure);
    }

    /// <summary>
    /// Alias for OpenDbConnection
    /// </summary>
    public static IDbConnection OpenDbConnection(this IDbConnectionFactory connectionFactory, Action<IDbConnection> configure)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnection(configure);
    }
    public static IDbConnection OpenDbConnection(this IDbConnectionFactory connectionFactory, string namedConnection)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnection(namedConnection);
    }
    public static IDbConnection OpenDbConnection(this IDbConnectionFactory connectionFactory, string namedConnection, Action<IDbConnection> configure)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnection(namedConnection, configure);
    }
    public static Task<IDbConnection> OpenDbConnectionAsync(this IDbConnectionFactory connectionFactory, string namedConnection, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionAsync(namedConnection, token);
    }
    public static Task<IDbConnection> OpenDbConnectionAsync(this IDbConnectionFactory connectionFactory, string namedConnection, Action<IDbConnection> configure, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionAsync(namedConnection, configure, token);
    }

    /// <summary>
    /// Alias for OpenDbConnection
    /// </summary>
    public static IDbConnection OpenDbConnectionString(this IDbConnectionFactory connectionFactory, string connectionString)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionString(connectionString);
    }
    public static IDbConnection OpenDbConnectionString(this IDbConnectionFactory connectionFactory, string connectionString, Action<IDbConnection> configure)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionString(connectionString,configure);
    }

    public static IDbConnection OpenDbConnectionString(this IDbConnectionFactory connectionFactory, string connectionString, string providerName)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionString(connectionString, providerName);
    }
    public static IDbConnection OpenDbConnectionString(this IDbConnectionFactory connectionFactory, string connectionString, string providerName, Action<IDbConnection> configure)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionString(connectionString, providerName, configure);
    }
    public static Task<IDbConnection> OpenDbConnectionStringAsync(this IDbConnectionFactory connectionFactory, string connectionString, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionStringAsync(connectionString, token);
    }
    public static Task<IDbConnection> OpenDbConnectionStringAsync(this IDbConnectionFactory connectionFactory, string connectionString, Action<IDbConnection> configure, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionStringAsync(connectionString, configure, token);
    }

    public static Task<IDbConnection> OpenDbConnectionStringAsync(this IDbConnectionFactory connectionFactory, string connectionString, string providerName, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionStringAsync(connectionString, providerName, token);
    }
    public static Task<IDbConnection> OpenDbConnectionStringAsync(this IDbConnectionFactory connectionFactory, string connectionString, string providerName, Action<IDbConnection> configure, CancellationToken token = default)
    {
        return ((OrmLiteConnectionFactory)connectionFactory).OpenDbConnectionStringAsync(connectionString, providerName, configure, token);
    }

    public static IOrmLiteDialectProvider GetDialectProvider(this IDbConnectionFactory connectionFactory, ConnectionInfo dbInfo)
    {
        return dbInfo != null
            ? GetDialectProvider(connectionFactory, providerName:dbInfo.ProviderName, namedConnection:dbInfo.NamedConnection)
            : ((OrmLiteConnectionFactory) connectionFactory).DialectProvider;
    }
        
    public static IOrmLiteDialectProvider GetDialectProvider(this IDbConnectionFactory connectionFactory,
        string providerName = null, string namedConnection = null)
    {
        var dbFactory = (OrmLiteConnectionFactory) connectionFactory;

        if (!string.IsNullOrEmpty(providerName))
            return OrmLiteConnectionFactory.DialectProviders.TryGetValue(providerName, out var provider)
                ? provider
                : throw new NotSupportedException($"Dialect provider is not registered '{providerName}'");
            
        if (!string.IsNullOrEmpty(namedConnection))
            return OrmLiteConnectionFactory.NamedConnections.TryGetValue(namedConnection, out var namedFactory)
                ? namedFactory.DialectProvider
                : throw new NotSupportedException($"Named connection is not registered '{namedConnection}'");
            
        return dbFactory.DialectProvider;
    }

    public static IDbConnection ToDbConnection(this IDbConnection db)
    {
        return db is IHasDbConnection hasDb
            ? hasDb.DbConnection.ToDbConnection()
            : db;
    }

    public static IDbCommand ToDbCommand(this IDbCommand dbCmd)
    {
        return dbCmd is IHasDbCommand hasDbCmd
            ? hasDbCmd.DbCommand.ToDbCommand()
            : dbCmd;
    }

    public static IDbTransaction ToDbTransaction(this IDbTransaction dbTrans)
    {
        return dbTrans is IHasDbTransaction hasDbTrans
            ? hasDbTrans.DbTransaction
            : dbTrans;
    }

    public static Guid GetConnectionId(this IDbConnection db) =>
        db is OrmLiteConnection conn ? conn.ConnectionId : Guid.Empty;

    public static Guid GetConnectionId(this IDbCommand dbCmd) =>
        dbCmd is OrmLiteCommand cmd ? cmd.ConnectionId : Guid.Empty;
        
    public static void RegisterConnection(this IDbConnectionFactory dbFactory, string namedConnection, string connectionString, IOrmLiteDialectProvider dialectProvider)
    {
        ((OrmLiteConnectionFactory)dbFactory).RegisterConnection(namedConnection, connectionString, dialectProvider);
    }

    public static void RegisterConnection(this IDbConnectionFactory dbFactory, string namedConnection, OrmLiteConnectionFactory connectionFactory)
    {
        ((OrmLiteConnectionFactory)dbFactory).RegisterConnection(namedConnection, connectionFactory);
    }
        
    public static IDbConnection OpenDbConnection(this IDbConnectionFactory dbFactory, ConnectionInfo connInfo, Action<IDbConnection> configure = null)
    {            
        if (dbFactory is IDbConnectionFactoryExtended dbFactoryExt && connInfo != null)
        {
            if (connInfo.ConnectionString != null)
            {
                return connInfo.ProviderName != null 
                    ? dbFactoryExt.OpenDbConnectionString(connInfo.ConnectionString, connInfo.ProviderName, configure) 
                    : dbFactoryExt.OpenDbConnectionString(connInfo.ConnectionString, configure);
            }

            if (connInfo.NamedConnection != null)
                return dbFactoryExt.OpenDbConnection(connInfo.NamedConnection, configure);
        }
        return dbFactory.Open(configure);
    }

    public static async Task<IDbConnection> OpenDbConnectionAsync(this IDbConnectionFactory dbFactory, ConnectionInfo connInfo, Action<IDbConnection> configure = null)
    {            
        if (dbFactory is IDbConnectionFactoryExtended dbFactoryExt && connInfo != null)
        {
            if (connInfo.ConnectionString != null)
            {
                return connInfo.ProviderName != null 
                    ? await dbFactoryExt.OpenDbConnectionStringAsync(connInfo.ConnectionString, connInfo.ProviderName, configure).ConfigAwait() 
                    : await dbFactoryExt.OpenDbConnectionStringAsync(connInfo.ConnectionString, configure).ConfigAwait();
            }

            if (connInfo.NamedConnection != null)
                return await dbFactoryExt.OpenDbConnectionAsync(connInfo.NamedConnection, configure).ConfigAwait();
        }
        return await dbFactory.OpenAsync(configure).ConfigAwait();
    }
        
    /// <summary>
    /// Register a read replica of the main connection, which OpenReadOnlyDbConnection() opens
    /// </summary>
    public static void RegisterReadReplica(this IDbConnectionFactory dbFactory, string connectionString) =>
        ((OrmLiteConnectionFactory)dbFactory).RegisterReadReplica(connectionString);

    /// <summary>
    /// Register a read replica of a named connection, which OpenReadOnlyDbConnection(namedConnection) opens
    /// </summary>
    public static void RegisterReadReplica(this IDbConnectionFactory dbFactory, string namedConnection, string connectionString) =>
        ((OrmLiteConnectionFactory)dbFactory).RegisterReadReplica(namedConnection, connectionString);

    /// <summary>
    /// Open a connection for queries that can read from a replica: the read replica when one is registered,
    /// otherwise the main connection, e.g:
    /// <para>using var db = dbFactory.OpenReadOnlyDbConnection();</para>
    /// </summary>
    public static IDbConnection OpenReadOnlyDbConnection(this IDbConnectionFactory dbFactory) =>
        dbFactory is OrmLiteConnectionFactory factory ? factory.OpenReadOnlyDbConnection() : dbFactory.OpenDbConnection();

    /// <summary>
    /// Open a connection for queries that can read from a replica: the named connection's read replica when one is
    /// registered, otherwise the named connection
    /// </summary>
    public static IDbConnection OpenReadOnlyDbConnection(this IDbConnectionFactory dbFactory, string namedConnection) =>
        dbFactory is OrmLiteConnectionFactory factory
            ? factory.OpenReadOnlyDbConnection(namedConnection)
            : dbFactory.OpenDbConnection(namedConnection);

    /// <summary>
    /// Open a connection for queries that can read from a replica: the read replica when one is registered,
    /// otherwise the main connection
    /// </summary>
    public static Task<IDbConnection> OpenReadOnlyDbConnectionAsync(this IDbConnectionFactory dbFactory,
        CancellationToken token = default) => dbFactory is OrmLiteConnectionFactory factory
        ? factory.OpenReadOnlyDbConnectionAsync(token)
        : dbFactory.OpenDbConnectionAsync(token);

    /// <summary>
    /// Open a connection for queries that can read from a replica: the named connection's read replica when one is
    /// registered, otherwise the named connection
    /// </summary>
    public static Task<IDbConnection> OpenReadOnlyDbConnectionAsync(this IDbConnectionFactory dbFactory,
        string namedConnection, CancellationToken token = default) => dbFactory is OrmLiteConnectionFactory factory
        ? factory.OpenReadOnlyDbConnectionAsync(namedConnection, token)
        : dbFactory.OpenDbConnectionAsync(namedConnection, token);

    public static Dictionary<string, OrmLiteConnectionFactory> GetNamedConnections(this IDbConnectionFactory dbFactory) => 
        OrmLiteConnectionFactory.NamedConnections;

    public static IDbConnection Use(this IDbConnectionFactory dbFactory, IDbConnection connection, IDbTransaction trans=null)
    {
        return ((OrmLiteConnectionFactory)dbFactory).Use(connection, trans);
    }
}