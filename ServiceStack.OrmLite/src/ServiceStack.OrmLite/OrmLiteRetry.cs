#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// Whether a statement that failed with a temporary error can be run again
/// </summary>
public enum TransientError
{
    /// <summary>
    /// Not a temporary error
    /// </summary>
    None,
    /// <summary>
    /// The database confirmed the statement wasn't applied, e.g. a deadlock or throttling, so any statement can be
    /// run again
    /// </summary>
    NotApplied,
    /// <summary>
    /// The statement may have been applied, e.g. the connection was lost, so only reads are run again
    /// </summary>
    MaybeApplied,
}

public static class OrmLiteRetry
{
    /// <summary>
    /// Retry statements that fail with a temporary error, waiting twice as long after each attempt, from delay
    /// (50ms by default) up to maxDelay (2s by default)
    /// </summary>
    public static OrmLiteRetryPolicy Exponential(int maxRetries = 3, TimeSpan? delay = null, TimeSpan? maxDelay = null) =>
        new(maxRetries, delay ?? TimeSpan.FromMilliseconds(50), maxDelay ?? TimeSpan.FromSeconds(2));

    /// <summary>
    /// Don't retry, e.g. to not retry a dialect when there's a global OrmLiteConfig.RetryPolicy.
    /// A dialect with it runs statements as it does without a policy.
    /// </summary>
    public static OrmLiteRetryPolicy None { get; } = new(0, TimeSpan.Zero, TimeSpan.Zero);
}

/// <summary>
/// When statements that fail with a temporary error are run again, e.g. after a deadlock, throttling or a lost
/// connection. Statements in a transaction aren't retried, use db.RunInTransaction() to retry the whole transaction.
/// </summary>
public class OrmLiteRetryPolicy
{
    /// <summary>
    /// The most times a statement is run again after its first attempt
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// How long to wait before the first retry, which doubles after each retry
    /// </summary>
    public TimeSpan Delay { get; }

    /// <summary>
    /// The longest to wait before a retry
    /// </summary>
    public TimeSpan MaxDelay { get; }

    /// <summary>
    /// Whether to wait a random 50-100% of the delay, so that clients that failed together don't retry together
    /// </summary>
    public bool Jitter { get; set; } = true;

    private readonly List<(Func<Exception, bool> predicate, TransientError kind)> handlers = new();
    private Action<Exception, int, TimeSpan>? onRetry;

    public OrmLiteRetryPolicy(int maxRetries, TimeSpan delay, TimeSpan maxDelay)
    {
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries));
        MaxRetries = maxRetries;
        Delay = delay;
        MaxDelay = maxDelay;
    }

    /// <summary>
    /// Also retry errors that the dialect doesn't recognize as temporary. They're treated as MaybeApplied by default,
    /// so only reads and whole transactions are run again.
    /// </summary>
    public OrmLiteRetryPolicy Handle(Func<Exception, bool> predicate, TransientError kind = TransientError.MaybeApplied)
    {
        if (kind == TransientError.None)
            throw new ArgumentException("A handled error must be NotApplied or MaybeApplied", nameof(kind));
        handlers.Add((predicate ?? throw new ArgumentNullException(nameof(predicate)), kind));
        return this;
    }

    /// <summary>
    /// Called before each retry with the error, the number of the retry (from 1) and how long it waits
    /// </summary>
    public OrmLiteRetryPolicy OnRetry(Action<Exception, int, TimeSpan> callback)
    {
        onRetry = callback;
        return this;
    }

    /// <summary>
    /// Whether the error, or one of its inner errors, is a temporary error of the dialect or of this policy
    /// </summary>
    public TransientError GetTransientError(IOrmLiteDialectProvider dialect, Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            var kind = dialect.GetTransientError(e);
            if (kind != TransientError.None)
                return kind;
            foreach (var (predicate, handledKind) in handlers)
            {
                if (predicate(e))
                    return handledKind;
            }
        }
        return TransientError.None;
    }

    /// <summary>
    /// How long to wait before a retry, from 1
    /// </summary>
    public TimeSpan GetDelay(int retry)
    {
        var ms = Math.Min(Delay.TotalMilliseconds * Math.Pow(2, Math.Max(retry - 1, 0)), MaxDelay.TotalMilliseconds);
        if (Jitter)
        {
            double random;
            lock (Random) random = Random.NextDouble();
            ms *= 0.5 + random * 0.5;
        }
        return TimeSpan.FromMilliseconds(ms);
    }

    private static readonly Random Random = new();

    internal void NotifyRetry(Exception ex, int retry, TimeSpan delay)
    {
        var log = OrmLiteLog.Log;
        if (log.IsDebugEnabled)
            log.DebugFormat("Retry {0} of {1} in {2:N0}ms after {3}: {4}", retry, MaxRetries, delay.TotalMilliseconds, ex.GetType().Name, ex.Message);
        onRetry?.Invoke(ex, retry, delay);
    }
}

/// <summary>
/// Runs statements, opens connections and transactions again after a temporary error
/// </summary>
internal static class OrmLiteRetryExec
{
    /// <summary>
    /// The policy of a statement on this command, or null if it isn't retried because there's no policy or it's
    /// in a transaction, whose earlier statements were rolled back with it
    /// </summary>
    internal static OrmLiteRetryPolicy? GetStatementPolicy(IDbCommand dbCmd, IOrmLiteDialectProvider dialect)
    {
        var policy = dialect.RetryPolicy;
        if (policy == null || policy.MaxRetries == 0 || dbCmd.Transaction != null)
            return null;
        if (dbCmd is OrmLiteCommand ormCmd && ormCmd.OrmLiteConnection.InTransaction())
            return null;
        return policy;
    }

    internal static bool ShouldRetryStatement(OrmLiteRetryPolicy policy, IOrmLiteDialectProvider dialect, IDbCommand dbCmd,
        Exception ex, int retry, out TimeSpan delay)
    {
        delay = default;
        if (retry > policy.MaxRetries)
            return false;
        var kind = policy.GetTransientError(dialect, ex);
        if (kind == TransientError.None || (kind == TransientError.MaybeApplied && !IsRead(dbCmd)))
            return false;
        delay = policy.GetDelay(retry);
        policy.NotifyRetry(ex, retry, delay);
        return true;
    }

    private static readonly Regex IntoRegex = new(@"\bINTO\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, OrmLiteUtils.DefaultRegexTimeout);
    private static readonly Regex WriteRegex = new(@"\b(INSERT|UPDATE|DELETE|MERGE|INTO)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, OrmLiteUtils.DefaultRegexTimeout);

    /// <summary>
    /// Whether a statement only reads, so it can be run again when it's not known if it was run
    /// </summary>
    internal static bool IsRead(IDbCommand dbCmd)
    {
        if (dbCmd.CommandType != CommandType.Text)
            return false;
        var sql = dbCmd.CommandText.AsSpan().TrimStart();
        if (sql.StartsWith("SELECT".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return !IntoRegex.IsMatch(dbCmd.CommandText); // SELECT ... INTO creates a table
        if (sql.StartsWith("WITH".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return !WriteRegex.IsMatch(dbCmd.CommandText); // a CTE can write
        return false;
    }

    /// <summary>
    /// Open the connection of a command again if it was lost
    /// </summary>
    internal static void Reopen(IDbCommand dbCmd)
    {
        if (dbCmd is OrmLiteCommand ormCmd)
        {
            if (ormCmd.OrmLiteConnection.State != ConnectionState.Open)
                ormCmd.OrmLiteConnection.Open();
            return;
        }
        var conn = dbCmd.Connection;
        if (conn == null || conn.State == ConnectionState.Open)
            return;
        if (conn.State == ConnectionState.Broken)
            conn.Close();
        conn.Open();
    }

    internal static async Task ReopenAsync(IDbCommand dbCmd, CancellationToken token)
    {
        if (dbCmd is OrmLiteCommand ormCmd)
        {
            if (ormCmd.OrmLiteConnection.State != ConnectionState.Open)
                await ormCmd.OrmLiteConnection.OpenAsync(token).ConfigAwait();
            return;
        }
        var conn = dbCmd.Connection;
        if (conn == null || conn.State == ConnectionState.Open)
            return;
        if (conn.State == ConnectionState.Broken)
            conn.Close();
        if (conn is DbConnection dbConn)
            await dbConn.OpenAsync(token).ConfigAwait();
        else
            conn.Open();
    }

    /// <summary>
    /// Run a statement, and again if it fails with a temporary error that's safe to retry
    /// </summary>
    internal static T Execute<T>(IDbCommand dbCmd, IOrmLiteDialectProvider dialect, Func<T> fn)
    {
        var policy = GetStatementPolicy(dbCmd, dialect);
        if (policy == null)
            return fn();

        for (var retry = 1; ; retry++)
        {
            try
            {
                return fn();
            }
            catch (Exception ex) when (ShouldRetryStatement(policy, dialect, dbCmd, ex, retry, out var delay))
            {
                Thread.Sleep(delay);
            }
            Reopen(dbCmd);
        }
    }

    internal static async Task<T> ExecuteAsync<T>(IDbCommand dbCmd, Func<Task<T>> fn, CancellationToken token)
    {
        var dialect = dbCmd.GetDialectProvider();
        var policy = GetStatementPolicy(dbCmd, dialect);
        if (policy == null)
            return await fn().ConfigAwait();

        // The dialect may run the statement synchronously on the command, which would also retry it
        var ormCmd = dbCmd as OrmLiteCommand;
        for (var retry = 1; ; retry++)
        {
            TimeSpan delay;
            try
            {
                if (ormCmd != null) ormCmd.SkipRetry = true;
                return await fn().ConfigAwait();
            }
            catch (Exception ex) when (ShouldRetryStatement(policy, dialect, dbCmd, ex, retry, out delay))
            {
            }
            finally
            {
                if (ormCmd != null) ormCmd.SkipRetry = false;
            }
            await Task.Delay(delay, token).ConfigAwait();
            await ReopenAsync(dbCmd, token).ConfigAwait();
        }
    }

    // Only the statements of a dialect with a policy create the closures of a retry
    internal static Task<int> ExecNonQueryWithRetryAsync(this IDbCommand dbCmd, CancellationToken token)
    {
        var dialect = dbCmd.GetDialectProvider();
        return dialect.RetryPolicy == null
            ? dialect.ExecuteNonQueryAsync(dbCmd, token)
            : RetryNonQueryAsync(dbCmd, dialect, token);
    }

    private static Task<int> RetryNonQueryAsync(IDbCommand dbCmd, IOrmLiteDialectProvider dialect, CancellationToken token) =>
        ExecuteAsync(dbCmd, () => dialect.ExecuteNonQueryAsync(dbCmd, token), token);

    internal static Task<IDataReader> ExecReaderWithRetryAsync(this IDbCommand dbCmd, CancellationToken token)
    {
        var dialect = dbCmd.GetDialectProvider();
        return dialect.RetryPolicy == null
            ? dialect.ExecuteReaderAsync(dbCmd, token)
            : RetryReaderAsync(dbCmd, dialect, token);
    }

    private static Task<IDataReader> RetryReaderAsync(IDbCommand dbCmd, IOrmLiteDialectProvider dialect, CancellationToken token) =>
        ExecuteAsync(dbCmd, () => dialect.ExecuteReaderAsync(dbCmd, token), token);

    internal static Task<object> ExecScalarWithRetryAsync(this IDbCommand dbCmd, CancellationToken token)
    {
        var dialect = dbCmd.GetDialectProvider();
        return dialect.RetryPolicy == null
            ? dialect.ExecuteScalarAsync(dbCmd, token)
            : RetryScalarAsync(dbCmd, dialect, token);
    }

    private static Task<object> RetryScalarAsync(IDbCommand dbCmd, IOrmLiteDialectProvider dialect, CancellationToken token) =>
        ExecuteAsync(dbCmd, () => dialect.ExecuteScalarAsync(dbCmd, token), token);

    /// <summary>
    /// Whether to open a connection again after it failed to open, which is safe for any temporary error
    /// </summary>
    internal static bool ShouldRetryOpen(OrmLiteRetryPolicy policy, IOrmLiteDialectProvider dialect, Exception ex,
        int retry, out TimeSpan delay)
    {
        delay = default;
        if (retry > policy.MaxRetries || policy.GetTransientError(dialect, ex) == TransientError.None)
            return false;
        delay = policy.GetDelay(retry);
        policy.NotifyRetry(ex, retry, delay);
        return true;
    }

    /// <summary>
    /// Whether to run a transaction again, which was rolled back. Unless the database confirmed it wasn't applied,
    /// a transaction that failed while committing may have been committed.
    /// </summary>
    internal static bool ShouldRetryTransaction(OrmLiteRetryPolicy? policy, IOrmLiteDialectProvider dialect,
        Exception ex, bool committing, int retry, out TimeSpan delay)
    {
        delay = default;
        if (policy == null || retry > policy.MaxRetries)
            return false;
        var kind = policy.GetTransientError(dialect, ex);
        if (kind == TransientError.None || (kind == TransientError.MaybeApplied && committing))
            return false;
        delay = policy.GetDelay(retry);
        policy.NotifyRetry(ex, retry, delay);
        return true;
    }

    /// <summary>
    /// Whether the error is from a connection to the database that was lost, which doesn't include timeouts as
    /// running a statement that timed out again is likely to time out again
    /// </summary>
    internal static bool IsConnectionLost(Exception ex)
    {
        for (var e = ex.InnerException; e != null; e = e.InnerException)
        {
            if (e is TimeoutException)
                return false;
            if (e is SocketException socketEx)
                return socketEx.SocketErrorCode != SocketError.TimedOut;
            if (e is IOException or EndOfStreamException)
                return !(e.InnerException is SocketException { SocketErrorCode: SocketError.TimedOut });
        }
        return false;
    }
}

public static class OrmLiteRetryApi
{
    /// <summary>
    /// Run fn in a transaction, then commit it. If it fails with a temporary error, e.g. a deadlock, the transaction
    /// is rolled back and fn is run again in a new transaction, as set by the dialect's RetryPolicy.
    /// Anything fn does outside the database, e.g. sending an email, is done again too.
    /// If the connection is already in a transaction, fn is run in it without retries.
    /// </summary>
    public static void RunInTransaction(this IDbConnection db, Action fn, IsolationLevel? isolationLevel = null) =>
        db.RunInTransaction(() => { fn(); return true; }, isolationLevel);

    /// <summary>
    /// Run fn in a transaction, then commit it and return its result. If it fails with a temporary error, e.g. a
    /// deadlock, the transaction is rolled back and fn is run again in a new transaction, as set by the dialect's
    /// RetryPolicy. Anything fn does outside the database, e.g. sending an email, is done again too.
    /// If the connection is already in a transaction, fn is run in it without retries.
    /// </summary>
    public static T RunInTransaction<T>(this IDbConnection db, Func<T> fn, IsolationLevel? isolationLevel = null)
    {
        if (db.InTransaction())
            return fn();

        var dialect = db.GetDialectProvider();
        for (var retry = 1; ; retry++)
        {
            if (db.State != ConnectionState.Open)
                db.Open();

            var committing = false;
            IDbTransaction? trans = null;
            try
            {
                trans = isolationLevel != null ? db.OpenTransaction(isolationLevel.Value) : db.OpenTransaction();
                var ret = fn();
                committing = true;
                trans.Commit();
                return ret;
            }
            catch (Exception ex) when (OrmLiteRetryExec.ShouldRetryTransaction(dialect.RetryPolicy, dialect, ex, committing, retry, out var delay))
            {
                DisposeQuietly(trans);
                trans = null;
                Thread.Sleep(delay);
            }
            finally
            {
                trans?.Dispose();
            }
        }
    }

    /// <summary>
    /// Run fn in a transaction, then commit it. If it fails with a temporary error, e.g. a deadlock, the transaction
    /// is rolled back and fn is run again in a new transaction, as set by the dialect's RetryPolicy.
    /// Anything fn does outside the database, e.g. sending an email, is done again too.
    /// If the connection is already in a transaction, fn is run in it without retries.
    /// </summary>
    public static Task RunInTransactionAsync(this IDbConnection db, Func<Task> fn, IsolationLevel? isolationLevel = null,
        CancellationToken token = default) =>
        db.RunInTransactionAsync(async () => { await fn().ConfigAwait(); return true; }, isolationLevel, token);

    /// <summary>
    /// Run fn in a transaction, then commit it and return its result. If it fails with a temporary error, e.g. a
    /// deadlock, the transaction is rolled back and fn is run again in a new transaction, as set by the dialect's
    /// RetryPolicy. Anything fn does outside the database, e.g. sending an email, is done again too.
    /// If the connection is already in a transaction, fn is run in it without retries.
    /// </summary>
    public static async Task<T> RunInTransactionAsync<T>(this IDbConnection db, Func<Task<T>> fn,
        IsolationLevel? isolationLevel = null, CancellationToken token = default)
    {
        if (db.InTransaction())
            return await fn().ConfigAwait();

        var dialect = db.GetDialectProvider();
        for (var retry = 1; ; retry++)
        {
            if (db.State != ConnectionState.Open)
            {
                if (db is OrmLiteConnection ormLiteConn)
                    await ormLiteConn.OpenAsync(token).ConfigAwait();
                else
                    db.Open();
            }

            var committing = false;
            IDbTransaction? trans = null;
            TimeSpan delay;
            try
            {
                trans = isolationLevel != null ? db.OpenTransaction(isolationLevel.Value) : db.OpenTransaction();
                var ret = await fn().ConfigAwait();
                committing = true;
                trans.Commit();
                return ret;
            }
            catch (Exception ex) when (OrmLiteRetryExec.ShouldRetryTransaction(dialect.RetryPolicy, dialect, ex, committing, retry, out delay))
            {
                DisposeQuietly(trans);
                trans = null;
            }
            finally
            {
                trans?.Dispose();
            }
            await Task.Delay(delay, token).ConfigAwait();
        }
    }

    /// <summary>
    /// Whether a write of many rows, which runs in a transaction of its own, runs in a transaction that's run again
    /// after a temporary error, e.g. InsertAll(). Writes in the App's transaction are retried with it.
    /// </summary>
    internal static bool RetriesAsTransaction(this IDbConnection db) =>
        !db.InTransaction() && db.GetDialectProvider().RetryPolicy != null;

    /// <summary>
    /// Run a write of many rows in a transaction that's run again after a temporary error, see RetriesAsTransaction()
    /// </summary>
    internal static T ExecAll<T>(this IDbConnection db, Func<IDbCommand, T> fn) =>
        db.RetriesAsTransaction() ? db.RunInTransaction(() => db.Exec(fn)) : db.Exec(fn);

    internal static void ExecAll(this IDbConnection db, Action<IDbCommand> fn)
    {
        if (db.RetriesAsTransaction())
            db.RunInTransaction(() => db.Exec(fn));
        else
            db.Exec(fn);
    }

    internal static Task<T> ExecAllAsync<T>(this IDbConnection db, Func<IDbCommand, Task<T>> fn, CancellationToken token) =>
        db.RetriesAsTransaction() ? db.RunInTransactionAsync(() => db.Exec(fn), token: token) : db.Exec(fn);

    internal static Task ExecAllAsync(this IDbConnection db, Func<IDbCommand, Task> fn, CancellationToken token) =>
        db.RetriesAsTransaction() ? db.RunInTransactionAsync(() => db.Exec(fn), token: token) : db.Exec(fn);

    /// <summary>
    /// Roll back a transaction that failed, whose connection may have been lost
    /// </summary>
    private static void DisposeQuietly(IDbTransaction? trans)
    {
        try
        {
            trans?.Dispose();
        }
        catch (Exception e)
        {
            OrmLiteLog.Log.Warn("Failed to roll back a transaction before retrying it", e);
        }
    }
}
