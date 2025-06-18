#nullable enable
using System;
using System.Data;
using System.Data.Common;
using ServiceStack.Data;

namespace ServiceStack.OrmLite;

public class SingleWriterDbConnection : DbConnection, IHasWriteLock
{
    private DbConnection? db;
    public OrmLiteConnectionFactory? Factory { get; }
    public object WriteLock { get; }

    public DbConnection Db => db ??= (DbConnection)ConnectionString.ToDbConnection(Factory!.DialectProvider);

    public SingleWriterDbConnection(DbConnection db, object writeLock)
    {
        this.db = db;
        this.WriteLock = writeLock;
        this.connectionString = db.ConnectionString;
    }

    public SingleWriterDbConnection(OrmLiteConnectionFactory factory, object writeLock)
    {
        Factory = factory;
        this.WriteLock = writeLock;
        this.connectionString = factory.ConnectionString;
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

                    Factory.DialectProvider.InitConnection(this);
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

    private string connectionString;
    public override string ConnectionString
    {
        get => connectionString;
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

    public override void Prepare()
    {
        cmd.Prepare();
    }

    public override string CommandText
    {
        get => cmd.CommandText;
        set => cmd.CommandText = value;
    }

    public override int CommandTimeout
    {
        get => cmd.CommandTimeout;
        set => cmd.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => cmd.CommandType;
        set => cmd.CommandType = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => cmd.UpdatedRowSource;
        set => cmd.UpdatedRowSource = value;
    }

    protected override DbConnection? DbConnection
    {
        get => cmd.Connection;
        set => cmd.Connection = value;
    }

    protected override DbParameterCollection DbParameterCollection => cmd.Parameters;

    protected override DbTransaction? DbTransaction
    {
        get => Db.Transaction;
        set => Db.Transaction = value;
    }

    public override bool DesignTimeVisible
    {
        get => cmd.DesignTimeVisible;
        set => cmd.DesignTimeVisible = value;
    }

    public override void Cancel()
    {
        cmd.Cancel();
    }

    protected override DbParameter CreateDbParameter()
    {
        return cmd.CreateParameter();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return cmd.ExecuteReader(behavior);
    }

    public override int ExecuteNonQuery()
    {
        lock (writeLock)
        {
            return cmd.ExecuteNonQuery();
        }
    }

    public override object? ExecuteScalar()
    {
        return cmd.ExecuteScalar();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            cmd?.Dispose();
        }
        base.Dispose(disposing);
    }
}

public static class SingleWriterExtensions
{
    public static DbConnection WithWriteLock(this IDbConnection db, object writeLock) => db switch
    {
        SingleWriterDbConnection writeLockConn => writeLockConn,
        OrmLiteConnection ormConn => new SingleWriterDbConnection((DbConnection)ormConn.ToDbConnection(), writeLock),
        _ => new SingleWriterDbConnection((DbConnection)db, writeLock)
    };

    /// <summary>
    /// Open a DB connection with a SingleWriter Lock 
    /// </summary>
    public static DbConnection OpenSingleWriterDb(this IDbConnectionFactory dbFactory, string? namedConnection=null)
    {
        var dbConn = namedConnection != null
            ? dbFactory.OpenDbConnection(namedConnection)
            : dbFactory.OpenDbConnection();
        var writeLock = dbConn.GetWriteLock() ?? Locks.GetDbLock(namedConnection);
        return dbConn.WithWriteLock(writeLock);
    }

    /// <summary>
    /// Create a DB connection with a SingleWriter Lock 
    /// </summary>
    public static DbConnection CreateSingleWriterDb(this IDbConnectionFactory dbFactory, string? namedConnection=null)
    {
        return ((OrmLiteConnectionFactory)dbFactory).CreateDbWithWriteLock(namedConnection);
    }

    public static object GetWriteLock(this IDbConnection dbConnection)
    {
        return dbConnection switch
        {
            OrmLiteConnection dbConn => dbConn.WriteLock ?? dbConnection,
            IHasWriteLock hasWriteLock => hasWriteLock.WriteLock,
            _ => dbConnection,
        };
    }
}

public class SingleWriterTransaction(SingleWriterDbConnection dbConnection, DbTransaction transaction, IsolationLevel isolationLevel) : DbTransaction
{
    protected override DbConnection DbConnection { get; } = dbConnection;
    public override IsolationLevel IsolationLevel { get; } = isolationLevel;
    public readonly DbTransaction Transaction = transaction;

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
        dbConnection.Transaction = null;
        base.Dispose(disposing);
    }
}