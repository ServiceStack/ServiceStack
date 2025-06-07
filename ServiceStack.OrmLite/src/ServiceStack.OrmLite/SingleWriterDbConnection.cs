#nullable enable
using System;
using System.Data;
using System.Data.Common;
using ServiceStack.Data;

namespace ServiceStack.OrmLite;

public class SingleWriterDbConnection : DbConnection
{
    private DbConnection? db;
    public readonly OrmLiteConnectionFactory? Factory;
    public DbConnection Db => db ??= (DbConnection)ConnectionString.ToDbConnection(Factory!.DialectProvider);
    public object writeLock;
    public object WriteLock => writeLock;

    public SingleWriterDbConnection(DbConnection db, object writeLock)
    {
        this.db = db;
        this.writeLock = writeLock;
    }

    public SingleWriterDbConnection(OrmLiteConnectionFactory factory, object writeLock)
    {
        Factory = factory;
        this.writeLock = writeLock;
    }

    internal DbTransaction? Transaction;
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        Transaction = Db.BeginTransaction(isolationLevel);
        return new SingleWriterTransaction(this, Transaction, isolationLevel);
    }

    public override void Close()
    {
        Db.Close();
    }

    public override void ChangeDatabase(string databaseName)
    {
        Db.ChangeDatabase(databaseName);
    }

    public override void Open()
    {
        var dbConn = Db;
        if (dbConn.State == ConnectionState.Broken)
            dbConn.Close();

        if (dbConn.State == ConnectionState.Closed)
        {
            var id = Diagnostics.OrmLite.WriteConnectionOpenBefore(dbConn);
            Exception? e = null;
            try
            {
                dbConn.Open();
                if (Factory != null)
                {
                    //so the internal connection is wrapped for example by miniprofiler
                    if (Factory.ConnectionFilter != null)
                        dbConn = (DbConnection)Factory.ConnectionFilter(dbConn);

                    Factory.DialectProvider.InitConnection(dbConn);
                }
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
    public override string ConnectionString
    {
        get => connectionString ?? throw new ArgumentNullException(nameof(ConnectionString));
        set => connectionString = value;
    }

    public override string Database => Db.Database;
    public override ConnectionState State => Db.State;
    public override string? DataSource => Db.DataSource;
    public override string? ServerVersion => Db.ServerVersion;

    protected override DbCommand CreateDbCommand()
    {
        var dbCmd = Db.CreateCommand();
        return new SingleWriterDbCommand(this, dbCmd, WriteLock);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            db?.Close();
            db?.Dispose();
            db = null;
        }
        base.Dispose(disposing);
    }
}

public class SingleWriterDbCommand(SingleWriterDbConnection db, DbCommand cmd, object writeLock) : DbCommand
{
    SingleWriterDbConnection Db = db;
    private readonly DbCommand Cmd = cmd;
    private readonly object WriteLock = writeLock;

    public override void Prepare()
    {
        Cmd.Prepare();
    }

    public override string CommandText
    {
        get => Cmd.CommandText;
        set => Cmd.CommandText = value;
    }

    public override int CommandTimeout
    {
        get => Cmd.CommandTimeout;
        set => Cmd.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => Cmd.CommandType;
        set => Cmd.CommandType = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => Cmd.UpdatedRowSource;
        set => Cmd.UpdatedRowSource = value;
    }

    protected override DbConnection? DbConnection
    {
        get => Cmd.Connection;
        set => Cmd.Connection = value;
    }

    protected override DbParameterCollection DbParameterCollection => Cmd.Parameters;

    protected override DbTransaction? DbTransaction
    {
        get => Db.Transaction;
        set => Db.Transaction = value;
    }

    public override bool DesignTimeVisible
    {
        get => Cmd.DesignTimeVisible;
        set => Cmd.DesignTimeVisible = value;
    }

    public override void Cancel()
    {
        Cmd.Cancel();
    }

    protected override DbParameter CreateDbParameter()
    {
        return Cmd.CreateParameter();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return Cmd.ExecuteReader(behavior);
    }

    public override int ExecuteNonQuery()
    {
        lock (WriteLock)
        {
            return Cmd.ExecuteNonQuery();
        }
    }

    public override object? ExecuteScalar()
    {
        return Cmd.ExecuteScalar();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Cmd?.Dispose();
        }
        base.Dispose(disposing);
    }
}

public static class SingleWriterExtensions
{
    public static DbConnection WithWriteLock(this IDbConnection dbConnection, object writeLock)
    {
        switch (dbConnection)
        {
            case SingleWriterDbConnection writeLockConn:
                return writeLockConn;
            case OrmLiteConnection dbConn:
                return new SingleWriterDbConnection((DbConnection)dbConn.ToDbConnection(), writeLock);
            default:
                return new SingleWriterDbConnection((DbConnection)dbConnection, writeLock);
        }
    }
    
    public static DbConnection OpenDbWithWriteLock(this IDbConnectionFactory dbFactory, string? namedConnection=null)
    {
        var dbConn = namedConnection != null
            ? dbFactory.OpenDbConnection(namedConnection)
            : dbFactory.OpenDbConnection();
        var writeLock = dbConn.GetWriteLock() ?? Locks.GetDbLock(namedConnection);
        return dbConn.WithWriteLock(writeLock);
    }

    public static DbConnection CreateDbWithWriteLock(this IDbConnectionFactory dbFactory, string? namedConnection=null)
    {
        return ((OrmLiteConnectionFactory)dbFactory).CreateDbWithWriteLock(namedConnection);
    }

    public static object? GetWriteLock(this IDbConnection dbConnection)
    {
        return dbConnection switch
        {
            OrmLiteConnection dbConn => dbConn.WriteLock,
            SingleWriterDbConnection singleWriterDbConn => singleWriterDbConn.WriteLock,
            _ => null
        };
    }
}

public class SingleWriterTransaction(SingleWriterDbConnection dbConnection, DbTransaction transaction, IsolationLevel isolationLevel) : DbTransaction
{
    SingleWriterDbConnection Db = dbConnection;
    protected override DbConnection DbConnection { get; } = dbConnection;
    public override IsolationLevel IsolationLevel { get; } = isolationLevel;
    public DbTransaction Transaction = transaction;

    public override void Commit()
    {
        Transaction.Commit();
    }

    public override void Rollback()
    {
        Transaction.Rollback();
    }

    protected override void Dispose(bool disposing)
    {
        Db.Transaction = null;
        base.Dispose(disposing);
    }
}