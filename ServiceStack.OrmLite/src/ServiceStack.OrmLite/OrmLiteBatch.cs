using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Data;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// Sends the statements of the APIs that write many rows together with an ADO.NET DbBatch, instead of one at a time.
/// Each statement is prepared on the command as it normally is, then added to the batch instead of being run.
/// </summary>
internal sealed class OrmLiteBatch : IDisposable
{
    private static ILog Log => OrmLiteLog.Log;

    /// <summary>
    /// A batch for the statements of the command, or null to run them one at a time: when the driver doesn't
    /// support batches, the dialect has UseDbBatch disabled or the statements are observed as they're run.
    /// </summary>
    /// <param name="dbCmd">The command the statements are prepared on</param>
    /// <param name="needsRowsAffected">Whether the rows affected by each statement are needed</param>
    internal static OrmLiteBatch TryCreate(IDbCommand dbCmd, bool needsRowsAffected = false)
    {
#if NET6_0_OR_GREATER
        var dialect = dbCmd.GetDialectProvider();
        if (!dialect.UseDbBatch || dialect.BatchSize <= 1)
            return null;
        if (needsRowsAffected && !dialect.SupportsBatchRowsAffected)
            return null;
        // Results filters and the dialect's hooks get each command that's run
        if (OrmLiteConfig.ResultsFilter != null
            || dialect.OnBeforeExecuteNonQuery != null || dialect.OnAfterExecuteNonQuery != null)
            return null;
        if (dbCmd is OrmLiteCommand { OrmLiteConnection.WriteLock: not null })
            return null;
        if (dbCmd.ToDbCommand() is not DbCommand { Connection.CanCreateBatch: true } cmd)
            return null;

        return new OrmLiteBatch(cmd, dialect.BatchSize, needsRowsAffected);
#else
        return null;
#endif
    }

    /// <summary>
    /// The rows affected by the statements that have been sent
    /// </summary>
    internal int RowsAffected { get; private set; }

#if NET6_0_OR_GREATER
    private readonly DbCommand cmd;
    private readonly DbBatch batch;
    private readonly int batchSize;
    private readonly bool assertRowsUpdated;

    private OrmLiteBatch(DbCommand cmd, int batchSize, bool assertRowsUpdated)
    {
        this.cmd = cmd;
        this.batchSize = batchSize;
        this.assertRowsUpdated = assertRowsUpdated;
        batch = cmd.Connection!.CreateBatch();
        batch.Timeout = cmd.CommandTimeout;
    }

    private bool IsFull => batch.BatchCommands.Count >= batchSize;

    // Copies the statement that's prepared on the command, which is then prepared for the next row
    private void AddCommand(IDbCommand dbCmd)
    {
        (dbCmd as OrmLiteCommand)?.OrmLiteConnection.OnExecute(dbCmd, isNonQuery: true);
        OrmLiteConfig.BeforeExecFilter?.Invoke(dbCmd);

        if (Log.IsDebugEnabled)
            Log.DebugCommand(dbCmd);

        var batchCmd = batch.CreateBatchCommand();
        batchCmd.CommandText = cmd.CommandText;
        batchCmd.CommandType = cmd.CommandType;
        foreach (DbParameter p in cmd.Parameters)
        {
            batchCmd.Parameters.Add(Clone(p));
        }
        batch.BatchCommands.Add(batchCmd);
    }

    private DbParameter Clone(DbParameter p)
    {
        // Keeps the driver's own settings of the param, e.g. its NpgsqlDbType
        if (p is ICloneable cloneable && cloneable.Clone() is DbParameter clone)
            return clone;

        var to = cmd.CreateParameter();
        to.PopulateWith(p);
        return to;
    }

    private void BeforeSend()
    {
        // The command's transaction can be assigned after the batch is created
        batch.Transaction = cmd.Transaction;

        if (Log.IsDebugEnabled)
            Log.Debug($"SQL BATCH: sending {batch.BatchCommands.Count} statements");
    }

    private void AfterSend(int rowsAffected)
    {
        try
        {
            if (assertRowsUpdated)
            {
                foreach (var batchCmd in batch.BatchCommands)
                {
                    if (batchCmd.RecordsAffected == 0)
                        throw new OptimisticConcurrencyException(OrmLiteWriteCommandExtensions.RowModifiedMessage);
                }
            }
            if (rowsAffected > 0)
                RowsAffected += rowsAffected;
        }
        finally
        {
            batch.BatchCommands.Clear();
        }
    }
#endif

    /// <summary>
    /// Add the statement that's prepared on the command, sending the batch when it has BatchSize statements
    /// </summary>
    internal void Add(IDbCommand dbCmd)
    {
#if NET6_0_OR_GREATER
        AddCommand(dbCmd);
        if (IsFull)
            Flush();
#endif
    }

    internal Task AddAsync(IDbCommand dbCmd, CancellationToken token)
    {
#if NET6_0_OR_GREATER
        AddCommand(dbCmd);
        if (IsFull)
            return FlushAsync(token);
#endif
        return TypeConstants.EmptyTask;
    }

    /// <summary>
    /// Send the statements that have been added, e.g. before running a statement that has to run after them
    /// </summary>
    internal void Flush()
    {
#if NET6_0_OR_GREATER
        if (batch.BatchCommands.Count == 0)
            return;

        BeforeSend();
        int rowsAffected;
        try
        {
            rowsAffected = batch.ExecuteNonQuery();
        }
        catch
        {
            batch.BatchCommands.Clear();
            throw;
        }
        AfterSend(rowsAffected);
#endif
    }

    internal async Task FlushAsync(CancellationToken token)
    {
#if NET6_0_OR_GREATER
        if (batch.BatchCommands.Count == 0)
            return;

        BeforeSend();
        int rowsAffected;
        try
        {
            rowsAffected = await batch.ExecuteNonQueryAsync(token).ConfigAwait();
        }
        catch
        {
            batch.BatchCommands.Clear();
            throw;
        }
        AfterSend(rowsAffected);
#else
        await TypeConstants.EmptyTask;
#endif
    }

    public void Dispose()
    {
#if NET6_0_OR_GREATER
        batch.Dispose();
#endif
    }
}
