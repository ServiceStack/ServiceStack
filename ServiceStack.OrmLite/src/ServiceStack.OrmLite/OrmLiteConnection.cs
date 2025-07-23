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
    : IDbConnection, IHasDbConnection, IHasDbTransaction, ISetDbTransaction, IHasDialectProvider, IHasName
{
    public readonly OrmLiteConnectionFactory Factory;
    public string? Name { get; set; }
    public IDbTransaction? Transaction { get; set; }
    public IDbTransaction? DbTransaction => Transaction;
    private IDbConnection? dbConnection;

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

    public IDbConnection DbConnection => dbConnection ??= ConnectionString.ToDbConnection(Factory.DialectProvider);

    public void Dispose()
    {
        Factory.OnDispose?.Invoke(this);
        if (!Factory.AutoDisposeConnection) return;

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
            Console.WriteLine(e);
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
        if (dbConnection == null)
        {
            LogManager.GetLogger(GetType()).WarnFormat("No dbConnection to Close()");
            return;
        }

        var id = Diagnostics.OrmLite.WriteConnectionCloseBefore(dbConnection);
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
                Diagnostics.OrmLite.WriteConnectionCloseError(id, connectionId, dbConnection, e);
            else
                Diagnostics.OrmLite.WriteConnectionCloseAfter(id, connectionId, dbConnection);
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
        var dbConn = DbConnection;
        if (dbConn.State == ConnectionState.Broken)
            dbConn.Close();

        if (dbConn.State == ConnectionState.Closed)
        {
            var id = Diagnostics.OrmLite.WriteConnectionOpenBefore(dbConn);
            Exception? e = null;
            try
            {
                dbConn.Open();
                //so the internal connection is wrapped for example by miniprofiler
                if (Factory.ConnectionFilter != null)
                    dbConn = Factory.ConnectionFilter(dbConn);

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
                    Diagnostics.OrmLite.WriteConnectionOpenError(id, dbConn, e);
                else
                    Diagnostics.OrmLite.WriteConnectionOpenAfter(id, dbConn);
            }
        }
    }

    public async Task OpenAsync(CancellationToken token = default)
    {
        var dbConn = DbConnection;
        if (dbConn.State == ConnectionState.Broken)
            dbConn.Close();

        if (dbConn.State == ConnectionState.Closed)
        {
            var id = Diagnostics.OrmLite.WriteConnectionOpenBefore(dbConn);
            Exception? e = null;
            try
            {
                await DialectProvider.OpenAsync(dbConn, token).ConfigAwait();
                //so the internal connection is wrapped for example by miniprofiler
                if (Factory.ConnectionFilter != null)
                    dbConn = Factory.ConnectionFilter(dbConn);

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
                    Diagnostics.OrmLite.WriteConnectionOpenError(id, dbConn, e);
                else
                    Diagnostics.OrmLite.WriteConnectionOpenAfter(id, dbConn);
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