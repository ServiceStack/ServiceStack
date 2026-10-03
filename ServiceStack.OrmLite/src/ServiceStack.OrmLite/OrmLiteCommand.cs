#nullable enable

using System;
using System.Data;
using System.Diagnostics;
using ServiceStack.Data;

namespace ServiceStack.OrmLite;

public class OrmLiteCommand : IDbCommand, IHasDbCommand, IHasDialectProvider
{
    private readonly OrmLiteConnection dbConn;
    public OrmLiteConnection OrmLiteConnection => dbConn;
    private readonly IDbCommand dbCmd;
    public IOrmLiteDialectProvider DialectProvider { get; set; }
    public bool IsDisposed { get; private set; }

    public OrmLiteCommand(OrmLiteConnection dbConn, IDbCommand dbCmd)
    {
        this.dbConn = dbConn ?? throw new ArgumentNullException(nameof(dbConn));
        this.dbCmd = dbCmd ?? throw new ArgumentNullException(nameof(dbCmd));
        this.DialectProvider = dbConn.GetDialectProvider();
    }

    public Guid ConnectionId => dbConn.ConnectionId;

    public void Dispose()
    {
        IsDisposed = true;
        dbCmd.Dispose();
    }

    public void Prepare()
    {
        dbCmd.Prepare();
    }

    public void Cancel()
    {
        dbCmd.Cancel();
    }

    public IDbDataParameter CreateParameter()
    {
        return dbCmd.CreateParameter();
    }

    public long StartTimestamp; 
    public long EndTimestamp; 
#if NET8_0_OR_GREATER
    public TimeSpan GetElapsedTime() => Stopwatch.GetElapsedTime(StartTimestamp, EndTimestamp);
#endif
    
    /// <summary>
    /// Set while the dialect runs the statement for an async API, which retries it
    /// </summary>
    internal bool SkipRetry;

    private bool CanRetry => !SkipRetry && DialectProvider.RetryPolicy != null;

    public int ExecuteNonQuery()
    {
        dbConn.OnExecute(this, isNonQuery: true);
        return !CanRetry
            ? ExecuteNonQueryOnce()
            : OrmLiteRetryExec.Execute(this, DialectProvider, ExecuteNonQueryOnce);
    }

    private int ExecuteNonQueryOnce()
    {
        StartTimestamp = Stopwatch.GetTimestamp();
        DialectProvider.OnBeforeExecuteNonQuery?.Invoke(this);
        try
        {
            var writeLock = dbConn.WriteLock;
            if (writeLock != null)
            {
                lock (writeLock)
                {
                    return  dbCmd.ExecuteNonQuery();
                }
            }
            return dbCmd.ExecuteNonQuery();
        }
        finally
        {
            EndTimestamp = Stopwatch.GetTimestamp();
            DialectProvider.OnAfterExecuteNonQuery?.Invoke(this);
        }
    }

    public IDataReader ExecuteReader()
    {
        dbConn.OnExecute(this, isNonQuery: false);
        return !CanRetry
            ? ExecuteReaderOnce()
            : OrmLiteRetryExec.Execute(this, DialectProvider, ExecuteReaderOnce);
    }

    private IDataReader ExecuteReaderOnce()
    {
        StartTimestamp = Stopwatch.GetTimestamp();
        var ret = dbCmd.ExecuteReader();
        EndTimestamp = Stopwatch.GetTimestamp();
        return ret;
    }

    public IDataReader ExecuteReader(CommandBehavior behavior)
    {
        dbConn.OnExecute(this, isNonQuery: false);
        return !CanRetry
            ? ExecuteReaderOnce(behavior)
            : OrmLiteRetryExec.Execute(this, DialectProvider, () => ExecuteReaderOnce(behavior));
    }

    private IDataReader ExecuteReaderOnce(CommandBehavior behavior)
    {
        StartTimestamp = Stopwatch.GetTimestamp();
        var ret = dbCmd.ExecuteReader(behavior);
        EndTimestamp = Stopwatch.GetTimestamp();
        return ret;
    }

    public object? ExecuteScalar()
    {
        dbConn.OnExecute(this, isNonQuery: false);
        return !CanRetry
            ? ExecuteScalarOnce()
            : OrmLiteRetryExec.Execute(this, DialectProvider, ExecuteScalarOnce);
    }

    private object? ExecuteScalarOnce()
    {
        StartTimestamp = Stopwatch.GetTimestamp();
        var ret = dbCmd.ExecuteScalar();
        EndTimestamp = Stopwatch.GetTimestamp();
        return ret;
    }

    public IDbConnection? Connection
    {
        get => dbCmd.Connection;
        set => dbCmd.Connection = value;
    }
    public IDbTransaction? Transaction
    {
        get => dbCmd.Transaction;
        set => dbCmd.Transaction = value;
    }
    public string? CommandText
    {
        get => dbCmd.CommandText;
        set => dbCmd.CommandText = value;
    }
    public int CommandTimeout
    {
        get => dbCmd.CommandTimeout;
        set => dbCmd.CommandTimeout = value;
    }
    public CommandType CommandType
    {
        get => dbCmd.CommandType;
        set => dbCmd.CommandType = value;
    }
    public IDataParameterCollection Parameters => dbCmd.Parameters;

    public UpdateRowSource UpdatedRowSource
    {
        get => dbCmd.UpdatedRowSource;
        set => dbCmd.UpdatedRowSource = value;
    }

    public IDbCommand DbCommand => dbCmd;
}