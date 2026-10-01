#nullable enable
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.Logging;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// Wrapper IDbConnection class to allow for connection sharing, mocking, etc.
/// </summary>
public class OrmLiteConnection
    : IDbConnection, IHasDbConnection, IHasDbTransaction, ISetDbTransaction, IHasDialectProvider, IHasTag
{
    public readonly OrmLiteConnectionFactory Factory;
    public string? Tag { get; set; }
    private IDbTransaction? transaction;
    public IDbTransaction? Transaction
    {
        get => source != null ? source.Transaction : transaction;
        set
        {
            if (source != null)
                source.Transaction = value;
            else
                transaction = value;
        }
    }
    public IDbTransaction? DbTransaction => Transaction;
    private IDbConnection? dbConnection;

    /// <summary>
    /// The connection this is an unfiltered view of, when created by WithoutFilters(), which shares its underlying
    /// connection and transaction
    /// </summary>
    private OrmLiteConnection? source;

    public IOrmLiteDialectProvider DialectProvider { get; set; }
    public string? LastCommandText { get; set; }
    public IDbCommand? LastCommand { get; set; }
    public string? NamedConnection { get; set; }

    /// <summary>
    /// Gets or sets the wait time before terminating the attempt to execute a command and generating an error(in seconds).
    /// </summary>
    public int? CommandTimeout { get; set; }

    public Guid ConnectionId { get; set; }
    public object? WriteLock { get; set; }

    /// <summary>
    /// Mandatory filters and write rules applied to statements on this connection, see db.EnsureFilter()
    /// </summary>
    public OrmLiteConnectionFilters Filters { get; internal set; } = OrmLiteConnectionFilters.Empty;

    private System.Collections.Generic.Dictionary<string, object?>? items;

    /// <summary>
    /// State to keep with this connection for as long as it's open, e.g. the tenant it's confined to, see db.SetItem().
    /// Shared with the connections returned by WithoutFilters(), which are the same connection.
    /// </summary>
    public System.Collections.Generic.Dictionary<string, object?> Items => source != null
        ? source.Items
        : items ??= new();

    /// <summary>
    /// Whether this connection was returned by WithoutFilters()
    /// </summary>
    public bool IsWithoutFilters => source != null;

    public OrmLiteConnection(OrmLiteConnectionFactory factory)
    {
        this.Factory = factory;
        this.DialectProvider = factory.DialectProvider;
    }

    public OrmLiteConnection(OrmLiteConnectionFactory factory, IDbConnection connection, IDbTransaction? transaction = null)
        : this(factory)
    {
        this.dbConnection = connection;
        if (transaction != null)
        {
            Transaction = transaction;
        }
    }

    public IDbConnection DbConnection => source != null
        ? source.DbConnection
        : dbConnection ??= ConnectionString.ToDbConnection(Factory.DialectProvider);

    /// <summary>
    /// A connection over the same underlying connection and transaction, without this connection's filters and
    /// rules. Disposing or closing it doesn't close the underlying connection.
    /// </summary>
    internal OrmLiteConnection CreateWithoutFilters()
    {
        var owner = source ?? this;
        return new OrmLiteConnection(Factory) {
            source = owner,
            DialectProvider = owner.DialectProvider,
            Tag = owner.Tag,
            NamedConnection = owner.NamedConnection,
            CommandTimeout = owner.CommandTimeout,
            ConnectionId = owner.ConnectionId,
            WriteLock = owner.WriteLock,
            connectionString = owner.connectionString,
        };
    }

    /// <summary>
    /// The number of times a shared connection, e.g. SQLite :memory:, is open
    /// </summary>
    private int sharedOpens;

    /// <summary>
    /// Open a shared connection which is returned for every open, e.g. SQLite :memory:
    /// </summary>
    internal OrmLiteConnection OpenShared()
    {
        Interlocked.Increment(ref sharedOpens);
        return this;
    }

    public void Dispose()
    {
        if (source != null)
            return; // the underlying connection is owned by the connection it was created from

        Factory.OnDispose?.Invoke(this);
        if (!Factory.AutoDisposeConnection)
        {
            // Filters and items of a shared connection are scoped to its outermost open so they aren't used by the next open
            if (Interlocked.Decrement(ref sharedOpens) <= 0)
            {
                Interlocked.Exchange(ref sharedOpens, 0);
                Filters = OrmLiteConnectionFilters.Empty;
                items = null;
            }
            return;
        }

        if (dbConnection == null)
        {
            return;
        }

        try
        {
            DialectProvider.OnDisposeConnection?.Invoke(this);
            dbConnection?.Dispose();
        }
        catch (Exception e)
        {
            LogManager.GetLogger(GetType()).Error("Failed to Dispose()", e);
        }
        dbConnection = null;
    }

    public IDbTransaction BeginTransaction()
    {
        if (Factory.AlwaysReturnTransaction != null)
            return Factory.AlwaysReturnTransaction;

        return DbConnection.BeginTransaction();
    }

    public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        if (Factory.AlwaysReturnTransaction != null)
            return Factory.AlwaysReturnTransaction;

        return DbConnection.BeginTransaction(isolationLevel);
    }

    public void Close()
    {
        if (source != null)
            return;

        if (dbConnection == null)
        {
            LogManager.GetLogger(GetType()).WarnFormat("No dbConnection to Close()");
            return;
        }

        var id = Diagnostics.OrmLite.WriteConnectionCloseBefore(this);
        var connectionId = dbConnection.GetConnectionId();
        Exception? e = null;
        try
        {
            DialectProvider.OnDisposeConnection?.Invoke(this);
            dbConnection.Close();
        }
        catch (Exception ex)
        {
            e = ex;
            throw;
        }
        finally
        {
            if (e != null)
                Diagnostics.OrmLite.WriteConnectionCloseError(id, connectionId, this, e);
            else
                Diagnostics.OrmLite.WriteConnectionCloseAfter(id, connectionId, this);
        }
    }

    public void ChangeDatabase(string databaseName)
    {
        DbConnection.ChangeDatabase(databaseName);
    }

    public IDbCommand CreateCommand()
    {
        if (Factory.AlwaysReturnCommand != null)
            return Factory.AlwaysReturnCommand;

        var cmd = DbConnection.CreateCommand();

        return cmd;
    }

    public void Open()
    {
        if (source != null)
        {
            source.Open();
            return;
        }

        var dbConn = DbConnection;
        if (dbConn.State == ConnectionState.Broken)
            dbConn.Close();

        if (dbConn.State == ConnectionState.Closed)
        {
            var id = Diagnostics.OrmLite.WriteConnectionOpenBefore(this);
            Exception? e = null;
            try
            {
                dbConn.Open();
                //so the internal connection is wrapped for example by miniprofiler
                if (Factory.ConnectionFilter != null)
                    dbConnection = Factory.ConnectionFilter(dbConn);

                DialectProvider.InitConnection(this);
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                if (e != null)
                    Diagnostics.OrmLite.WriteConnectionOpenError(id, this, e);
                else
                    Diagnostics.OrmLite.WriteConnectionOpenAfter(id, this);
            }
        }
    }

    public async Task OpenAsync(CancellationToken token = default)
    {
        if (source != null)
        {
            await source.OpenAsync(token).ConfigAwait();
            return;
        }

        var dbConn = DbConnection;
        if (dbConn.State == ConnectionState.Broken)
            dbConn.Close();

        if (dbConn.State == ConnectionState.Closed)
        {
            var id = Diagnostics.OrmLite.WriteConnectionOpenBefore(this);
            Exception? e = null;
            try
            {
                await DialectProvider.OpenAsync(dbConn, token).ConfigAwait();
                //so the internal connection is wrapped for example by miniprofiler
                if (Factory.ConnectionFilter != null)
                    dbConnection = Factory.ConnectionFilter(dbConn);

                DialectProvider.InitConnection(this);
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                if (e != null)
                    Diagnostics.OrmLite.WriteConnectionOpenError(id, this, e);
                else
                    Diagnostics.OrmLite.WriteConnectionOpenAfter(id, this);
            }
        }
    }

    private string? connectionString;

    public string? ConnectionString
    {
        get => connectionString ?? Factory.ConnectionString;
        set => connectionString = value;
    }

    public int ConnectionTimeout => DbConnection.ConnectionTimeout;

    public string Database => DbConnection.Database;

    public ConnectionState State => DbConnection.State;

    public bool AutoDisposeConnection { get; set; }

    public static explicit operator DbConnection(OrmLiteConnection dbConn)
    {
        return (DbConnection)dbConn.DbConnection;
    }
}

internal interface ISetDbTransaction
{
    IDbTransaction Transaction { get; set; }
}

public static class OrmLiteConnectionUtils
{
    public static bool InTransaction(this IDbConnection db) =>
        db is IHasDbTransaction { DbTransaction: { } };

    public static IDbTransaction? GetTransaction(this IDbConnection db) =>
        db is IHasDbTransaction setDb ? setDb.DbTransaction : null;
}