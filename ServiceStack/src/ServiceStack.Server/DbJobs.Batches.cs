#nullable enable
#if NET8_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using ServiceStack.Jobs;
using ServiceStack.OrmLite;

namespace ServiceStack;

/// <summary>
/// RDBMS specifics of the Job Batch, Queue and node registry features shared with the SQLite
/// provider in BackgroundJobsProviderBase
/// </summary>
public partial class DbJobs
{
    protected override IBackgroundJobsOptions Options => feature;
    protected override CancellationToken JobsToken => ct;
    protected override List<DateTime> GetArchiveMonths(IDbConnection db) => feature.GetTableMonths(db);
    protected override void DropArchive(DateTime month) => feature.DbProvider.DropTables(month);
    protected override List<string>? NodeQueues => feature.Queues;

    /// <summary>
    /// A slot outlives its lease so that it isn't lost between renewals, and it's renewed with the lease
    /// </summary>
    protected override int GetConcurrencySlotSecs(BackgroundJob job) =>
        Math.Max(feature.LeaseDurationSecs, job.TimeoutSecs ?? feature.DefaultTimeoutSecs);

    // Queues this node processes

    private bool ProcessesQueue(string queue) =>
        feature.Queues == null || feature.Queues.Contains(queue, StringComparer.OrdinalIgnoreCase);

    // Prefetch

    /// <summary>Jobs on each queue this node has claimed, whether they're running or waiting for a Worker</summary>
    private Dictionary<string, int> GetInFlightCounts()
    {
        var to = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var worker in workers.Values)
        {
            foreach (var job in worker.GetLeasedJobs())
            {
                to[job.Queue] = to.GetValueOrDefault(job.Queue) + 1;
            }
        }
        return to;
    }

    /// <summary>
    /// Max Jobs this node holds for a queue: one per Worker it can run, plus MaxPrefetchJobs waiting
    /// </summary>
    private int GetInFlightLimit(string queue) => GetQueueConcurrency(queue) + Math.Max(0, feature.MaxPrefetchJobs);

    private bool HasPrefetchCapacity(string queue) =>
        GetInFlightCounts().GetValueOrDefault(queue) < GetInFlightLimit(queue);

    // Cluster-wide rate limits

    private readonly ConcurrentDictionary<string, DateTime> rateLimitedUntil = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Takes a start from the queue's RateLimit, counted in the JobQueue row so the limit applies across
    /// every node rather than to each. Windows are aligned to the clock so every node agrees which window
    /// a start falls in. A node that finds a window full doesn't ask again until the next one.
    /// </summary>
    protected override bool TryTakeRateLimitSlot(string queue, DateTime now)
    {
        if (!GetJobQueueMap().TryGetValue(queue, out var q) || q.RateLimit is not > 0)
            return true;
        if (rateLimitedUntil.TryGetValue(queue, out var until) && now < until)
            return false;

        var window = GetRateLimitWindow(q);
        var windowStart = new DateTime(now.Ticks - now.Ticks % window.Ticks, DateTimeKind.Utc);
        try
        {
            using var db = OpenDb();
            var dialect = db.GetDialectProvider();
            var table = dialect.GetQuotedTableName(typeof(JobQueue));
            var name = dialect.GetQuotedColumnName(nameof(JobQueue.Name));
            var count = dialect.GetQuotedColumnName(nameof(JobQueue.RateLimitCount));
            var start = dialect.GetQuotedColumnName(nameof(JobQueue.RateLimitWindowStart));
            // The count is assigned before the window start: MySQL evaluates SET assignments in order
            var updated = db.ExecuteSql($"UPDATE {table} SET " +
                $"{count} = CASE WHEN {start} = @windowStart THEN COALESCE({count}, 0) + 1 ELSE 1 END, " +
                $"{start} = @windowStart " +
                $"WHERE {name} = @queue AND ({start} IS NULL OR {start} <> @windowStart OR COALESCE({count}, 0) < @limit)",
                new { windowStart, queue = q.Name, limit = q.RateLimit.Value });
            if (updated > 0)
                return true;
            rateLimitedUntil[queue] = windowStart + window;
            return false;
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error taking rate limit slot for queue {Queue}", queue);
            return false;
        }
    }

    protected override bool IsRateLimited(JobQueue q, DateTime now) =>
        q.Name != null && rateLimitedUntil.TryGetValue(q.Name, out var until) && now < until;
}
#endif
