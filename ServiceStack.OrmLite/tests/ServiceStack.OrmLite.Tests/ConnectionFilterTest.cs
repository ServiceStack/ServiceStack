using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.Data;

namespace ServiceStack.OrmLite.Tests;

public class ConnectionFilterTest
{
    [Test]
    public void ReproduceConnectionFilterIssue()
    {
        var dbConnectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider)
        {
            ConnectionFilter = dbConnection => new DbConnectionWrapper(dbConnection)
        };

        using var connection = dbConnectionFactory.OpenDbConnection();
        Assert.That(connection.CreateCommand().GetType(), Is.EqualTo(typeof(DbCommandWrapper)));
        var result = connection.Scalar<int>("select 1");
        Assert.That(result, Is.EqualTo(1));
    }
}

public sealed class DbConnectionWrapper(IDbConnection innerConnection) : IDbConnection
{
    private IDbConnection InnerConnection { get; } = innerConnection;

    // we rely on the fact that a DbCommandWrapper is returned by the connection
    public IDbCommand CreateCommand()
    {
        return new DbCommandWrapper(InnerConnection.CreateCommand());
    }
    
    #region uncustomized logic
    public void Dispose()
    {
        InnerConnection.Dispose();
    }

    public IDbTransaction BeginTransaction()
    {
        return InnerConnection.BeginTransaction();
    }

    public IDbTransaction BeginTransaction(IsolationLevel il)
    {
        return InnerConnection.BeginTransaction(il);
    }

    public void Close()
    {
        InnerConnection.Close();
    }

    public void ChangeDatabase(string databaseName)
    {
        InnerConnection.ChangeDatabase(databaseName);
    }

    public void Open()
    {
        InnerConnection.Open();
    }

    public string ConnectionString
    {
        get => InnerConnection.ConnectionString;
        set => InnerConnection.ConnectionString = value;
    }

    public int ConnectionTimeout => InnerConnection.ConnectionTimeout;

    public string Database => InnerConnection.Database;

    public ConnectionState State => InnerConnection.State;
    #endregion
}

public class DbCommandWrapper(IDbCommand innerCommand) : IDbCommand, IHasDbCommand
{
    public IDbCommand DbCommand => innerCommand;

    // we implement shared transaction scope in our code, which relies on
    // this custom transaction setter that prevents nulling out the transaction
    public IDbTransaction Transaction
    {
        get => innerCommand.Transaction;
        set
        {
            if (value != null)
                innerCommand.Transaction = value;
        }
    }

    // we rely on these customized Execute... that logs and profiles each command
    public IDataReader ExecuteReader()
    {
        return ExecuteWithLoggingProfiling(() => innerCommand.ExecuteReader());
    }

    public int ExecuteNonQuery()
    {
        return ExecuteWithLoggingProfiling(() => innerCommand.ExecuteNonQuery());
    }

    public IDataReader ExecuteReader(CommandBehavior behavior)
    {
        return ExecuteWithLoggingProfiling(() => innerCommand.ExecuteReader(behavior));
    }

    public object ExecuteScalar()
    {
        return ExecuteWithLoggingProfiling(() => innerCommand.ExecuteScalar());
    }
    
    #region uncustomized logic
    public void Dispose()
    {
        innerCommand.Dispose();
    }

    public void Prepare()
    {
        innerCommand.Prepare();
    }

    public void Cancel()
    {
        innerCommand.Cancel();
    }

    public IDbDataParameter CreateParameter()
    {
        return innerCommand.CreateParameter();
    }

    public IDbConnection Connection
    {
        get => innerCommand.Connection;
        set => innerCommand.Connection = value;
    }
    
    public string CommandText
    {
        get => innerCommand.CommandText;
        set => innerCommand.CommandText = value;
    }

    public int CommandTimeout
    {
        get => innerCommand.CommandTimeout;
        set => innerCommand.CommandTimeout = value;
    }

    public CommandType CommandType
    {
        get => innerCommand.CommandType;
        set => innerCommand.CommandType = value;
    }

    public IDataParameterCollection Parameters => innerCommand.Parameters;

    public UpdateRowSource UpdatedRowSource
    {
        get => innerCommand.UpdatedRowSource;
        set => innerCommand.UpdatedRowSource = value;
    }
    #endregion

    private T ExecuteWithLoggingProfiling<T>(Func<T> func)
    {
        using var profiler = new DbCommandProfiler(innerCommand);
        LogCommand();
            
        return func();
    }

    private void LogCommand()
    {
        TestContext.WriteLine("Logging command");
        // code removed for brevity
    }
}

public sealed class DbCommandProfiler : IDisposable
{
    public DbCommandProfiler(IDbCommand command)
    {
        TestContext.WriteLine("Started profiling");
        // code removed for brevity
    }

    public void Dispose()
    {
        TestContext.WriteLine("Completed profiling");
        // code removed for brevity
    }
}