#nullable enable
#if NET8_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.Jobs;

/// <summary>
/// Configuration shared by the SQLite (BackgroundsJobFeature) and RDBMS (DatabaseJobFeature) job providers
/// </summary>
public interface IBackgroundJobsOptions
{
    /// <summary>Identifies this App Server in the JobNode registry</summary>
    string ServerId { get; }
    int DefaultTimeoutSecs { get; }
    int MaxConcurrentJobs { get; }
    Dictionary<string, int> QueueConcurrency { get; }
    int ReloadJobQueuesSecs { get; }
    int NodeHeartbeatSecs { get; }
    int NodeTimeoutSecs { get; }
    TimeSpan? ArchiveRetention { get; }
    TimeSpan? JobSummaryRetention { get; }
    Func<JobReplyToContext, Task> OnJobReplyTo { get; }
    /// <summary>
    /// Whether a Job's ReplyTo may be used, checked when it's queued and again before its result is sent.
    /// Null allows any ReplyTo, see JobReplyTo.AllowUrlPrefixes() to restrict which URLs results are sent to.
    /// </summary>
    Func<BackgroundJobBase, bool>? ValidateReplyTo { get; }
}

/// <summary>
/// Job Batches, Queue controls, concurrency keys, rate limits, the node registry and failed attempt
/// history, shared by the SQLite and RDBMS job providers
/// </summary>
public abstract class BackgroundJobsProviderBase : IBackgroundJobsQueues
{
    protected readonly ILogger Log;
    protected BackgroundJobsProviderBase(ILogger log) => Log = log;

    protected abstract IBackgroundJobsOptions Options { get; }
    /// <summary>The token Job callbacks and ReplyTo deliveries run with</summary>
    protected abstract CancellationToken JobsToken { get; }
    public abstract IDbConnection OpenDb();
    public abstract BackgroundJobRef EnqueueCommand(string commandName, object arg, BackgroundJobOptions? options = null);
    public abstract bool CancelJob(long jobId);
    public abstract object CreateRequest(BackgroundJobBase job);
    public abstract object? CreateResponse(BackgroundJobBase job);
    public abstract List<WorkerStats> GetWorkerStats();
    protected abstract CommandInfo AssertCommand(string? command);
    /// <summary>The months that have a CompletedJob/FailedJob archive</summary>
    protected abstract List<DateTime> GetArchiveMonths(IDbConnection db);
    /// <summary>Drops the CompletedJob/FailedJob archive of a month</summary>
    protected abstract void DropArchive(DateTime month);

    /// <summary>
    /// Runs a write against the Jobs database. SQLite overrides it to take the database's write lock.
    /// </summary>
    protected virtual T Write<T>(IDbConnection db, Func<T> fn) => fn();
    protected void Write(IDbConnection db, Action fn) => Write(db, () => { fn(); return 0; });

    /// <summary>How long a ConcurrencyKey slot is held before it's assumed its holder is gone</summary>
    protected virtual int GetConcurrencySlotSecs(BackgroundJob job) => job.TimeoutSecs ?? Options.DefaultTimeoutSecs;

    // ReplyTo

    /// <summary>Rejects a Job whose ReplyTo isn't allowed by ValidateReplyTo</summary>
    protected void AssertValidReplyTo(BackgroundJobBase job)
    {
        if (job.ReplyTo != null && Options.ValidateReplyTo?.Invoke(job) == false)
            throw new ArgumentException($"ReplyTo '{job.ReplyTo}' is not allowed", nameof(job.ReplyTo));
    }

    /// <summary>
    /// Delivers a completed Job's result to its ReplyTo address. Failures are logged but don't fail
    /// the Job, which has already run successfully.
    /// </summary>
    protected async Task NotifyReplyToAsync(BackgroundJob job, object? response)
    {
        if (job.ReplyTo == null || job.Transient)
            return;
        try
        {
            // Checked again in case ValidateReplyTo changed since the Job was queued
            if (Options.ValidateReplyTo?.Invoke(job) == false)
            {
                Log.LogWarning("JOBS Did not send Job {Id} result to ReplyTo '{ReplyTo}': it's not allowed",
                    job.Id, job.ReplyTo);
                return;
            }
            response ??= CreateResponse(job);
            var ctx = new JobReplyToContext((IBackgroundJobs)this, job, job.ReplyTo, response) { Token = JobsToken };
            await Options.OnJobReplyTo(ctx).ConfigAwait();
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Could not send Job {Id} result to ReplyTo '{ReplyTo}'",
                job.Id, job.ReplyTo);
        }
    }

    // Job Dependencies

    /// <summary>
    /// The finished parent a dependent Job can run after, or null if it has to keep waiting. A Job with
    /// the OnFinished policy also runs after a parent that failed or was cancelled.
    /// </summary>
    protected static CompletedJob? GetFinishedParent(BackgroundJobBase job, JobResult? parent)
    {
        if (parent?.Completed != null)
            return parent.Completed;
        if (parent?.Failed != null && job.DependsOnPolicy == JobDependencyPolicy.OnFinished)
            return parent.Failed.PopulateJob(new CompletedJob());
        return null;
    }

    /// <summary>Only dependents that require their parent to succeed are cancelled when it doesn't</summary>
    protected static bool CancelsWithParent(BackgroundJobBase job) =>
        job.DependsOnPolicy != JobDependencyPolicy.OnFinished;

    // Job Batches

    public JobBatch? GetJobBatch(string batchId)
    {
        using var db = OpenDb();
        return db.SingleById<JobBatch>(batchId);
    }

    public JobBatch CreateJobBatch(JobBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (string.IsNullOrEmpty(batch.Id))
            throw new ArgumentNullException(nameof(batch.Id));
        if (batch.Callback != null)
            AssertCommand(batch.Callback);
        if (batch.OnSuccess != null)
            AssertCommand(batch.OnSuccess);

        batch.CreatedDate = batch.CreatedDate == default ? DateTime.UtcNow : batch.CreatedDate;
        using var db = OpenDb();
        Write(db, () => {
            var updated = db.UpdateOnly(() => new JobBatch {
                Description = batch.Description,
                Total = batch.Total,
                Callback = batch.Callback,
                OnSuccess = batch.OnSuccess,
                ParentBatchId = batch.ParentBatchId,
                CreatedBy = batch.CreatedBy,
                Meta = batch.Meta,
            }, where: x => x.Id == batch.Id);
            if (updated == 0)
                db.Insert(batch);
        });
        return db.SingleById<JobBatch>(batch.Id) ?? batch;
    }

    /// <summary>
    /// Creates a Job's batch if it doesn't exist yet. Run on its own connection before the Job is inserted,
    /// since a failed insert of a concurrently created batch would abort the enclosing transaction on
    /// PostgreSQL.
    /// </summary>
    /// <exception cref="InvalidOperationException">The batch was cancelled</exception>
    protected void EnsureJobBatch(string batchId, string? createdBy)
    {
        using var db = OpenDb();
        var batch = db.SingleById<JobBatch>(batchId);
        if (batch == null)
        {
            try
            {
                Write(db, () => db.Insert(new JobBatch {
                    Id = batchId,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = createdBy,
                }));
            }
            catch
            {
                // Another submitter created it first
                batch = db.SingleById<JobBatch>(batchId);
                if (batch == null)
                    throw;
            }
        }
        if (batch?.CancelledDate != null)
            throw new InvalidOperationException($"Job Batch '{batchId}' was cancelled");
    }

    /// <summary>
    /// Counts a Job against its batch, within the transaction that inserts the Job. Incremented in SQL
    /// so concurrent submitters (and nodes) can't lose an update to a read-modify-write race.
    /// </summary>
    protected static void AddJobToBatch(IDbConnection db, string batchId)
    {
        var dialect = db.GetDialectProvider();
        var table = dialect.GetQuotedTableName(typeof(JobBatch));
        var id = dialect.GetQuotedColumnName(nameof(JobBatch.Id));
        var queued = dialect.GetQuotedColumnName(nameof(JobBatch.Queued));
        db.ExecuteSql($"UPDATE {table} SET {queued} = {queued} + 1 WHERE {id} = @batchId", new { batchId });
    }

    public List<long> CancelJobBatch(string batchId)
    {
        ArgumentNullException.ThrowIfNull(batchId);
        var now = DateTime.UtcNow;
        List<long> jobIds;
        using (var db = OpenDb())
        {
            Write(db, () => db.UpdateOnly(() => new JobBatch { CancelledDate = now },
                where: x => x.Id == batchId && x.CancelledDate == null));
            jobIds = db.Column<long>(db.From<BackgroundJob>()
                .Where(x => x.BatchId == batchId && x.CompletedDate == null)
                .Select(x => x.Id));
        }
        var to = new List<long>();
        foreach (var jobId in jobIds)
        {
            if (CancelJob(jobId))
                to.Add(jobId);
        }
        Log.LogInformation("JOBS Cancelled Batch {BatchId} and {Count} of its Jobs", batchId, to.Count);
        return to;
    }

    public const string BatchCallbackPrefix = "batch-callback:";
    public const string BatchOnSuccessPrefix = "batch-success:";

    /// <summary>
    /// Moves a Job from a batch's queued count into its terminal count and, when it was the last
    /// outstanding Job, records the batch as complete and queues its callbacks exactly once.
    /// </summary>
    protected void UpdateJobBatch(BackgroundJobBase job)
    {
        var batchId = job.BatchId;
        if (batchId == null)
            return;
        try
        {
            var finishedColumn = job.State switch {
                BackgroundJobState.Failed => nameof(JobBatch.Failed),
                BackgroundJobState.Cancelled => nameof(JobBatch.Cancelled),
                _ => nameof(JobBatch.Completed),
            };

            var now = DateTime.UtcNow;
            JobBatch? batch;
            var claimedCallback = false;

            // Scoped so this connection is released before the callback Jobs are queued on their own
            using (var db = OpenDb())
            {
                var dialect = db.GetDialectProvider();
                var table = dialect.GetQuotedTableName(typeof(JobBatch));
                var id = dialect.GetQuotedColumnName(nameof(JobBatch.Id));
                var queued = dialect.GetQuotedColumnName(nameof(JobBatch.Queued));
                var finished = dialect.GetQuotedColumnName(finishedColumn);

                Write(db, () => db.ExecuteSql($"UPDATE {table} SET " +
                    $"{queued} = CASE WHEN {queued} > 0 THEN {queued} - 1 ELSE 0 END, " +
                    $"{finished} = {finished} + 1 WHERE {id} = @batchId", new { batchId }));
                batch = db.SingleById<JobBatch>(batchId);
                if (batch == null || !IsBatchComplete(batch))
                    return;

                Write(db, () => db.UpdateOnly(() => new JobBatch { CompletedDate = now },
                    where: x => x.Id == batchId && x.CompletedDate == null));
                if (batch.Callback == null && batch.OnSuccess == null)
                    return;

                // Only the node that wins this compare-and-set queues the callbacks
                claimedCallback = Write(db, () => db.UpdateOnly(() => new JobBatch { NotifiedDate = now },
                    where: x => x.Id == batchId && x.NotifiedDate == null)) > 0;
            }

            if (!claimedCallback)
                return;

            batch.CompletedDate = now;
            batch.NotifiedDate = now;
            if (batch.Callback != null)
            {
                Log.LogInformation("JOBS Batch {BatchId} finished, queueing callback {Callback}",
                    batchId, batch.Callback);
                EnqueueCommand(batch.Callback, batch, new() {
                    RefId = $"{BatchCallbackPrefix}{batchId}",
                    DuplicateRefIdBehavior = DuplicateRefIdBehavior.ReturnExisting,
                });
            }
            if (batch.OnSuccess != null && batch.Failed == 0 && batch.Cancelled == 0)
            {
                Log.LogInformation("JOBS Batch {BatchId} succeeded, queueing {OnSuccess}",
                    batchId, batch.OnSuccess);
                EnqueueCommand(batch.OnSuccess, batch, new() {
                    RefId = $"{BatchOnSuccessPrefix}{batchId}",
                    DuplicateRefIdBehavior = DuplicateRefIdBehavior.ReturnExisting,
                });
            }
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error updating Job Batch {BatchId}", batchId);
        }
    }

    /// <summary>
    /// Returns a requeued Job to its batch's queued count, so the batch isn't counted as finished
    /// while it runs again, nor counted twice when it does.
    /// </summary>
    protected void RequeueJobInBatch(string batchId, BackgroundJobState previousState)
    {
        try
        {
            var finishedColumn = previousState switch {
                BackgroundJobState.Failed => nameof(JobBatch.Failed),
                BackgroundJobState.Cancelled => nameof(JobBatch.Cancelled),
                _ => nameof(JobBatch.Completed),
            };
            using var db = OpenDb();
            var dialect = db.GetDialectProvider();
            var table = dialect.GetQuotedTableName(typeof(JobBatch));
            var id = dialect.GetQuotedColumnName(nameof(JobBatch.Id));
            var queued = dialect.GetQuotedColumnName(nameof(JobBatch.Queued));
            var finished = dialect.GetQuotedColumnName(finishedColumn);
            var completedDate = dialect.GetQuotedColumnName(nameof(JobBatch.CompletedDate));
            Write(db, () => db.ExecuteSql($"UPDATE {table} SET {queued} = {queued} + 1, " +
                $"{finished} = CASE WHEN {finished} > 0 THEN {finished} - 1 ELSE 0 END, " +
                $"{completedDate} = NULL WHERE {id} = @batchId", new { batchId }));
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error requeueing Job in Batch {BatchId}", batchId);
        }
    }

    /// <summary>
    /// A batch with a known Total finishes when every Job is accounted for. Without a Total it can
    /// only be judged complete once nothing is left queued, so batches that are built up over time
    /// should set Total to get a reliable Callback.
    /// </summary>
    protected static bool IsBatchComplete(JobBatch batch) => batch.Total > 0
        ? batch.Finished >= batch.Total
        : batch.Queued == 0 && batch.Finished > 0;

    /// <summary>
    /// Whether every Job in a Batch has finished, used by Jobs that fan in on a whole Batch.
    /// An unknown Batch is not treated as finished, so a Job can't run before its Batch is created.
    /// </summary>
    public bool IsBatchFinished(string batchId)
    {
        var batch = GetJobBatch(batchId);
        return batch != null && IsBatchComplete(batch);
    }

    // Job Queues

    private Dictionary<string, JobQueue> jobQueues = new(StringComparer.OrdinalIgnoreCase);
    private DateTime lastJobQueuesLoad = DateTime.MinValue;

    /// <summary>
    /// Runtime queue controls are cached and refreshed on a timer so that pausing a queue on one
    /// node takes effect on the others without a restart.
    /// </summary>
    protected Dictionary<string, JobQueue> GetJobQueueMap()
    {
        if (DateTime.UtcNow - lastJobQueuesLoad >= TimeSpan.FromSeconds(Options.ReloadJobQueuesSecs))
            ReloadJobQueues();
        return jobQueues;
    }

    private void ReloadJobQueues()
    {
        lastJobQueuesLoad = DateTime.UtcNow;
        try
        {
            using var db = OpenDb();
            var to = new Dictionary<string, JobQueue>(StringComparer.OrdinalIgnoreCase);
            foreach (var queue in db.Select<JobQueue>())
            {
                if (queue.Name != null)
                    to[queue.Name] = queue;
            }
            jobQueues = to;
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error reloading Job Queues");
        }
    }

    public List<JobQueue> GetJobQueues()
    {
        ReloadJobQueues();
        var to = new Dictionary<string, JobQueue>(jobQueues, StringComparer.OrdinalIgnoreCase);
        // Include queues that are in use or configured but have no saved controls yet
        var knownQueues = new List<string> { JobQueues.Default };
        knownQueues.AddRange(Options.QueueConcurrency.Keys);
        using (var db = OpenDb())
        {
            knownQueues.AddRange(db.Column<string>(db.From<BackgroundJob>()
                .SelectDistinct(x => x.Queue)));
        }
        foreach (var name in knownQueues)
        {
            if (name != null && !to.ContainsKey(name))
                to[name] = new JobQueue { Name = name };
        }
        return to.Values.OrderBy(x => x.Name).ToList();
    }

    public JobQueue SetJobQueue(string name, bool? paused = null, int? concurrency = null,
        string? modifiedBy = null, int? rateLimit = null, int? rateLimitSecs = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (concurrency is <= 0)
            throw new ArgumentOutOfRangeException(nameof(concurrency), "Concurrency must be greater than zero");
        if (rateLimit is < 0)
            throw new ArgumentOutOfRangeException(nameof(rateLimit), "RateLimit can't be negative");
        if (rateLimitSecs is <= 0)
            throw new ArgumentOutOfRangeException(nameof(rateLimitSecs), "RateLimitSecs must be greater than zero");

        var now = DateTime.UtcNow;
        using var db = OpenDb();
        var existing = db.SingleById<JobQueue>(name);
        var to = existing ?? new JobQueue { Name = name };
        if (paused != null)
            to.Paused = paused.Value;
        if (concurrency != null)
            to.Concurrency = concurrency;
        if (rateLimit != null)
            to.RateLimit = rateLimit > 0 ? rateLimit : null;
        if (rateLimitSecs != null)
            to.RateLimitSecs = rateLimitSecs;
        to.ModifiedDate = now;
        to.ModifiedBy = modifiedBy;
        Write(db, () => {
            if (existing == null)
                db.Insert(to);
            else
                db.UpdateOnly(() => new JobQueue {
                    Paused = to.Paused,
                    Concurrency = to.Concurrency,
                    RateLimit = to.RateLimit,
                    RateLimitSecs = to.RateLimitSecs,
                    ModifiedDate = to.ModifiedDate,
                    ModifiedBy = to.ModifiedBy,
                }, where: x => x.Name == name);
        });

        jobQueues = new Dictionary<string, JobQueue>(jobQueues, StringComparer.OrdinalIgnoreCase) {
            [name] = to,
        };
        Log.LogInformation("JOBS Queue {Queue} updated: Paused={Paused}, Concurrency={Concurrency}, RateLimit={RateLimit}/{RateLimitSecs}s",
            name, to.Paused, to.Concurrency, to.RateLimit, to.RateLimitSecs ?? 1);
        return to;
    }

    public bool IsQueuePaused(string queue) =>
        GetJobQueueMap().TryGetValue(queue, out var q) && q.Paused;

    /// <summary>
    /// Max concurrent Jobs for a queue: a runtime override wins, then configured per-queue
    /// concurrency, then the global default.
    /// </summary>
    public int GetQueueConcurrency(string queue)
    {
        if (GetJobQueueMap().TryGetValue(queue, out var q) && q.Concurrency is > 0)
            return q.Concurrency.Value;
        return Math.Max(1, Options.QueueConcurrency.TryGetValue(queue, out var configured)
            ? configured
            : Options.MaxConcurrentJobs);
    }

    /// <summary>
    /// Queues whose Jobs can't be claimed right now: paused, or rate limited with no slot left in
    /// the current window.
    /// </summary>
    protected List<string> GetBlockedQueues(DateTime now)
    {
        var to = new List<string>();
        foreach (var q in GetJobQueueMap().Values)
        {
            if (q.Name == null)
                continue;
            if (q.Paused || (q.RateLimit is > 0 && IsRateLimited(q, now)))
                to.Add(q.Name);
        }
        return to;
    }

    // Job Expiry

    /// <summary>
    /// Cancels Jobs that expired before they could be started. Running late is often worse than
    /// not running at all, e.g. a reminder for a meeting that's already finished.
    /// </summary>
    protected void ExpireJobs()
    {
        try
        {
            var now = DateTime.UtcNow;
            using var db = OpenDb();
            var expiredJobs = db.Select(db.From<BackgroundJob>()
                .Where(x => x.ExpiresAt != null && x.ExpiresAt < now
                    && x.State == BackgroundJobState.Queued && x.CompletedDate == null));
            if (expiredJobs.Count == 0)
                return;

            foreach (var job in expiredJobs)
            {
                var error = new ResponseStatus {
                    ErrorCode = JobErrorCodes.JobExpired,
                    Message = $"Job expired at {job.ExpiresAt:O} before it was started",
                };
                Write(db, () => {
                    var updated = db.UpdateOnly(() => new BackgroundJob {
                        State = BackgroundJobState.Cancelled,
                        Error = error,
                        ErrorCode = error.ErrorCode,
                        CancelRequestedDate = now,
                        CompletedDate = now,
                        LastActivityDate = now,
                    }, where: x => x.Id == job.Id && x.State == BackgroundJobState.Queued);
                    if (updated > 0)
                    {
                        db.UpdateOnly(() => new JobSummary {
                            State = BackgroundJobState.Cancelled,
                            CompletedDate = now,
                            CancelRequestedDate = now,
                            ErrorCode = error.ErrorCode,
                            ErrorMessage = error.Message,
                        }, where: x => x.Id == job.Id);
                    }
                });
            }
            Log.LogInformation("JOBS Expired {Count} Jobs that were not started before their ExpiresAt",
                expiredJobs.Count);
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error expiring Jobs");
        }
    }

    // Concurrency keys

    /// <summary>
    /// Takes the exclusive slot for a Job's ConcurrencyKey. The lock table's primary key is what
    /// enforces it, so two nodes can't both decide they hold the same key.
    /// </summary>
    protected bool TryAcquireConcurrencySlot(IDbConnection db, BackgroundJob job, DateTime now)
    {
        var key = job.ConcurrencyKey;
        if (key == null)
            return true;

        var expiresAt = now.AddSeconds(GetConcurrencySlotSecs(job));
        var serverId = Options.ServerId;
        try
        {
            Write(db, () => db.Insert(new JobConcurrencyLock {
                Key = key,
                JobId = job.Id,
                LeaseOwner = serverId,
                ExpiresAt = expiresAt,
                AcquiredDate = now,
            }));
            return true;
        }
        catch
        {
            // Held by another Job. Reclaim it only if its holder is gone, otherwise wait our turn.
            // A recovered or requeued Job that still holds its own slot takes it back.
            var jobId = job.Id;
            var reclaimed = Write(db, () => db.UpdateOnly(() => new JobConcurrencyLock {
                JobId = jobId,
                LeaseOwner = serverId,
                ExpiresAt = expiresAt,
                AcquiredDate = now,
            }, where: x => x.Key == key && (x.ExpiresAt < now || x.JobId == jobId)));
            return reclaimed > 0;
        }
    }

    /// <summary>
    /// Extends the slot of a Job this node still holds a lease for. Without it the slot would lapse
    /// while the Job waits for a Worker or runs, letting another Job with the same key run alongside it.
    /// </summary>
    protected void RenewConcurrencySlot(IDbConnection db, BackgroundJob job, DateTime now)
    {
        var key = job.ConcurrencyKey;
        if (key == null)
            return;
        try
        {
            var jobId = job.Id;
            var expiresAt = now.AddSeconds(GetConcurrencySlotSecs(job));
            Write(db, () => db.UpdateOnly(() => new JobConcurrencyLock { ExpiresAt = expiresAt },
                where: x => x.Key == key && x.JobId == jobId));
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error renewing concurrency slot {Key}", key);
        }
    }

    /// <summary>Hands back a ConcurrencyKey once its Job is no longer running</summary>
    protected void ReleaseConcurrencySlot(BackgroundJobBase job)
    {
        var key = job.ConcurrencyKey;
        if (key == null)
            return;
        try
        {
            var jobId = job.Id;
            using var db = OpenDb();
            Write(db, () => db.Delete<JobConcurrencyLock>(x => x.Key == key && x.JobId == jobId));
        }
        catch (Exception e)
        {
            // Not fatal: the slot's ExpiresAt means it can't be stuck forever
            Log.LogError(e, "JOBS Error releasing concurrency slot {Key}", key);
        }
    }

    /// <summary>
    /// Frees ConcurrencyKey slots whose holder never released them, e.g. a node that was killed.
    /// </summary>
    protected void ReleaseExpiredConcurrencySlots()
    {
        try
        {
            var now = DateTime.UtcNow;
            using var db = OpenDb();
            var released = Write(db, () => db.Delete<JobConcurrencyLock>(x => x.ExpiresAt < now));
            if (released > 0)
                Log.LogInformation("JOBS Released {Count} expired concurrency slots", released);
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error releasing expired concurrency slots");
        }
    }

    // Queue rate limits

    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> queueStarts = new();

    protected static TimeSpan GetRateLimitWindow(JobQueue q) => TimeSpan.FromSeconds(Math.Max(1, q.RateLimitSecs ?? 1));

    /// <summary>
    /// Whether another Job may start on this queue, for work bound by a third-party quota. Counted in
    /// memory, which is exact for a single node. The RDBMS provider counts across every node instead.
    /// </summary>
    protected virtual bool TryTakeRateLimitSlot(string queue, DateTime now)
    {
        if (!GetJobQueueMap().TryGetValue(queue, out var q) || q.RateLimit is not > 0)
            return true;

        var window = GetRateLimitWindow(q);
        var starts = queueStarts.GetOrAdd(queue, _ => new ConcurrentQueue<DateTime>());
        while (starts.TryPeek(out var oldest) && now - oldest > window)
        {
            starts.TryDequeue(out _);
        }
        if (starts.Count >= q.RateLimit.Value)
            return false;

        starts.Enqueue(now);
        return true;
    }

    /// <summary>Whether a rate limited queue has no slot left in its current window</summary>
    protected virtual bool IsRateLimited(JobQueue q, DateTime now)
    {
        if (q.Name == null || q.RateLimit is not > 0 || !queueStarts.TryGetValue(q.Name, out var starts))
            return false;
        var window = GetRateLimitWindow(q);
        return starts.Count(x => now - x <= window) >= q.RateLimit.Value;
    }

    // Node registry

    private DateTime lastNodeHeartbeat = DateTime.MinValue;

    /// <summary>
    /// Whether an operator asked this node to stop taking new Jobs, refreshed with its heartbeat
    /// </summary>
    protected bool IsDraining { get; private set; }

    /// <summary>Queues this node processes, null for every queue</summary>
    protected virtual List<string>? NodeQueues => null;

    /// <summary>
    /// Records this node as alive so operators can see the servers processing Jobs, and which of
    /// them have stopped reporting.
    /// </summary>
    protected void RecordNodeHeartbeat()
    {
        var now = DateTime.UtcNow;
        if (now - lastNodeHeartbeat < TimeSpan.FromSeconds(Options.NodeHeartbeatSecs))
            return;
        lastNodeHeartbeat = now;

        try
        {
            var serverId = Options.ServerId;
            var runningJobs = GetWorkerStats().Count(x => x.RunningJob != null);
            var concurrency = Options.MaxConcurrentJobs;
            var queues = NodeQueues;
            using var db = OpenDb();
            Write(db, () => {
                var updated = db.UpdateOnly(() => new JobNode {
                    LastHeartbeat = now,
                    RunningJobs = runningJobs,
                    Concurrency = concurrency,
                    Queues = queues,
                    StoppedDate = null,
                }, where: x => x.ServerId == serverId);
                if (updated == 0)
                {
                    db.Insert(new JobNode {
                        ServerId = serverId,
                        MachineName = Environment.MachineName,
                        ProcessId = Environment.ProcessId,
                        Version = typeof(JobNode).Assembly.GetName().Version?.ToString(),
                        StartedDate = now,
                        LastHeartbeat = now,
                        RunningJobs = runningJobs,
                        Concurrency = concurrency,
                        Queues = queues,
                    });
                }
            });
            var draining = db.Scalar<bool>(db.From<JobNode>()
                .Where(x => x.ServerId == serverId)
                .Select(x => x.Draining));
            if (draining != IsDraining)
                Log.LogInformation("JOBS Node {ServerId} {Action} draining", serverId, draining ? "started" : "stopped");
            IsDraining = draining;
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error recording node heartbeat");
        }
    }

    /// <summary>Marks this node as stopped so it isn't reported as a node that vanished</summary>
    protected void RecordNodeStopped()
    {
        try
        {
            var serverId = Options.ServerId;
            var now = DateTime.UtcNow;
            using var db = OpenDb();
            Write(db, () => db.UpdateOnly(() => new JobNode { StoppedDate = now, LastHeartbeat = now, RunningJobs = 0 },
                where: x => x.ServerId == serverId));
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error recording node shutdown");
        }
    }

    public bool SetJobNodeDraining(string serverId, bool draining)
    {
        ArgumentNullException.ThrowIfNull(serverId);
        using var db = OpenDb();
        var updated = Write(db, () => db.UpdateOnly(() => new JobNode { Draining = draining },
            where: x => x.ServerId == serverId));
        if (updated == 0)
            return false;
        // Other nodes see it on their next heartbeat
        if (serverId == Options.ServerId)
            IsDraining = draining;
        Log.LogInformation("JOBS Node {ServerId} Draining={Draining}", serverId, draining);
        return true;
    }

    public List<JobNode> GetJobNodes()
    {
        using var db = OpenDb();
        return db.Select(db.From<JobNode>().OrderByDescending(x => x.LastHeartbeat));
    }

    public JobsStatus GetJobsStatus()
    {
        var now = DateTime.UtcNow;
        using var db = OpenDb();
        var to = new JobsStatus {
            Queued = (int)db.Count<BackgroundJob>(x => x.State == BackgroundJobState.Queued),
            Running = (int)db.Count<BackgroundJob>(x => x.State == BackgroundJobState.Started),
        };

        // The oldest Job that's due shows a queue that stopped being processed, which a backlog
        // count alone misses when the queue is small but stuck.
        var oldest = db.Scalar<DateTime?>(db.From<BackgroundJob>()
            .Where(x => x.State == BackgroundJobState.Queued && x.CompletedDate == null
                && (x.RunAfter == null || x.RunAfter <= now))
            .Select(x => Sql.Min(x.CreatedDate)));
        if (oldest != null && oldest < now)
            to.OldestQueued = now - oldest.Value;

        var alive = TimeSpan.FromSeconds(Options.NodeTimeoutSecs);
        var nodes = db.Select<JobNode>();
        to.Nodes = nodes.Count;
        to.NodesAlive = nodes.Count(x => x.IsAlive(alive));
        return to;
    }

    // Failed attempts

    /// <summary>
    /// Records a failed attempt at executing a Job, called after the failure has been recorded against
    /// the Job itself. Only an attempt that started is recorded. Not fatal: the Job still records its latest error.
    /// </summary>
    protected void RecordJobAttempt(BackgroundJobBase job, int attempt, DateTime? startedDate,
        BackgroundJobState state, ResponseStatus? error)
    {
        if (job.Id == 0 || startedDate == null)
            return;
        try
        {
            var now = DateTime.UtcNow;
            var jobAttempt = new JobAttempt {
                JobId = job.Id,
                Attempt = attempt,
                State = state,
                ServerId = Options.ServerId,
                StartedDate = startedDate,
                DurationMs = (int)(now - startedDate.Value).TotalMilliseconds,
                ErrorCode = error?.ErrorCode,
                Error = error,
                CreatedDate = now,
            };
            using var db = OpenDb();
            Write(db, () => db.Insert(jobAttempt));
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error recording failed attempt of Job {Id}", job.Id);
        }
    }

    public List<JobAttempt> GetJobAttempts(long jobId)
    {
        using var db = OpenDb();
        return db.Select(db.From<JobAttempt>()
            .Where(x => x.JobId == jobId)
            .OrderBy(x => x.Id));
    }

    // Retention

    private DateTime lastAttemptsPurge = DateTime.MinValue;

    /// <summary>Deletes failed attempts older than JobSummaryRetention, along with the summaries they belong to</summary>
    protected void PurgeExpiredJobAttempts()
    {
        if (Options.JobSummaryRetention == null)
            return;
        var now = DateTime.UtcNow;
        if (now - lastAttemptsPurge < TimeSpan.FromHours(1))
            return;
        lastAttemptsPurge = now;

        try
        {
            var expiredDate = now - Options.JobSummaryRetention.Value;
            using var db = OpenDb();
            var deleted = Write(db, () => db.Delete<JobAttempt>(x => x.CreatedDate < expiredDate));
            if (deleted > 0)
                Log.LogInformation("JOBS Deleted {Count} JobAttempt rows created before {ExpiredDate}",
                    deleted, expiredDate);
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error purging expired JobAttempt rows");
        }
    }

    private DateTime lastArchivePurge = DateTime.MinValue;

    /// <summary>
    /// Drops monthly CompletedJob/FailedJob archives older than ArchiveRetention. JobSummaryRetention
    /// only trims the summaries, so without this the archives grow forever.
    /// </summary>
    protected void PurgeExpiredArchives()
    {
        if (Options.ArchiveRetention == null)
            return;
        var now = DateTime.UtcNow;
        if (now - lastArchivePurge < TimeSpan.FromHours(1))
            return;
        lastArchivePurge = now;

        try
        {
            var oldestMonth = new DateTime(now.Year, now.Month, 1).Add(-Options.ArchiveRetention.Value);
            using var db = OpenDb();
            foreach (var month in GetArchiveMonths(db))
            {
                if (month >= oldestMonth)
                    continue;
                Log.LogInformation("JOBS Dropping Job archive for {Month:yyyy-MM}", month);
                DropArchive(month);
            }
        }
        catch (Exception e)
        {
            Log.LogError(e, "JOBS Error purging expired Job archives");
        }
    }
}
#endif
