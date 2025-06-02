#nullable enable
using System.Data;
using System.Data.Common;

namespace ServiceStack.OrmLite;

public class SingleWriterDbConnection(DbConnection dbConnection, object writeLock) : DbConnection
{
    private readonly DbConnection _dbConnection = dbConnection;
    public object WriteLock { get; } = writeLock;

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        return _dbConnection.BeginTransaction(isolationLevel);
    }

    public override void Close()
    {
        _dbConnection.Close();
    }

    public override void ChangeDatabase(string databaseName)
    {
        _dbConnection.ChangeDatabase(databaseName);
    }

    public override void Open()
    {
        _dbConnection.Open();
    }

    public override string ConnectionString
    {
        get => _dbConnection.ConnectionString;
        set => _dbConnection.ConnectionString = value;
    }

    public override string Database => _dbConnection.Database;
    public override ConnectionState State => _dbConnection.State;
    public override string DataSource => _dbConnection.DataSource;
    public override string ServerVersion => _dbConnection.ServerVersion;

    protected override DbCommand CreateDbCommand()
    {
        var dbCmd = _dbConnection.CreateCommand();
        return new SingleWriterDbCommand(dbCmd, WriteLock);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dbConnection?.Dispose();
        }
        base.Dispose(disposing);
    }
}

public class SingleWriterDbCommand(DbCommand dbCmd, object writeLock) : DbCommand
{
    private readonly DbCommand _dbCmd = dbCmd;
    private readonly object _writeLock = writeLock;

    public override void Prepare()
    {
        _dbCmd.Prepare();
    }

    public override string CommandText
    {
        get => _dbCmd.CommandText;
        set => _dbCmd.CommandText = value;
    }

    public override int CommandTimeout
    {
        get => _dbCmd.CommandTimeout;
        set => _dbCmd.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => _dbCmd.CommandType;
        set => _dbCmd.CommandType = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => _dbCmd.UpdatedRowSource;
        set => _dbCmd.UpdatedRowSource = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _dbCmd.Connection;
        set => _dbCmd.Connection = value;
    }

    protected override DbParameterCollection DbParameterCollection => _dbCmd.Parameters;

    protected override DbTransaction? DbTransaction
    {
        get => _dbCmd.Transaction;
        set => _dbCmd.Transaction = value;
    }

    public override bool DesignTimeVisible
    {
        get => _dbCmd.DesignTimeVisible;
        set => _dbCmd.DesignTimeVisible = value;
    }

    public override void Cancel()
    {
        _dbCmd.Cancel();
    }

    protected override DbParameter CreateDbParameter()
    {
        return _dbCmd.CreateParameter();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return _dbCmd.ExecuteReader(behavior);
    }

    public override int ExecuteNonQuery()
    {
        lock (_writeLock)
        {
            return _dbCmd.ExecuteNonQuery();
        }
    }

    public override object? ExecuteScalar()
    {
        return _dbCmd.ExecuteScalar();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dbCmd?.Dispose();
        }
        base.Dispose(disposing);
    }
}

public static class SingleWriterExtensions
{
    public static IDbConnection WithWriteLock(this IDbConnection dbConnection, object writeLock)
    {
        switch (dbConnection)
        {
            case OrmLiteConnection dbConn:
                dbConn.WriteLock = writeLock;
                return dbConn;
            case SingleWriterDbConnection:
                return dbConnection;
            default:
                return new SingleWriterDbConnection((DbConnection)dbConnection, writeLock);
        }
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